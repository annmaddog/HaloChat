namespace HaloChat.Security;

/// <summary>
/// Sinh khóa AES cho một cuộc trò chuyện (một cặp người dùng) — chỗ cắm cho
/// nhóm bảo mật (giai đoạn 2). Giai đoạn 1 dùng placeholder
/// <see cref="NullConversationKeyProvider"/>; nhóm bảo mật sẽ thay bằng cài
/// đặt sinh khóa thật (HKDF từ bí mật dùng chung — xem spec giai đoạn 2).
/// </summary>
public interface IConversationKeyProvider
{
    /// <summary>
    /// Trả về khóa (dạng chuỗi, ví dụ Base64) dùng chung cho cuộc trò chuyện
    /// giữa 2 người dùng. Thứ tự tham số không quan trọng:
    /// <c>GetKey(a, b)</c> phải luôn bằng <c>GetKey(b, a)</c>.
    /// </summary>
    string GetKey(string userIdA, string userIdB);
}
