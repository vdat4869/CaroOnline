using Caro.Core.Entities;
using Caro.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caro.Services;

public class GameService : IGameService
{
    private const int BoardSize = 15;
    private readonly CaroDbContext _db;

    public GameService(CaroDbContext db)
    {
        _db = db;
    }

    public async Task<Game> CreateGameAsync(CreateGameOptions options, CancellationToken ct = default)
    {
        var game = new Game
        {
            CreatedAt = DateTime.UtcNow,
            Mode = options.Mode,
            P1UserId = options.P1UserId,
            P2UserId = options.P2UserId,
            PveDifficulty = options.PveDifficulty,
            TimeControlSeconds = options.TimeControlSeconds,
            CurrentTurn = 1
        };
        _db.Games.Add(game);
        await _db.SaveChangesAsync(ct);
        return game;
    }

    public async Task<Move> MakeMoveAsync(int gameId, int player, int x, int y, CancellationToken ct = default)
    {
        var game = await _db.Games.Include(g => g.Moves).FirstOrDefaultAsync(g => g.Id == gameId, ct)
                   ?? throw new InvalidOperationException("Game not found");

        ValidateMove(game, player, x, y);

        var move = new Move
        {
            GameId = gameId,
            Player = player,
            X = x,
            Y = y,
            MoveNumber = game.Moves.Count + 1,
            CreatedAt = DateTime.UtcNow
        };

        _db.Moves.Add(move);
        game.Moves.Add(move);
        
        // Check win condition
        if (CheckWin(game.Moves, player, x, y))
        {
            game.FinishedAt = DateTime.UtcNow;
            game.Result = player == 1 ? "P1_WIN" : "P2_WIN";
        }
        else
        {
            game.CurrentTurn = player == 1 ? 2 : 1;
        }

        await _db.SaveChangesAsync(ct);
        return move;
    }

    private static bool CheckWin(List<Move> moves, int player, int lastX, int lastY)
    {
        // Check 4 directions: horizontal, vertical, diagonal (\), diagonal (/)
        var directions = new[] { (0, 1), (1, 0), (1, 1), (1, -1) };
        
        foreach (var (dx, dy) in directions)
        {
            int count = 1; // Count includes the last move
            
            // Check forward direction
            for (int i = 1; i < 5; i++)
            {
                int x = lastX + dx * i;
                int y = lastY + dy * i;
                if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize) break;
                if (moves.Any(m => m.X == x && m.Y == y && m.Player == player))
                    count++;
                else
                    break;
            }
            
            // Check backward direction
            for (int i = 1; i < 5; i++)
            {
                int x = lastX - dx * i;
                int y = lastY - dy * i;
                if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize) break;
                if (moves.Any(m => m.X == x && m.Y == y && m.Player == player))
                    count++;
                else
                    break;
            }
            
            if (count >= 5) return true;
        }
        
        return false;
    }

    public async Task<bool> ResignAsync(int gameId, int player, CancellationToken ct = default)
    {
        var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == gameId, ct)
                   ?? throw new InvalidOperationException("Game not found");
        game.FinishedAt = DateTime.UtcNow;
        game.Result = player == 1 ? "P2_WIN_RESIGN" : "P1_WIN_RESIGN";
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<Game?> GetGameAsync(int id, CancellationToken ct = default)
    {
        return _db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    public Task<List<Game>> GetGamesAsync(CancellationToken ct = default)
    {
        return _db.Games.AsNoTracking().OrderByDescending(g => g.Id).Take(100).ToListAsync(ct);
    }

    public Task<List<Move>> GetMovesAsync(int gameId, CancellationToken ct = default)
    {
        return _db.Moves.AsNoTracking().Where(m => m.GameId == gameId).OrderBy(m => m.MoveNumber).ToListAsync(ct);
    }

    private static void ValidateMove(Game game, int player, int x, int y)
    {
        if (game.FinishedAt != null) throw new InvalidOperationException("Game already finished");
        if (player != game.CurrentTurn) throw new InvalidOperationException("Not your turn");
        if (x < 0 || y < 0 || x >= BoardSize || y >= BoardSize) throw new InvalidOperationException("Out of bounds");

        var occupied = game.Moves.Any(m => m.X == x && m.Y == y);
        if (occupied) throw new InvalidOperationException("Cell already occupied");
    }
}


