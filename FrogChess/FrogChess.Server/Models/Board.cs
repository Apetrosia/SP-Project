namespace FrogChess.Server.Models;

public class Board
{
    public const int Size = 8;          
    public const int PlayableSize = 6;

    private readonly Cell[,] _cells;

    public Board()
    {
        _cells = new Cell[Size, Size];
        for (int r = 0; r < Size; r++)
        for (int c = 0; c < Size; c++)
        {
            bool isSwamp = r == 0 || r == Size - 1 || c == 0 || c == Size - 1;
            _cells[r, c] = new Cell
            {
                Row = r, Col = c,
                Type = isSwamp ? CellType.Swamp : CellType.White
            };
        }
    }

    public Cell this[int row, int col] => _cells[row, col];

    public IEnumerable<Cell> PlayableCells()
    {
        for (int r = 1; r <= PlayableSize; r++)
        for (int c = 1; c <= PlayableSize; c++)
            yield return _cells[r, c];
    }

    public IEnumerable<Cell> AllCells()
    {
        for (int r = 0; r < Size; r++)
        for (int c = 0; c < Size; c++)
            yield return _cells[r, c];
    }

    public void ClearFrog(int row, int col) => _cells[row, col].Frog = null;
    public void SetFrog(int row, int col, PlayerColor color) => _cells[row, col].Frog = color;

    public Board Clone()
    {
        var clone = new Board();
        foreach (var cell in AllCells())
            clone[cell.Row, cell.Col].Frog = cell.Frog;
        return clone;
    }
}