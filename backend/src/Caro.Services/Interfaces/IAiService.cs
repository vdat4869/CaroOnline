namespace Caro.Services;

public enum AiDifficulty
{
    Easy = 1,
    Normal = 2,
    Hard = 3
}

public record AiMove(int X, int Y);

public interface IAiService
{
    AiMove ChooseMove(int[,] board, int currentPlayer, AiDifficulty difficulty);
}


