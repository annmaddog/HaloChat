using HaloChat.Security;
using HaloChat.Web.Models;
using HaloChat.Web.Services;
using Xunit;

namespace HaloChat.Web.Tests;

public class ChatHistoryMapperTests
{
    [Fact]
    public void MapToViewModels_OrdersMessagesBySentTimeAscending()
    {
        var cipher = new PlaintextMessageCipher();
        var later = cipher.Encrypt("tin nhắn sau", key: "");
        var earlier = cipher.Encrypt("tin nhắn trước", key: "");
        var messages = new List<Message>
        {
            new() { SenderId = "a", ReceiverId = "b", CipherText = later.CipherText, Algorithm = later.Algorithm, SentAtUtc = new DateTime(2026, 1, 2) },
            new() { SenderId = "a", ReceiverId = "b", CipherText = earlier.CipherText, Algorithm = earlier.Algorithm, SentAtUtc = new DateTime(2026, 1, 1) }
        };

        var result = ChatHistoryMapper.MapToViewModels(messages, cipher, currentUserId: "a");

        Assert.Equal("tin nhắn trước", result[0].Content);
        Assert.Equal("tin nhắn sau", result[1].Content);
    }

    [Fact]
    public void MapToViewModels_SetsIsMineBasedOnSenderId()
    {
        var cipher = new PlaintextMessageCipher();
        var payload = cipher.Encrypt("hello", key: "");
        var messages = new List<Message>
        {
            new() { SenderId = "me", ReceiverId = "them", CipherText = payload.CipherText, Algorithm = payload.Algorithm, SentAtUtc = DateTime.UtcNow },
            new() { SenderId = "them", ReceiverId = "me", CipherText = payload.CipherText, Algorithm = payload.Algorithm, SentAtUtc = DateTime.UtcNow }
        };

        var result = ChatHistoryMapper.MapToViewModels(messages, cipher, currentUserId: "me");

        Assert.True(result[0].IsMine);
        Assert.False(result[1].IsMine);
    }

    [Fact]
    public void MapToViewModels_DecryptsContentUsingProvidedCipher()
    {
        var cipher = new PlaintextMessageCipher();
        var payload = cipher.Encrypt("Xin chào các bạn!", key: "");
        var messages = new List<Message>
        {
            new() { SenderId = "a", ReceiverId = "b", CipherText = payload.CipherText, Iv = payload.Iv, Tag = payload.Tag, Algorithm = payload.Algorithm, SentAtUtc = DateTime.UtcNow }
        };

        var result = ChatHistoryMapper.MapToViewModels(messages, cipher, currentUserId: "a");

        Assert.Equal("Xin chào các bạn!", result[0].Content);
    }
}
