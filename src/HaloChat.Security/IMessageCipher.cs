namespace HaloChat.Security;

/// <summary>
/// Giao diện mã hóa/giải mã tin nhắn — chỗ cắm cho nhóm bảo mật (giai đoạn 2).
///
/// Hợp đồng về tham số <c>key</c>: interface này KHÔNG quy định cách lấy khóa.
/// Giai đoạn 1 (PlaintextMessageCipher) bỏ qua tham số này hoàn toàn. Cài đặt
/// AES thật ở giai đoạn 2 nên tự lấy khóa qua một service tiêm vào (DI) —
/// KHÔNG nên yêu cầu caller (ChatHub, ChatHistoryMapper) truyền khóa thật vào
/// tham số này, vì cả hai nơi gọi hiện tại luôn truyền <c>string.Empty</c> và
/// sẽ không được sửa lại khi giai đoạn 2 triển khai.
/// </summary>
public interface IMessageCipher
{
    /// <summary>Mã hóa plaintext. Tham số <c>key</c>: xem ghi chú ở đầu interface.</summary>
    EncryptedPayload Encrypt(string plaintext, string key);

    /// <summary>Giải mã payload. Tham số <c>key</c>: xem ghi chú ở đầu interface.</summary>
    string Decrypt(EncryptedPayload payload, string key);
}
