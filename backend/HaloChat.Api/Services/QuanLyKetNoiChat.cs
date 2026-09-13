using System.Collections.Concurrent;

namespace HaloChat.Api.Services;

public class QuanLyKetNoiChat : IQuanLyKetNoiChat
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _ketNoiTheoUser = new();
    private readonly object _khoa = new();

    public bool ThemKetNoi(string userId, string connectionId)
    {
        lock (_khoa)
        {
            var laOnlineMoi = !_ketNoiTheoUser.ContainsKey(userId) || _ketNoiTheoUser[userId].Count == 0;
            var tapHop = _ketNoiTheoUser.GetOrAdd(userId, _ => new HashSet<string>());
            tapHop.Add(connectionId);
            return laOnlineMoi;
        }
    }

    public bool XoaKetNoi(string userId, string connectionId)
    {
        lock (_khoa)
        {
            if (!_ketNoiTheoUser.TryGetValue(userId, out var tapHop))
            {
                return false;
            }

            tapHop.Remove(connectionId);
            if (tapHop.Count == 0)
            {
                _ketNoiTheoUser.TryRemove(userId, out _);
                return true;
            }

            return false;
        }
    }

    public bool DangOnline(string userId)
    {
        lock (_khoa)
        {
            return _ketNoiTheoUser.TryGetValue(userId, out var tapHop) && tapHop.Count > 0;
        }
    }

    public IReadOnlyCollection<string> LayConnectionIds(string userId)
    {
        lock (_khoa)
        {
            return _ketNoiTheoUser.TryGetValue(userId, out var tapHop) ? tapHop.ToList() : Array.Empty<string>();
        }
    }
}
