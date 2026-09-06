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

    [Fact]
    public void MapToViewModels_WhenDecryptThrows_UsesPlaceholderAndStillDecryptsOtherMessages()
    {
        var badCipherText = new byte[] { 9, 9, 9 };
        var goodPayload = new PlaintextMessageCipher().Encrypt("tin nhắn ổn", key: "");
        var cipher = new ThrowingCipher(throwForCipherText: badCipherText);

        var messages = new List<Message>
        {
            new() { SenderId = "a", ReceiverId = "b", CipherText = badCipherText, Algorithm = "none", SentAtUtc = new DateTime(2026, 1, 1) },
            new() { SenderId = "a", ReceiverId = "b", CipherText = goodPayload.CipherText, Iv = goodPayload.Iv, Tag = goodPayload.Tag, Algorithm = goodPayload.Algorithm, SentAtUtc = new DateTime(2026, 1, 2) }
        };

        var result = ChatHistoryMapper.MapToViewModels(messages, cipher, currentUserId: "a");

        Assert.Equal("[không giải mã được]", result[0].Content);
        Assert.Equal("tin nhắn ổn", result[1].Content);
    }

    private sealed class ThrowingCipher : IMessageCipher
    {
        private readonly byte[] _throwForCipherText;

        public ThrowingCipher(byte[] throwForCipherText)
        {
            _throwForCipherText = throwForCipherText;
        }

        public EncryptedPayload Encrypt(string plaintext, string key)
            => new PlaintextMessageCipher().Encrypt(plaintext, key);

        public string Decrypt(EncryptedPayload payload, string key)
        {
            if (payload.CipherText.SequenceEqual(_throwForCipherText))
            {
                throw new InvalidOperationException("Không giải mã được — mô phỏng lỗi decrypt.");
            }

            return new PlaintextMessageCipher().Decrypt(payload, key);
        }
    }
}
