using System.Collections.Generic;

namespace FrogChess.Server.Models;

public static class MoveValidation
{
    private static readonly (int dr, int dc)[] Directions = {
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1),           (0, 1),
        (1, -1),  (1, 0),  (1, 1)
    };

    public static List<(int r, int c)>? ValidatePath(
        Board board, PlayerColor playerColor,
        IReadOnlyList<(int row, int col)> path)
    {
        var (jumps, error) = ValidatePathWithError(board, playerColor, path);
        return error == null ? jumps : null;
    }

    public static (List<(int r, int c)>? jumps, string? error) ValidatePathWithError(
        Board board, PlayerColor playerColor,
        IReadOnlyList<(int row, int col)> path)
    {
        if (path == null || path.Count < 2)
            return (null, "Path must contain at least two cells.");

        var start = path[0];
        var startCell = board[start.row, start.col];
        if (startCell.Frog != playerColor)
            return (null, "The starting cell does not contain your frog.");

        var jumped = new List<(int r, int c)>();
        Board sim = board.Clone();

        for (int i = 1; i < path.Count; i++)
        {
            var from = path[i - 1];
            var to = path[i];

            var (jumpedCell, error) = ValidateSingleJumpWithError(sim, from, to);
            if (error != null)
                return (null, $"Jump {i} from ({from.row},{from.col}) to ({to.row},{to.col}) is illegal: {error}");

            var (jumpedRow, jumpedCol) = jumpedCell!.Value;
            sim.ClearFrog(jumpedRow, jumpedCol);
            sim.SetFrog(to.row, to.col, playerColor);
            sim.ClearFrog(from.row, from.col);
            jumped.Add((jumpedRow, jumpedCol));
        }
        return (jumped, null);
    }

    private static ((int, int)? cell, string? error) ValidateSingleJumpWithError(
        Board board, (int row, int col) from, (int row, int col) to)
    {
        int dr = to.row - from.row;
        int dc = to.col - from.col;

        if (Math.Abs(dr) > 2 || Math.Abs(dc) > 2)
            return (null, "Jump is more than 2 squares away.");
        if (Math.Abs(dr) != 2 && Math.Abs(dc) != 2)
            return (null, "Jump must be exactly 2 squares away (not a straight or diagonal 2‑step).");
        if (dr != 0 && dc != 0 && Math.Abs(dr) != Math.Abs(dc))
            return (null, "Diagonal jumps must be exactly 2 squares diagonally.");

        int midRow = from.row + dr / 2;
        int midCol = from.col + dc / 2;

        var midCell = board[midRow, midCol];
        if (midCell.IsEmpty)
            return (null, $"There is no frog at the middle cell ({midRow},{midCol}).");

        var targetCell = board[to.row, to.col];
        if (!targetCell.IsEmpty)
            return (null, $"The landing cell ({to.row},{to.col}) is occupied.");

        return ((midRow, midCol), null);
    }


    public static List<Jump> GetValidJumps(Board board, int fromRow, int fromCol)
    {
        var result = new List<Jump>();
        foreach (var (dr, dc) in Directions)
        {
            int toRow = fromRow + 2 * dr;
            int toCol = fromCol + 2 * dc;
            if (toRow < 0 || toRow >= Board.Size || toCol < 0 || toCol >= Board.Size)
                continue;

            var (cell, error) = ValidateSingleJumpWithError(board, (fromRow, fromCol), (toRow, toCol));
            if (error == null && cell.HasValue)
                result.Add(new Jump(fromRow, fromCol, toRow, toCol));
        }
        return result;
    }

    public static bool HasAnyMove(Board board, PlayerColor player)
    {
        foreach (var cell in board.AllCells())
        {
            if (cell.Frog != player) continue;
            if (GetValidJumps(board, cell.Row, cell.Col).Count > 0)
                return true;
        }
        return false;
    }
}