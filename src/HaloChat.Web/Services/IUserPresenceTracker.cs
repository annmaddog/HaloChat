namespace HaloChat.Web.Services;

public interface IUserPresenceTracker
{
    /// <returns>true nếu đây là kết nối đầu tiên của user này (user vừa online)</returns>
    bool AddConnection(string userId);

    /// <returns>true nếu đây là kết nối cuối cùng của user này (user vừa offline)</returns>
    bool RemoveConnection(string userId);

    IReadOnlyCollection<string> GetOnlineUserIds();
}
