using HaloChat.Security;
using HaloChat.Web.Models;
using HaloChat.Web.ViewModels;

namespace HaloChat.Web.Services;

public static class ChatHistoryMapper
{
    public static List<MessageViewModel> MapToViewModels(
        IEnumerable<Message> messages,
        IMessageCipher cipher,
        string key,
        string currentUserId)
    {
        return messages
            .OrderBy(m => m.SentAtUtc)
            .Select(m => new MessageViewModel
            {
                Content = DecryptOrPlaceholder(m, cipher, key),
                IsMine = m.SenderId == currentUserId,
                SentAtUtc = m.SentAtUtc
            })
            .ToList();
    }

    private static string DecryptOrPlaceholder(Message m, IMessageCipher cipher, string key)
    {
        try
        {
            return cipher.Decrypt(new EncryptedPayload(m.CipherText, m.Iv, m.Tag, m.Algorithm), key);
        }
        catch
        {
            // Một tin nhắn cũ (giai đoạn 1, Algorithm = "none") có thể không giải mã được
            // bằng cipher thật ở giai đoạn 2 — không để một dòng lỗi làm sập cả trang.
            return "[không giải mã được]";
        }
    }
}
