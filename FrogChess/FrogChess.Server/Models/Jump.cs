namespace FrogChess.Server.Models;

public record struct Jump(int FromRow, int FromCol, int ToRow, int ToCol);