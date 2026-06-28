namespace FrogChess.Server.Models;
public enum CellType { White, Swamp } 

public class Cell
{
    public int Row { get; init; }
    public int Col { get; init; }
    public CellType Type { get; init; }
    public PlayerColor? Frog { get; set; }   

    public bool IsEmpty => Frog == null;
    public bool IsWhite => Type == CellType.White;
    public bool IsSwamp => Type == CellType.Swamp;
}