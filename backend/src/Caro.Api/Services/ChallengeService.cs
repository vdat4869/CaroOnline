using System.Collections.Concurrent;

namespace Caro.Api.Services;

public record Challenge(Guid Id, int FromUserId, int ToUserId, DateTime CreatedAt);

public class ChallengeService
{
    private readonly ConcurrentDictionary<Guid, Challenge> _challenges = new();

    public Challenge Create(int fromUserId, int toUserId)
    {
        var c = new Challenge(Guid.NewGuid(), fromUserId, toUserId, DateTime.UtcNow);
        _challenges[c.Id] = c;
        return c;
    }

    public Challenge? Get(Guid id)
    {
        _challenges.TryGetValue(id, out var c);
        return c;
    }

    public bool Remove(Guid id) => _challenges.TryRemove(id, out _);
}


