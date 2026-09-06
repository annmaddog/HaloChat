using HaloChat.Security;
using Xunit;

namespace HaloChat.Security.Tests;

public class NullConversationKeyProviderTests
{
    [Fact]
    public void GetKey_ReturnsEmptyString()
    {
        var provider = new NullConversationKeyProvider();

        var key = provider.GetKey("user-a", "user-b");

        Assert.Equal(string.Empty, key);
    }
}
