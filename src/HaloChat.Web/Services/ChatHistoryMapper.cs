using HaloChat.Security;
using HaloChat.Web.Models;
using HaloChat.Web.ViewModels;

namespace HaloChat.Web.Services;

public static class ChatHistoryMapper
{
    public static List<MessageViewModel> MapToViewModels(
        IEnumerable<Message> messages,
        IMessageCipher cipher,
        string currentUserId)
    {
        return messages
            .OrderBy(m => m.SentAtUtc)
            .Select(m => new MessageViewModel
            {
                // key rỗng — placeholder, nhóm bảo mật sẽ thay bằng khóa thật khi cắm AES vào
                Content = cipher.Decrypt(new EncryptedPayload(m.CipherText, m.Iv, m.Tag, m.Algorithm), key: string.Empty),
                IsMine = m.SenderId == currentUserId,
                SentAtUtc = m.SentAtUtc
            })
            .ToList();
    }
}
