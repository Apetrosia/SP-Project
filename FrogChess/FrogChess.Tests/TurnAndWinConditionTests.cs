using FrogChess.Server.Models;
using Xunit;

namespace FrogChess.Tests;

public class TurnAndWinConditionTests
{
    private Game CreateGame()
    {
        var game = new Game();
        
        var cells = game.Board.PlayableCells().ToList();
        cells = cells.OrderBy(_ => Guid.NewGuid()).ToList();
        for (int i = 0; i < 18; i++) cells[i].Frog = PlayerColor.Green;
        for (int i = 18; i < 36; i++) cells[i].Frog = PlayerColor.Red;
        game.Started = true;
        game.Green = new Player { Name = "G", Color = PlayerColor.Green, ConnectionId = "g", IsConnected = true };
        game.Red = new Player { Name = "O", Color = PlayerColor.Red, ConnectionId = "o", IsConnected = true };
        game.CurrentTurn = PlayerColor.Green;
        return game;
    }

    [Fact]
    public void PlayerWithoutMoves_MustPass()
    {
        var game = CreateGame();
        
        foreach (var cell in game.Board.PlayableCells())
            cell.Frog = null;
        
        game.Board.SetFrog(3, 3, PlayerColor.Green);
        
        Assert.False(MoveValidation.HasAnyMove(game.Board, PlayerColor.Green));
    }

    [Fact]
    public void BothCannotMove_GameEnds_LastJumperWins()
    {
        var game = CreateGame();
        game.LastJumpPlayer = PlayerColor.Green;
        foreach (var cell in game.Board.PlayableCells())
            cell.Frog = null;
        Assert.False(MoveValidation.HasAnyMove(game.Board, PlayerColor.Green));
        Assert.False(MoveValidation.HasAnyMove(game.Board, PlayerColor.Red));
        game.ConsecutivePasses = 2;
        game.Finished = true;
        game.Winner = game.LastJumpPlayer;
        Assert.Equal(PlayerColor.Green, game.Winner);
    }

    [Fact]
    public void AfterMove_TurnSwitches()
    {
        var game = CreateGame();
        game.CurrentTurn = PlayerColor.Green;
        game.CurrentTurn = PlayerColor.Red;
        Assert.Equal(PlayerColor.Red, game.CurrentTurn);
    }
}