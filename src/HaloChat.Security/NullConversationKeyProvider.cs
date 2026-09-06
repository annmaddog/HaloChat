namespace HaloChat.Security;

/// <summary>
/// Cài đặt tạm thời — luôn trả về chuỗi rỗng vì PlaintextMessageCipher (giai
/// đoạn 1) bỏ qua tham số khóa. Nhóm bảo mật sẽ thay bằng cài đặt sinh khóa
/// thật (ví dụ HKDF) ở giai đoạn 2.
/// </summary>
public class NullConversationKeyProvider : IConversationKeyProvider
{
    public string GetKey(string userIdA, string userIdB) => string.Empty;
}
