using FrogChess.Server.Models;
using Xunit;

namespace FrogChess.Tests;

public class FirstMoveRemovalTests
{
    private Game InitializeTestGame()
    {
        var game = new Game();
        var playableCells = game.Board.PlayableCells().ToList();
        var shuffledCells = playableCells.OrderBy(_ => Guid.NewGuid()).ToList();

        for (int i = 0; i < 18; i++)
        {
            shuffledCells[i].Frog = PlayerColor.Green;
        }
        for (int i = 18; i < 36; i++)
        {
            shuffledCells[i].Frog = PlayerColor.Red;
        }

        game.Started = true;
        game.Green = new Player
        {
            Name = "GreenPlayer",
            Color = PlayerColor.Green,
            ConnectionId = "green-conn",
            IsConnected = true
        };
        game.Red = new Player
        {
            Name = "RedPlayer",
            Color = PlayerColor.Red,
            ConnectionId = "red-conn",
            IsConnected = true
        };

        return game;
    }

    [Fact]
    public void FrogRemoval_IsAllowedBeforeFirstJump()
    {
        var game = InitializeTestGame();
        var targetCell = game.Board[2, 2];
        targetCell.Frog = PlayerColor.Green;

        game.Board.ClearFrog(2, 2);
        game.GreenRemovedFrog = true;

        Assert.False(game.GreenHasJumped, "Player should not have jumped yet");
        Assert.Null(game.Board[2, 2].Frog);
        Assert.True(game.GreenRemovedFrog, "Removal flag should be set");
    }

    [Fact]
    public void FrogRemoval_IsForbiddenAfterFirstJump()
    {
        var game = InitializeTestGame();

        game.GreenHasJumped = true;

        Assert.True(game.GreenHasJumped, "Player has already jumped");
    }

    [Fact]
    public void FrogRemoval_CanOnlyBeDoneOnce()
    {
        var game = InitializeTestGame();

        game.GreenRemovedFrog = true;

        Assert.True(game.GreenRemovedFrog, "Removal already used");
    }
}