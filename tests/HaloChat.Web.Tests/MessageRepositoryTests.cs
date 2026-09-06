using HaloChat.Web.Data;
using HaloChat.Web.Models;
using HaloChat.Web.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HaloChat.Web.Tests;

public class MessageRepositoryTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SaveMessage_ThenQuery_ReturnsSameCipherTextBytes()
    {
        await using var db = CreateInMemoryContext();
        var message = new Message
        {
            SenderId = "user-a",
            ReceiverId = "user-b",
            CipherText = new byte[] { 1, 2, 3, 4 },
            Algorithm = "none",
            SentAtUtc = DateTime.UtcNow
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        var saved = await db.Messages.SingleAsync();
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, saved.CipherText);
        Assert.Equal("user-a", saved.SenderId);
    }

    [Fact]
    public async Task Query_ConversationBetweenTwoUsers_ExcludesOtherUsersMessages()
    {
        await using var db = CreateInMemoryContext();
        db.Messages.AddRange(
            new Message { SenderId = "a", ReceiverId = "b", CipherText = new byte[] { 1 }, Algorithm = "none", SentAtUtc = DateTime.UtcNow },
            new Message { SenderId = "b", ReceiverId = "a", CipherText = new byte[] { 2 }, Algorithm = "none", SentAtUtc = DateTime.UtcNow },
            new Message { SenderId = "a", ReceiverId = "c", CipherText = new byte[] { 3 }, Algorithm = "none", SentAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var conversation = await db.Messages
            .Where(MessageQueries.BetweenUsers("a", "b"))
            .ToListAsync();

        Assert.Equal(2, conversation.Count);
    }
}
