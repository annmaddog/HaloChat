using HaloChat.Security;
using HaloChat.Web.Data;
using HaloChat.Web.Models;
using HaloChat.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace HaloChat.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _db;
    private readonly IMessageCipher _cipher;
    private readonly IConversationKeyProvider _keyProvider;
    private readonly IUserPresenceTracker _presence;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatHub(
        ApplicationDbContext db,
        IMessageCipher cipher,
        IConversationKeyProvider keyProvider,
        IUserPresenceTracker presence,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _cipher = cipher;
        _keyProvider = keyProvider;
        _presence = presence;
        _userManager = userManager;
    }

    private string CurrentUserId => Context.UserIdentifier
        ?? throw new InvalidOperationException("Kết nối SignalR không có UserIdentifier.");

    public override async Task OnConnectedAsync()
    {
        var becameOnline = _presence.AddConnection(CurrentUserId);
        if (becameOnline)
        {
            await Clients.All.SendAsync("PresenceChanged", CurrentUserId, true);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var becameOffline = _presence.RemoveConnection(CurrentUserId);
        if (becameOffline)
        {
            await Clients.All.SendAsync("PresenceChanged", CurrentUserId, false);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public IEnumerable<string> GetOnlineUsers() => _presence.GetOnlineUserIds();

    public async Task SendMessage(string receiverId, string content)
    {
        try
        {
            var senderId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Tin nhắn không được để trống.");
            }

            if (receiverId == senderId)
            {
                throw new InvalidOperationException("Không thể tự gửi tin nhắn cho chính mình.");
            }

            var receiver = await _userManager.FindByIdAsync(receiverId);
            if (receiver is null)
            {
                throw new InvalidOperationException("Người nhận không tồn tại.");
            }

            var key = _keyProvider.GetKey(senderId, receiverId);
            var payload = _cipher.Encrypt(content, key);

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                CipherText = payload.CipherText,
                Iv = payload.Iv,
                Tag = payload.Tag,
                Algorithm = payload.Algorithm,
                SentAtUtc = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            var outgoing = new
            {
                senderId,
                receiverId,
                content,
                sentAtUtc = message.SentAtUtc
            };

            await Clients.User(senderId).SendAsync("ReceiveMessage", outgoing);
            await Clients.User(receiverId).SendAsync("ReceiveMessage", outgoing);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("SendFailed", ex.Message);
        }
    }
}
