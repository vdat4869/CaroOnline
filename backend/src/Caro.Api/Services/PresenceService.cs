using System.Collections.Concurrent;

namespace Caro.Api.Services;

public class PresenceService
{
    private readonly ConcurrentDictionary<string, int> _connectionIdToUserId = new();
    private readonly ConcurrentDictionary<int, HashSet<string>> _userIdToConnections = new();

    public void OnConnected(int userId, string connectionId)
    {
        _connectionIdToUserId[connectionId] = userId;
        _userIdToConnections.AddOrUpdate(userId, _ => new HashSet<string> { connectionId }, (_, set) => { lock (set) set.Add(connectionId); return set; });
    }

    public void OnDisconnected(string connectionId)
    {
        if (_connectionIdToUserId.TryRemove(connectionId, out var userId))
        {
            if (_userIdToConnections.TryGetValue(userId, out var set))
            {
                lock (set) set.Remove(connectionId);
                if (set.Count == 0)
                {
                    _userIdToConnections.TryRemove(userId, out _);
                }
            }
        }
    }

    public int[] GetOnlineUserIds() => _userIdToConnections.Keys.ToArray();

    public IReadOnlyCollection<string> GetConnectionsForUser(int userId)
    {
        if (_userIdToConnections.TryGetValue(userId, out var set)) return set;
        return Array.Empty<string>();
    }
}


