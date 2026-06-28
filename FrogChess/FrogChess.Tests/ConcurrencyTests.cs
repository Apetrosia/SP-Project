using FrogChess.Server.Models;
using System.Collections.Concurrent;
using Xunit;

namespace FrogChess.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task ParallelPathValidation_PreservesOriginalBoardIntegrity()
    {
        var board = new Board();
        board.SetFrog(3, 3, PlayerColor.Red);
        board.SetFrog(3, 4, PlayerColor.Green);
        board.SetFrog(4, 4, PlayerColor.Red);

        var validationErrors = new ConcurrentBag<string>();
        var validationTasks = new List<Task>();

        for (int i = 0; i < 15; i++)
        {
            validationTasks.Add(Task.Run(() =>
            {
                var testPath = new List<(int, int)> { (3, 3), (3, 5) };
                var clonedBoard = board.Clone();
                var (_, error) = MoveValidation.ValidatePathWithError(clonedBoard, PlayerColor.Red, testPath);
                
                if (error != null)
                {
                    validationErrors.Add(error);
                }
            }));
        }

        await Task.WhenAll(validationTasks);

        Assert.Empty(validationErrors);
        Assert.Equal(PlayerColor.Red, board[3, 3].Frog);
        Assert.Equal(PlayerColor.Green, board[3, 4].Frog);
        Assert.Equal(PlayerColor.Red, board[4, 4].Frog);
        Assert.Null(board[3, 5].Frog);
    }
}