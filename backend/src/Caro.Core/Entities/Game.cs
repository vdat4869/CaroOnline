namespace Caro.Core.Entities;

public class Game
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Result { get; set; }
    public string Mode { get; set; } = "PvP"; // PvP or PvE
    public int? P1UserId { get; set; }
    public int? P2UserId { get; set; }
    public int? PveDifficulty { get; set; }
    public int TimeControlSeconds { get; set; }
    public int CurrentTurn { get; set; } = 1; // 1 or 2

    public List<Move> Moves { get; set; } = new();
    public List<GameHistory> History { get; set; } = new();
}


