namespace FrogChess.Server.Models;

public class Player
{
    public string Name { get; init; } = string.Empty;
    public PlayerColor Color { get; init; }
    public string? ConnectionId { get; set; }
    public bool IsConnected { get; set; }
}