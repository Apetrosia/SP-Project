namespace FrogChess.Server.Models;

public class Game
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public Board Board { get; private set; } = new();
    public Player? Green { get; set; }
    public Player? Red { get; set; }
    public PlayerColor CurrentTurn { get; set; } = PlayerColor.Green;
    public bool Started { get; set; }
    public bool Finished { get; set; }
    public PlayerColor? Winner { get; set; }
    public int ConsecutivePasses { get; set; }
    public PlayerColor? LastJumpPlayer { get; set; }

    
    public bool GreenHasJumped { get; set; }
    public bool RedHasJumped { get; set; }

    
    public bool GreenRemovedFrog { get; set; }
    public bool RedRemovedFrog { get; set; }
}