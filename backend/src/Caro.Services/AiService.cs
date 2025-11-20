namespace Caro.Services;

public class AiService : IAiService
{
    private static readonly (int dx, int dy)[] Directions =
    {
        (1, 0), (0, 1), (1, 1), (1, -1)
    };

    public AiMove ChooseMove(int[,] board, int currentPlayer, AiDifficulty difficulty)
    {
        // Very simple placeholder:
        // - Easy: first empty
        // - Normal/Hard: pick near the center
        int size = board.GetLength(0);
        if (difficulty == AiDifficulty.Easy)
        {
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (board[x, y] == 0)
                        return new AiMove(x, y);
        }
        // naive center bias
        int cx = size / 2, cy = size / 2;
        int radius = 0;
        while (radius < size)
        {
            for (int y = Math.Max(0, cy - radius); y <= Math.Min(size - 1, cy + radius); y++)
            {
                for (int x = Math.Max(0, cx - radius); x <= Math.Min(size - 1, cx + radius); x++)
                {
                    if (board[x, y] == 0) return new AiMove(x, y);
                }
            }
            radius++;
        }
        return new AiMove(0, 0);
    }
}


