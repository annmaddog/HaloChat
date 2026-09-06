using System.Text;

namespace HaloChat.Security;

/// <summary>
/// Cài đặt tạm thời — KHÔNG mã hóa gì cả, chỉ chuyển plaintext thành UTF-8 bytes.
/// Nhóm bảo mật sẽ thay bằng AesMessageCipher ở giai đoạn 2.
/// </summary>
public class PlaintextMessageCipher : IMessageCipher
{
    public EncryptedPayload Encrypt(string plaintext, string key)
        => new(Encoding.UTF8.GetBytes(plaintext), null, null, "none");

    public string Decrypt(EncryptedPayload payload, string key)
        => Encoding.UTF8.GetString(payload.CipherText);
}
