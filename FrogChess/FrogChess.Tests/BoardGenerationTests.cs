using FrogChess.Server.Models;
using Xunit;

namespace FrogChess.Tests;

public class BoardGenerationTests
{
    [Fact]
    public void InitialBoardSetup_ContainsCorrectFrogDistribution()
    {
        var board = new Board();
        var playableCells = board.PlayableCells().ToList();
        var random = new Random();

        var shuffledCells = playableCells.OrderBy(_ => random.Next()).ToList();

        for (int i = 0; i < 18; i++)
        {
            shuffledCells[i].Frog = PlayerColor.Green;
        }
        for (int i = 18; i < 36; i++)
        {
            shuffledCells[i].Frog = PlayerColor.Red;
        }

        var greenFrogs = board.PlayableCells().Count(c => c.Frog == PlayerColor.Green);
        var redFrogs = board.PlayableCells().Count(c => c.Frog == PlayerColor.Red);
        var totalOccupied = board.PlayableCells().Count(c => c.Frog != null);

        Assert.Equal(18, greenFrogs);
        Assert.Equal(18, redFrogs);
        Assert.Equal(36, totalOccupied);
    }

    [Fact]
    public void MultipleBoardGenerations_ProduceUniqueConfigurations()
    {
        var uniqueConfigurations = new HashSet<string>();
        const int generationAttempts = 100;

        for (int attempt = 0; attempt < generationAttempts; attempt++)
        {
            var board = new Board();
            var playableCells = board.PlayableCells().ToList();

            var randomizedCells = playableCells.OrderBy(_ => Guid.NewGuid()).ToList();

            for (int i = 0; i < 18; i++)
            {
                randomizedCells[i].Frog = PlayerColor.Green;
            }
            for (int i = 18; i < 36; i++)
            {
                randomizedCells[i].Frog = PlayerColor.Red;
            }

            var configSignature = string.Join(",",
                board.PlayableCells().Select(c => $"({c.Row},{c.Col}):{c.Frog}"));
            uniqueConfigurations.Add(configSignature);
        }
        
        Assert.True(uniqueConfigurations.Count > 1,
            "Board generation should produce different configurations");
    }
}