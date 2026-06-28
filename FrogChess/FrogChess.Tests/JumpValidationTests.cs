using FrogChess.Server.Models;
using Xunit;

namespace FrogChess.Tests;

public class JumpValidationTests
{
    [Fact]
    public void ValidJump_OverOwnFrog()
    {
        var board = new Board();
        board.SetFrog(2, 2, PlayerColor.Green);
        board.SetFrog(2, 3, PlayerColor.Green);
        var path = new List<(int, int)> { (2, 2), (2, 4) };
        var (jumped, error) = MoveValidation.ValidatePathWithError(board, PlayerColor.Green, path);
        Assert.Null(error);
        Assert.NotNull(jumped);
        Assert.Single(jumped!);
        Assert.Equal((2, 3), jumped![0]);
    }

    [Fact]
    public void ValidJump_OverOpponentFrog()
    {
        var board = new Board();
        board.SetFrog(2, 2, PlayerColor.Green);
        board.SetFrog(2, 3, PlayerColor.Red);
        var path = new List<(int, int)> { (2, 2), (2, 4) };
        var (jumped, error) = MoveValidation.ValidatePathWithError(board, PlayerColor.Green, path);
        Assert.Null(error);
        Assert.NotNull(jumped);
        Assert.Equal((2, 3), jumped![0]);
    }

    [Fact]
    public void ChainOfTwoJumps_Works()
    {
        var board = new Board();
        board.SetFrog(2, 2, PlayerColor.Green);
        board.SetFrog(2, 3, PlayerColor.Red);
        board.SetFrog(2, 5, PlayerColor.Red);
        var path = new List<(int, int)> { (2, 2), (2, 4), (2, 6) };
        var (jumped, error) = MoveValidation.ValidatePathWithError(board, PlayerColor.Green, path);
        Assert.Null(error);
        Assert.NotNull(jumped);
        Assert.Equal(2, jumped!.Count);
    }

    [Fact]
    public void Jump_NotStraightLine_Rejects()
    {
        var board = new Board();
        board.SetFrog(2, 2, PlayerColor.Green);
        var path = new List<(int, int)> { (2, 2), (3, 4) };
        var (_, error) = MoveValidation.ValidatePathWithError(board, PlayerColor.Green, path);
        Assert.NotNull(error);
    }

    [Fact]
    public void Jump_LandingOnOccupied_Rejects()
    {
        var board = new Board();
        board.SetFrog(2, 2, PlayerColor.Green);
        board.SetFrog(2, 3, PlayerColor.Red);
        board.SetFrog(2, 4, PlayerColor.Red);
        var path = new List<(int, int)> { (2, 2), (2, 4) };
        var (_, error) = MoveValidation.ValidatePathWithError(board, PlayerColor.Green, path);
        Assert.NotNull(error);
    }

    [Fact]
    public void Jump_MoreThanTwoSquares_Rejects()
    {
        var board = new Board();
        board.SetFrog(2, 2, PlayerColor.Green);
        board.SetFrog(2, 4, PlayerColor.Red);
        var path = new List<(int, int)> { (2, 2), (2, 6) };
        var (_, error) = MoveValidation.ValidatePathWithError(board, PlayerColor.Green, path);
        Assert.NotNull(error);
    }
}