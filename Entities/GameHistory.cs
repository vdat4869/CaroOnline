namespace Caro.Core.Entities;

public class GameHistory
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public string Event { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}


