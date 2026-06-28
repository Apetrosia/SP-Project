

(function () {
    'use strict';

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/gamehub")
        .withAutomaticReconnect()
        .build();

    let currentGameId = null;
    let myColor = null;
    let myPlayerName = null;
    let boardCells = [];
    let simulatedCells = [];
    let removeMode = false;
    let selectedPath = [];
    let currentTurn = null;
    let gameOver = false;
    let canRemove = false;
    let gameActive = false;


    let disconnectTimer = null;
    let disconnectSeconds = 0;

    let turnEl, playersEl, msgEl, passBtn, removeBtn, finishBtn, cancelBtn, messageArea;

    function initDomReferences() {
        turnEl = document.getElementById('turnText');
        playersEl = document.getElementById('playersList');
        msgEl = document.getElementById('gameMsg');
        passBtn = document.getElementById('passBtn');
        removeBtn = document.getElementById('removeBtn');
        finishBtn = document.getElementById('finishBtn');
        cancelBtn = document.getElementById('cancelBtn');
        messageArea = document.getElementById('messageArea');

        if (passBtn) passBtn.style.display = 'none';
    }

    function showMessage(text, isError = false) {
        if (!messageArea) return;
        messageArea.textContent = text;
        messageArea.style.opacity = '1';
        messageArea.style.color = isError ? '#b91c1c' : '#065f46';
        clearTimeout(messageArea._hideTimer);
        messageArea._hideTimer = setTimeout(() => {
            messageArea.style.opacity = '0';
        }, 5000);
    }

    function updateStatus(turnText, players, msg) {
        if (!turnEl || !playersEl || !msgEl) return;
        turnEl.textContent = turnText;
        playersEl.innerHTML = players.map(p =>
            `<span class="player-badge${p.active ? ' active' : ''}">${p.name}</span>`
        ).join('');
        msgEl.textContent = msg;
    }

    function applyBoardState(state) {
        if (state && state.cells) {
            boardCells = state.cells;
            window.setBoardState(state.cells);
        }
    }


    function getValidJumps(row, col, cells) {
        const jumps = [];
        const dirs = [[-2, 0], [2, 0], [0, -2], [0, 2], [-2, -2], [-2, 2], [2, -2], [2, 2]];
        const cellMap = new Map(cells.map(c => [`${c.row},${c.col}`, c]));

        for (const [dr, dc] of dirs) {
            const toRow = row + dr, toCol = col + dc;
            if (toRow < 0 || toRow > 7 || toCol < 0 || toCol > 7) continue;

            const midRow = row + dr / 2, midCol = col + dc / 2;
            const midCell = cellMap.get(`${midRow},${midCol}`);
            const targetCell = cellMap.get(`${toRow},${toCol}`);

            if (!midCell || midCell.color === undefined) continue;
            if (targetCell && targetCell.color !== undefined) continue;

            jumps.push({ row: toRow, col: toCol, midRow, midCol });
        }
        return jumps;
    }

    function highlightDestinations(destinations) {
        const targets = destinations.map(d => ({
            r: d.row, c: d.col,
            isSwamp: d.row === 0 || d.row === 7 || d.col === 0 || d.col === 7
        }));
        window.highlightCells(targets);
    }

    function applyJumpLocally(fromRow, fromCol, toRow, toCol, midRow, midCol, cells) {
        cells = cells.filter(c => !(c.row === midRow && c.col === midCol));
        const frog = cells.find(c => c.row === fromRow && c.col === fromCol);
        if (frog) {
            frog.row = toRow;
            frog.col = toCol;
            frog.color = myColor;
        }
        return cells;
    }

    function isSwamp(row, col) {
        return row === 0 || row === 7 || col === 0 || col === 7;
    }


    function clearSelection() {
        selectedPath = [];
        simulatedCells = [];
        window.clearHighlights();
        window.setBoardState(boardCells);
        if (finishBtn) finishBtn.style.display = 'none';
        if (cancelBtn && !removeMode) cancelBtn.style.display = 'none';
    }

    function cancelMovePreview() {
        if (removeMode) {
            removeMode = false;
            if (removeBtn) {
                removeBtn.classList.remove('bg-amber-100', 'border-amber-500', 'text-amber-700');
                removeBtn.textContent = "🐸 Remove Frog";
            }
            if (cancelBtn) cancelBtn.style.display = 'none';
            return;
        }
        clearSelection();
        showMessage("Move preview cancelled.");
    }

    function selectFrog(row, col) {
        selectedPath = [[row, col]];
        simulatedCells = JSON.parse(JSON.stringify(boardCells));
        window.highlightCells([{ r: row, c: col, isSwamp: false }]);
        const jumps = getValidJumps(row, col, simulatedCells);
        highlightDestinations(jumps);
        if (finishBtn) finishBtn.style.display = 'none';
        if (cancelBtn) cancelBtn.style.display = 'inline-block';
    }


    function startDisconnectCountdown(seconds) {
        stopDisconnectCountdown();
        disconnectSeconds = seconds;
        updateCountdownDisplay();
        disconnectTimer = setInterval(() => {
            disconnectSeconds--;
            if (disconnectSeconds <= 0) {
                stopDisconnectCountdown();
            } else {
                updateCountdownDisplay();
            }
        }, 1000);
    }

    function stopDisconnectCountdown() {
        if (disconnectTimer) {
            clearInterval(disconnectTimer);
            disconnectTimer = null;
        }
        disconnectSeconds = 0;

    }

    function updateCountdownDisplay() {
        if (!messageArea) return;
        messageArea.textContent = `Opponent disconnected – ${disconnectSeconds}s to reconnect`;
        messageArea.style.opacity = '1';
        messageArea.style.color = '#b91c1c';
        clearTimeout(messageArea._hideTimer);

    }


    connection.on("GameCreated", (gameId, color) => {
        myColor = color;
        myPlayerName = myPlayerName || "Frogger";
        currentGameId = gameId;
        localStorage.setItem('frogchess_gameId', gameId);
        localStorage.setItem('frogchess_playerName', myPlayerName);
        const idDisplay = document.getElementById('gameIdDisplay');
        const idText = document.getElementById('gameIdText');
        if (idText) idText.textContent = gameId;
        if (idDisplay) idDisplay.style.display = 'flex';
        updateStatus("🐸 WAITING", [{ name: myPlayerName, active: true }],
            "Waiting for opponent...");
        gameActive = false;
    });

    connection.on("YourColor", (color) => {
        myColor = color;
    });

    connection.on("GameStarted", (state) => {
        localStorage.setItem('frogchess_gameId', currentGameId);
        localStorage.setItem('frogchess_playerName', myPlayerName);
        applyBoardState(state);
        const opp = (myColor === 'green') ? 'Red' : 'Green';
        updateStatus("🎲 GAME STARTED",
            [{ name: myPlayerName, active: false }, { name: opp, active: false }],
            "First turn coming...");
        gameOver = false;
        gameActive = true;
        canRemove = true;
        if (removeBtn) removeBtn.style.display = 'none';
        window.clearHighlights();
        stopDisconnectCountdown();
    });

    connection.on("TurnChanged", (color, message) => {
        currentTurn = color;
        const myTurn = (currentTurn === myColor);
        const opp = myColor === 'green' ? 'Red' : 'Green';
        updateStatus(myTurn ? "🐸 YOUR TURN" : "⏳ THEIR TURN",
            [{ name: myPlayerName, active: myTurn }, { name: opp, active: !myTurn }],
            message);
        clearSelection();
        if (myTurn && canRemove) {
            if (removeBtn) removeBtn.style.display = 'inline-block';
        } else {
            if (removeBtn) removeBtn.style.display = 'none';
        }
    });

    connection.on("BoardState", (state) => applyBoardState(state));

    connection.on("MoveExecuted", (path, jumped, removedBySwamp) => {
        if (removedBySwamp) showMessage("Frog lost in the swamp!");
    });

    connection.on("FrogRemoved", (row, col, removingColor) => {
        if (removingColor === myColor) {
            showMessage("Frog removed");
            canRemove = false;
            if (removeBtn) removeBtn.style.display = 'none';
        }
    });

    connection.on("RemoveRightLost", () => {
        canRemove = false;
        if (removeBtn) removeBtn.style.display = 'none';
    });

    connection.on("Error", (msg) => {
        showMessage(msg, true);
        clearSelection();
    });

    connection.on("GameOver", (winner) => {
        gameOver = true;
        gameActive = false;
        localStorage.removeItem('frogchess_gameId');
        localStorage.removeItem('frogchess_playerName');
        updateStatus("🏁 GAME OVER", [],
            winner ? winner.charAt(0).toUpperCase() + winner.slice(1) + " wins!" : "Draw");
        if (removeBtn) removeBtn.style.display = 'none';
        if (finishBtn) finishBtn.style.display = 'none';
        if (cancelBtn) cancelBtn.style.display = 'none';
        stopDisconnectCountdown();
    });

    connection.on("PlayerDisconnected", (color, timeoutSec) => {

        startDisconnectCountdown(timeoutSec);
    });

    connection.on("PlayerReconnected", (color) => {
        stopDisconnectCountdown();
        showMessage("Opponent reconnected");
    });

    connection.on("Reconnected", (state) => {
        applyBoardState(state);
        gameOver = false;
        gameActive = true;
        stopDisconnectCountdown();
    });

    connection.on("ReconnectFailed", () => {
        localStorage.removeItem('frogchess_gameId');
        localStorage.removeItem('frogchess_playerName');
        stopDisconnectCountdown();
    });


    async function connectAndJoin(name, gameId) {
        myPlayerName = name;
        try {
            if (connection.state !== signalR.HubConnectionState.Connected) {
                await connection.start();
                console.log("SignalR connected");
            }
            if (gameId) {
                await connection.invoke("JoinGame", gameId, name);
            } else {
                await connection.invoke("CreateGame", name);
            }
        } catch (err) {
            showMessage(err.message || "Connection failed", true);
        }
    }

    async function reconnectFromStorage() {

        return false;
    }

    function handleCellClick(row, col) {
        if (!gameActive || gameOver) return;
        if (currentTurn !== myColor) {
            showMessage("Not your turn", true);
            return;
        }

        if (removeMode) {
            const cell = boardCells.find(c => c.row === row && c.col === col);
            if (!cell || (cell.color !== 'green' && cell.color !== 'red')) {
                showMessage("Tap a frog to remove", true);
                return;
            }
            connection.invoke("RemoveFrog", row, col);
            setRemoveMode(false);
            return;
        }

        const clickedCell = boardCells.find(c => c.row === row && c.col === col);
        const isOwnFrog = clickedCell && clickedCell.color === myColor;

        if (selectedPath.length === 0) {
            if (isOwnFrog) {
                selectFrog(row, col);
            } else {
                showMessage("Select one of your frogs", true);
            }
            return;
        }

        const [lastRow, lastCol] = selectedPath[selectedPath.length - 1];
        const validJumps = getValidJumps(lastRow, lastCol, simulatedCells);
        const jump = validJumps.find(j => j.row === row && j.col === col);

        if (jump) {
            selectedPath.push([row, col]);
            simulatedCells = applyJumpLocally(lastRow, lastCol, row, col,
                jump.midRow, jump.midCol, simulatedCells);
            window.setBoardState(simulatedCells);

            const further = getValidJumps(row, col, simulatedCells);
            const currentlyInSwamp = isSwamp(row, col);

            window.clearHighlights();

            if (further.length > 0) {
                highlightDestinations(further);
                finishBtn.style.display = 'inline-block';
                if (cancelBtn) cancelBtn.style.display = 'inline-block';
                showMessage("Click another destination or Finish Move.");
            } else {
                if (currentlyInSwamp) {
                    finishBtn.style.display = 'inline-block';
                    if (cancelBtn) cancelBtn.style.display = 'inline-block';
                    showMessage("Frog will be lost! Click Finish Move to confirm.", true);
                    window.highlightCells([{ r: row, c: col, isSwamp: true }]);
                } else {
                    connection.invoke("MakeMove", selectedPath);
                    clearSelection();
                }
            }
        } else {
            showMessage("Invalid landing spot – click a highlighted cell", true);
        }
    }

    function finishMove() {
        if (selectedPath.length < 2) {
            showMessage("At least one jump required", true);
            return;
        }
        connection.invoke("MakeMove", selectedPath);
        clearSelection();
    }

    function setRemoveMode(active) {
        removeMode = active;
        selectedPath = [];
        simulatedCells = [];
        window.clearHighlights();
        window.setBoardState(boardCells);
        if (finishBtn) finishBtn.style.display = 'none';
        if (active) {
            if (removeBtn) {
                removeBtn.classList.add('bg-amber-100', 'border-amber-500', 'text-amber-700');
                removeBtn.textContent = "✅ Confirm Remove";
            }
            if (cancelBtn) cancelBtn.style.display = 'inline-block';
        } else {
            if (cancelBtn) cancelBtn.style.display = 'none';
            if (removeBtn) {
                removeBtn.classList.remove('bg-amber-100', 'border-amber-500', 'text-amber-700');
                removeBtn.textContent = "🐸 Remove Frog";
                removeBtn.style.display = canRemove ? 'inline-block' : 'none';
            }
        }
    }

    function toggleRemoveMode() { setRemoveMode(!removeMode); }

    window.frogGame = {
        connectAndJoin,
        reconnectFromStorage,
        handleCellClick,
        passTurn: () => { },
        toggleRemoveMode,
        setRemoveButtonVisible: (show) => {
            if (removeBtn) removeBtn.style.display = show ? 'inline-block' : 'none';
        }
    };

    window.addEventListener('DOMContentLoaded', () => {
        initDomReferences();
        if (removeBtn) removeBtn.addEventListener('click', toggleRemoveMode);
        if (finishBtn) finishBtn.addEventListener('click', finishMove);
        if (cancelBtn) cancelBtn.addEventListener('click', cancelMovePreview);
    });

    window.hideModal = () => {
        const modal = document.getElementById('joinModalBackdrop');
        if (modal) modal.style.display = 'none';
    };
    window.showModal = () => {
        const modal = document.getElementById('joinModalBackdrop');
        if (modal) modal.style.display = 'flex';
    };
    window.copyGameId = () => {
        const text = document.getElementById('gameIdText')?.textContent;
        if (text) navigator.clipboard.writeText(text);
    };
})();