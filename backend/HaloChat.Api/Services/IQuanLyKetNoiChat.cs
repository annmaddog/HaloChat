namespace HaloChat.Api.Services;

/// <summary>
/// Theo dõi các connectionId SignalR đang mở của từng user (1 user có thể
/// mở nhiều tab/thiết bị cùng lúc). Dùng cho: (a) presence online/offline
/// (user online khi có >=1 connection), (b) join/gỡ SignalR Group cho các
/// connection cụ thể khi thành viên nhóm thay đổi (Groups.AddToGroupAsync/
/// RemoveFromGroupAsync đòi connectionId, không chỉ userId).
/// </summary>
public interface IQuanLyKetNoiChat
{
    /// <summary>Ghi nhận 1 connection mới. Trả về true nếu đây là connection ĐẦU TIÊN của user (vừa online).</summary>
    bool ThemKetNoi(string userId, string connectionId);

    /// <summary>Gỡ 1 connection. Trả về true nếu đây là connection CUỐI CÙNG bị gỡ (vừa offline).</summary>
    bool XoaKetNoi(string userId, string connectionId);

    bool DangOnline(string userId);

    /// <summary>Toàn bộ connectionId đang mở của 1 user (rỗng nếu offline).</summary>
    IReadOnlyCollection<string> LayConnectionIds(string userId);
}
