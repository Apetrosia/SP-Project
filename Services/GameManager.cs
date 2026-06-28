using System.Collections.Concurrent;
using System.Threading;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using FrogChess.Server.Hubs;
using FrogChess.Server.Models;

namespace FrogChess.Server.Services;

public class GameManager
{
    private readonly ConcurrentDictionary<string, Game> _games = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<(string gameId, string color), Timer> _reconnectTimers = new();
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameManager> _logger;

    public GameManager(IHubContext<GameHub> hubContext, ILogger<GameManager> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }
    public Task<Game> CreateGame(string playerName, string connectionId)
    {
        var game = new Game
        {
            Green = new Player { Name = playerName, Color = PlayerColor.Green, ConnectionId = connectionId, IsConnected = true }
        };
        _games[game.Id] = game;
        _locks[game.Id] = new SemaphoreSlim(1, 1);
        _logger.LogInformation("Game {GameId} created by {Player}", game.Id, playerName);
        return Task.FromResult(game);
    }

    public async Task<Game?> JoinGame(string gameId, string playerName, string connectionId)
    {
        if (!_games.TryGetValue(gameId, out var game))
            return null;

        if (game.Red != null)
            return null; 

        game.Red = new Player { Name = playerName, Color = PlayerColor.Red, ConnectionId = connectionId, IsConnected = true };

        
        StartGame(game);
        _logger.LogInformation("Game {GameId} joined by {Player}, game started", gameId, playerName);
        return game;
    }

       public Game? ReconnectPlayer(string gameId, string playerName, string connectionId)
    {
        if (!_games.TryGetValue(gameId, out var game))
            return null;

        if (game.Green?.Name == playerName)
        {
            game.Green.ConnectionId = connectionId;
            game.Green.IsConnected = true;
            CancelReconnectTimer(gameId, "green");
            return game;
        }
        if (game.Red?.Name == playerName)
        {
            game.Red.ConnectionId = connectionId;
            game.Red.IsConnected = true;
            CancelReconnectTimer(gameId, "red");
            return game;
        }
        return null;
    }

    public void StartReconnectTimer(Game game, PlayerColor color)
    {
        string colorStr = color == PlayerColor.Green ? "green" : "red";
        var key = (game.Id, colorStr);
        var timer = new Timer(async _ =>
        {
            try
            {
                await HandleReconnectTimeout(game.Id, color);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconnect timer error for game {GameId}", game.Id);
            }
        }, null, TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);

        _reconnectTimers.AddOrUpdate(key, timer, (k, old) =>
        {
            old.Dispose();
            return timer;
        });
    }

    public void CancelReconnectTimer(string gameId, string color)
    {
        var key = (gameId, color);
        if (_reconnectTimers.TryRemove(key, out var timer))
        {
            timer.Dispose();
        }
    }

    private async Task HandleReconnectTimeout(string gameId, PlayerColor disconnectedColor)
    {
        if (!_games.TryGetValue(gameId, out var game))
            return;

        if (game.Finished)
            return;

        
        Player? disconnectedPlayer = disconnectedColor == PlayerColor.Green ? game.Green : game.Red;
        if (disconnectedPlayer == null || disconnectedPlayer.IsConnected)
            return;

        
        PlayerColor winnerColor = disconnectedColor == PlayerColor.Green ? PlayerColor.Red : PlayerColor.Green;
        game.Finished = true;
        game.Winner = winnerColor;

        
        string winnerColorStr = winnerColor == PlayerColor.Green ? "green" : "red";
        await _hubContext.Clients.Group(gameId).SendAsync("GameOver", winnerColorStr);
        _logger.LogInformation("Game {GameId} ended due to disconnect timeout. Winner: {Winner}", gameId, winnerColorStr);

        
        RemoveGame(gameId);
    }

    public void RemoveGame(string gameId)
    {
        _games.TryRemove(gameId, out _);
        if (_locks.TryRemove(gameId, out var sem))
            sem.Dispose();
        
        foreach (var key in _reconnectTimers.Keys.Where(k => k.gameId == gameId).ToList())
        {
            if (_reconnectTimers.TryRemove(key, out var timer))
                timer.Dispose();
        }
    }

    private void StartGame(Game game)
    {
        
        var cells = game.Board.PlayableCells().ToList();
        var rng = new Random();
        
        cells = cells.OrderBy(_ => rng.Next()).ToList();

        for (int i = 0; i < 18; i++)
            cells[i].Frog = PlayerColor.Green;
        for (int i = 18; i < 36; i++)
            cells[i].Frog = PlayerColor.Red;

        game.Started = true;
        game.CurrentTurn = PlayerColor.Green;
    }

    public async Task LockGame(string gameId) => await _locks[gameId].WaitAsync();
    public void UnlockGame(string gameId) => _locks[gameId].Release();

    public Game? GetGame(string gameId) => _games.TryGetValue(gameId, out var g) ? g : null;

    public IEnumerable<Game> GetAllGames() => _games.Values;
}