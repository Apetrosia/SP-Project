using Microsoft.AspNetCore.SignalR;
using FrogChess.Server.Models;
using FrogChess.Server.Services;
using System.Linq;

namespace FrogChess.Server.Hubs;

public class GameHub : Hub
{
    private readonly GameManager _manager;
    private readonly ILogger<GameHub> _logger;

    public GameHub(GameManager manager, ILogger<GameHub> logger)
    {
        _manager = manager;
        _logger = logger;
    }

    public async Task CreateGame(string playerName)
    {
        var game = await _manager.CreateGame(playerName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, game.Id);
        Context.Items["GameId"] = game.Id;
        await Clients.Caller.SendAsync("GameCreated", game.Id, "green");
        _logger.LogInformation("Game {GameId} created by {Player}", game.Id, playerName);
    }

public async Task JoinGame(string gameId, string playerName)
{
    var game = await _manager.JoinGame(gameId, playerName, Context.ConnectionId);
    if (game == null)
    {
        var reconnected = _manager.ReconnectPlayer(gameId, playerName, Context.ConnectionId);
        if (reconnected != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, reconnected.Id);
            Context.Items["GameId"] = reconnected.Id;

            string colorStr = (reconnected.Green?.Name == playerName) ? "green" : "red";
            await Clients.Caller.SendAsync("YourColor", colorStr);
            await Clients.Caller.SendAsync("Reconnected", GetBoardState(reconnected));
            await Clients.Group(reconnected.Id).SendAsync("PlayerReconnected", colorStr);

            string currentTurnColor = reconnected.CurrentTurn == PlayerColor.Green ? "green" : "red";
            string turnMessage = $"{GetPlayerName(reconnected, reconnected.CurrentTurn)}'s turn";
            await Clients.Caller.SendAsync("TurnChanged", currentTurnColor, turnMessage);

            _logger.LogInformation("{Player} reconnected to game {GameId}", playerName, gameId);
            return;
        }

        await Clients.Caller.SendAsync("Error", "Game not found or full");
        return;
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, game.Id);
    Context.Items["GameId"] = game.Id;
    await Clients.Caller.SendAsync("YourColor", "red");
    await Clients.Client(game.Green!.ConnectionId!).SendAsync("OpponentJoined", "red", playerName);
    await Clients.Group(game.Id).SendAsync("GameStarted", GetBoardState(game));
    await Clients.Group(game.Id).SendAsync("TurnChanged", "green", $"{game.Green.Name}'s turn");
    _logger.LogInformation("{Player} joined game {GameId}", playerName, gameId);
}

