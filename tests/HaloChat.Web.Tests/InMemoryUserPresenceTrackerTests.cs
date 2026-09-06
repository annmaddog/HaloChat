using HaloChat.Web.Services;
using Xunit;

namespace HaloChat.Web.Tests;

public class InMemoryUserPresenceTrackerTests
{
    [Fact]
    public void AddConnection_FirstConnectionForUser_ReturnsTrue()
    {
        var tracker = new InMemoryUserPresenceTracker();

        var wentOnline = tracker.AddConnection("user-1");

        Assert.True(wentOnline);
    }

    [Fact]
    public void AddConnection_SecondConnectionForSameUser_ReturnsFalse()
    {
        var tracker = new InMemoryUserPresenceTracker();
        tracker.AddConnection("user-1");

        var wentOnline = tracker.AddConnection("user-1");

        Assert.False(wentOnline);
    }

    [Fact]
    public void RemoveConnection_LastConnectionForUser_ReturnsTrue()
    {
        var tracker = new InMemoryUserPresenceTracker();
        tracker.AddConnection("user-1");

        var wentOffline = tracker.RemoveConnection("user-1");

        Assert.True(wentOffline);
    }

    [Fact]
    public void RemoveConnection_WhenTwoTabsOpen_OnlyReturnsTrueAfterBoth()
    {
        var tracker = new InMemoryUserPresenceTracker();
        tracker.AddConnection("user-1");
        tracker.AddConnection("user-1"); // 2 tab

        var afterFirstClose = tracker.RemoveConnection("user-1");
        var afterSecondClose = tracker.RemoveConnection("user-1");

        Assert.False(afterFirstClose);
        Assert.True(afterSecondClose);
    }

    [Fact]
    public void RemoveConnection_UserNeverAdded_ReturnsFalse()
    {
        var tracker = new InMemoryUserPresenceTracker();

        var result = tracker.RemoveConnection("ghost-user");

        Assert.False(result);
    }

    [Fact]
    public void GetOnlineUserIds_ReflectsCurrentlyConnectedUsers()
    {
        var tracker = new InMemoryUserPresenceTracker();
        tracker.AddConnection("user-1");
        tracker.AddConnection("user-2");
        tracker.RemoveConnection("user-2");

        var online = tracker.GetOnlineUserIds();

        Assert.Single(online);
        Assert.Contains("user-1", online);
    }
}
