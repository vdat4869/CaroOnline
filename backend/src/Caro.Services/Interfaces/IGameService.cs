using Caro.Core.Entities;

namespace Caro.Services;

public record CreateGameOptions(string Mode, int? P1UserId, int? P2UserId, int? PveDifficulty, int TimeControlSeconds);

public interface IGameService
{
    Task<Game> CreateGameAsync(CreateGameOptions options, CancellationToken ct = default);
    Task<Move> MakeMoveAsync(int gameId, int player, int x, int y, CancellationToken ct = default);
    Task<bool> ResignAsync(int gameId, int player, CancellationToken ct = default);
    Task<Game?> GetGameAsync(int id, CancellationToken ct = default);
    Task<List<Game>> GetGamesAsync(CancellationToken ct = default);
    Task<List<Move>> GetMovesAsync(int gameId, CancellationToken ct = default);
}