public async Task Reconnect(string gameId, string playerName)
{
    var game = _manager.ReconnectPlayer(gameId, playerName, Context.ConnectionId);
    if (game == null)
    {
        
        await Clients.Caller.SendAsync("ReconnectFailed");
        return;
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, game.Id);
    Context.Items["GameId"] = game.Id;

    string colorStr = (game.Green?.Name == playerName) ? "green" : "red";
    await Clients.Caller.SendAsync("YourColor", colorStr);
    await Clients.Caller.SendAsync("Reconnected", GetBoardState(game));
    await Clients.Group(game.Id).SendAsync("PlayerReconnected", colorStr);

    string currentTurnColor = game.CurrentTurn == PlayerColor.Green ? "green" : "red";
    string turnMessage = $"{GetPlayerName(game, game.CurrentTurn)}'s turn";
    await Clients.Caller.SendAsync("TurnChanged", currentTurnColor, turnMessage);
    _logger.LogInformation("{Player} reconnected to game {GameId}", playerName, gameId);
}

    public async Task RemoveFrog(int row, int col)
    {
        var game = await GetAndLockGame();
        if (game == null) return;
        try
        {
            var player = GetCurrentPlayer(game);
            if (player == null) { await SendError("Not a player"); return; }
            bool alreadyRemoved = player.Color == PlayerColor.Green ? game.GreenRemovedFrog : game.RedRemovedFrog;
            if (alreadyRemoved) { await SendError("You already removed a frog"); return; }
            if (HasPlayerJumped(game, player.Color)) { await SendError("You already jumped"); return; }

            var cell = game.Board[row, col];
            if (cell.IsEmpty) { await SendError("Cell is empty"); return; }

            game.Board.ClearFrog(row, col);
            if (player.Color == PlayerColor.Green) game.GreenRemovedFrog = true;
            else game.RedRemovedFrog = true;

            
            string removingColor = player.Color == PlayerColor.Green ? "green" : "red";
            await Clients.Group(game.Id).SendAsync("FrogRemoved", row, col, removingColor);
            await Clients.Group(game.Id).SendAsync("BoardState", GetBoardState(game));
        }
        finally { _manager.UnlockGame(game.Id); }
    }

    public async Task MakeMove(List<int[]> pathArray)
    {
        var game = await GetAndLockGame();
        if (game == null) return;
        try
        {
            var player = GetCurrentPlayer(game);
            if (player == null) { await SendError("Not a player"); return; }
            if (game.CurrentTurn != player.Color) { await SendError("Not your turn"); return; }

            var path = pathArray.Select(p => (row: p[0], col: p[1])).ToList();
            var (jumped, error) = MoveValidation.ValidatePathWithError(game.Board, player.Color, path);
            if (error != null)
            {
                await SendError(error);
                return;
            }

            var board = game.Board;
            for (int i = 1; i < path.Count; i++)
            {
                var from = path[i - 1];
                var to = path[i];
                board.ClearFrog(jumped![i - 1].r, jumped[i - 1].c);
                board.SetFrog(to.row, to.col, player.Color);
                board.ClearFrog(from.row, from.col);
            }

            var final = board[path.Last().row, path.Last().col];
            bool removedBySwamp = false;
            if (final.IsSwamp)
            {
                board.ClearFrog(final.Row, final.Col);
                removedBySwamp = true;
            }

            
            bool wasFirstJump = !HasPlayerJumped(game, player.Color);
            game.LastJumpPlayer = player.Color;
            game.ConsecutivePasses = 0;
            if (player.Color == PlayerColor.Green) game.GreenHasJumped = true;
            else game.RedHasJumped = true;

            
            if (wasFirstJump)
            {
                await Clients.Caller.SendAsync("RemoveRightLost");
            }

            await Clients.Group(game.Id).SendAsync("MoveExecuted", path, jumped, removedBySwamp);
            await Clients.Group(game.Id).SendAsync("BoardState", GetBoardState(game));

            SwitchTurn(game);
            await AutoPassIfNeeded(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing move");
            await SendError("An unexpected error occurred");
        }
        finally { _manager.UnlockGame(game.Id); }
    }

    public async Task PassTurn()
    {
        var game = await GetAndLockGame();
        if (game == null) return;
        try
        {
            var player = GetCurrentPlayer(game);
            if (player == null) { await SendError("Not a player"); return; }
            if (game.CurrentTurn != player.Color) { await SendError("Not your turn"); return; }

            bool hasMoves = MoveValidation.HasAnyMove(game.Board, player.Color);
            if (hasMoves) { await SendError("You have legal moves – you cannot pass"); return; }

            await ProcessPass(game, player.Color);
            if (!game.Finished)
            {
                SwitchTurn(game);
                await AutoPassIfNeeded(game);
            }
        }
        finally { _manager.UnlockGame(game.Id); }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var game in _manager.GetAllGames())
        {
            if (game.Green?.ConnectionId == Context.ConnectionId && game.Green.IsConnected)
            {
                game.Green.IsConnected = false;
                await Clients.Group(game.Id).SendAsync("PlayerDisconnected", "green", 30);
                _manager.StartReconnectTimer(game, PlayerColor.Green);
                break;
            }
            if (game.Red?.ConnectionId == Context.ConnectionId && game.Red.IsConnected)
            {
                game.Red.IsConnected = false;
                await Clients.Group(game.Id).SendAsync("PlayerDisconnected", "red", 30);
                _manager.StartReconnectTimer(game, PlayerColor.Red);
                break;
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<Game?> GetAndLockGame()
    {
        var gameId = Context.Items["GameId"] as string;
        if (gameId == null) { await SendError("You're not in a game"); return null; }
        var game = _manager.GetGame(gameId);
        if (game == null) { await SendError("Game not found"); return null; }
        await _manager.LockGame(gameId);
        return game;
    }

    private Player? GetCurrentPlayer(Game game)
    {
        if (game.Green?.ConnectionId == Context.ConnectionId) return game.Green;
        if (game.Red?.ConnectionId == Context.ConnectionId) return game.Red;
        return null;
    }

    private bool HasPlayerJumped(Game game, PlayerColor color) =>
        color == PlayerColor.Green ? game.GreenHasJumped : game.RedHasJumped;

    private string GetPlayerName(Game game, PlayerColor color) =>
        color == PlayerColor.Green ? game.Green?.Name : game.Red?.Name ?? "?";

    private void SwitchTurn(Game game)
    {
        game.CurrentTurn = game.CurrentTurn == PlayerColor.Green ? PlayerColor.Red : PlayerColor.Green;
        string nextColor = game.CurrentTurn == PlayerColor.Green ? "green" : "red";
        _ = Clients.Group(game.Id).SendAsync("TurnChanged", nextColor,
                $"{GetPlayerName(game, game.CurrentTurn)}'s turn");
    }

    private async Task ProcessPass(Game game, PlayerColor passingColor)
    {
        game.ConsecutivePasses++;
        await Clients.Group(game.Id).SendAsync("PlayerPassed", passingColor.ToString().ToLower());
        if (game.ConsecutivePasses >= 2)
        {
            game.Finished = true;
            game.Winner = game.LastJumpPlayer;
            await Clients.Group(game.Id).SendAsync("GameOver", game.Winner?.ToString().ToLower());
            _manager.RemoveGame(game.Id);
        }
    }

    private async Task AutoPassIfNeeded(Game game)
    {
        while (!game.Finished)
        {
            PlayerColor current = game.CurrentTurn;
            bool hasMoves = MoveValidation.HasAnyMove(game.Board, current);
            if (hasMoves) break;
            await ProcessPass(game, current);
            if (game.Finished) return;
            SwitchTurn(game);
        }
    }

    private object GetBoardState(Game game)
    {
        var cells = game.Board.AllCells()
            .Where(c => !c.IsEmpty)
            .Select(c => new { row = c.Row, col = c.Col, color = c.Frog?.ToString().ToLower() ?? "" });
        return new { cells };
    }

    private async Task SendError(string message)
    {
        await Clients.Caller.SendAsync("Error", message);
    }
}