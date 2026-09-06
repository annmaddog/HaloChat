using System.Collections.Concurrent;

namespace HaloChat.Web.Services;

public class InMemoryUserPresenceTracker : IUserPresenceTracker
{
    private readonly ConcurrentDictionary<string, int> _connectionCounts = new();

    public bool AddConnection(string userId)
    {
        var newCount = _connectionCounts.AddOrUpdate(userId, 1, (_, count) => count + 1);
        return newCount == 1;
    }

    public bool RemoveConnection(string userId)
    {
        while (_connectionCounts.TryGetValue(userId, out var count))
        {
            var newCount = count - 1;
            if (newCount <= 0)
            {
                if (_connectionCounts.TryRemove(new KeyValuePair<string, int>(userId, count)))
                {
                    return true;
                }
            }
            else if (_connectionCounts.TryUpdate(userId, newCount, count))
            {
                return false;
            }
        }
        return false;
    }

    public IReadOnlyCollection<string> GetOnlineUserIds()
        => _connectionCounts.Keys.ToList();
}
