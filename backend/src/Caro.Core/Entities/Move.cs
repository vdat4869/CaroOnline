namespace Caro.Core.Entities;

public class Move
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int Player { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int MoveNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}


