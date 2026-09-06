namespace HaloChat.Security;

/// <summary>
/// Giao diện mã hóa/giải mã tin nhắn — chỗ cắm cho nhóm bảo mật (giai đoạn 2).
///
/// Hợp đồng về tham số <c>key</c>: caller (ChatHub, ChatController qua
/// ChatHistoryMapper) luôn tính khóa bằng <see cref="IConversationKeyProvider"/>
/// rồi truyền vào đây — cài đặt IMessageCipher KHÔNG tự lấy khóa qua DI.
/// Giai đoạn 1 dùng placeholder <see cref="NullConversationKeyProvider"/>
/// (trả về chuỗi rỗng) vì PlaintextMessageCipher bỏ qua tham số này; giai
/// đoạn 2 chỉ cần thay 2 placeholder (IMessageCipher và
/// IConversationKeyProvider) bằng cài đặt AES thật — không phải sửa lại
/// ChatHub/ChatController.
/// </summary>
public interface IMessageCipher
{
    /// <summary>Mã hóa plaintext. Tham số <c>key</c>: xem ghi chú ở đầu interface.</summary>
    EncryptedPayload Encrypt(string plaintext, string key);

    /// <summary>Giải mã payload. Tham số <c>key</c>: xem ghi chú ở đầu interface.</summary>
    string Decrypt(EncryptedPayload payload, string key);
}
