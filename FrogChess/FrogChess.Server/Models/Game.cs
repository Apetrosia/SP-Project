namespace FrogChess.Server.Models;

public class Game
{
    public string Id { get; } = GenerateCustomId();
    private static string GenerateCustomId()
    {
        var guid = Guid.NewGuid().ToString("N");
        return $"{guid.Substring(0, 4)}-{guid.Substring(4, 4)}-{guid.Substring(8, 4)}";
    }
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