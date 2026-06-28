using FrogChess.Server.Models;
using Xunit;

namespace FrogChess.Tests;

public class SwampMechanicTests
{
    [Fact]
    public void Frog_LandsOnSwamp_IsRemoved()
    {
        var board = new Board();
        
        board.SetFrog(2, 2, PlayerColor.Green);
        board.SetFrog(2, 1, PlayerColor.Red);
        var path = new List<(int, int)> { (2, 2), (2, 0) };
        var (jumped, error) = MoveValidation.ValidatePathWithError(board, PlayerColor.Green, path);
        Assert.Null(error);
        Assert.NotNull(jumped);
        Assert.Single(jumped);
        
        board.ClearFrog(jumped[0].r, jumped[0].c);
        board.SetFrog(2, 0, PlayerColor.Green);
        board.ClearFrog(2, 2);
        
        Assert.True(board[2, 0].IsSwamp);
        
        board.ClearFrog(2, 0);
        Assert.Null(board[2, 0].Frog);
    }

    [Fact]
    public void Frog_JumpsFromSwampToWhite_Stays()
    {
        var board = new Board();
        
        board.SetFrog(0, 1, PlayerColor.Green);
        board.SetFrog(1, 1, PlayerColor.Red);
        var path = new List<(int, int)> { (0, 1), (2, 1) };
        var (jumped, error) = MoveValidation.ValidatePathWithError(board, PlayerColor.Green, path);
        Assert.Null(error);
        Assert.NotNull(jumped);
        Assert.Single(jumped);
        board.ClearFrog(jumped[0].r, jumped[0].c);
        board.SetFrog(2, 1, PlayerColor.Green);
        board.ClearFrog(0, 1);
        Assert.False(board[2, 1].IsSwamp); 
        
        Assert.NotNull(board[2, 1].Frog);
    }
}