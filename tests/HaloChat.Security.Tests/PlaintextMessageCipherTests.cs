using HaloChat.Security;
using Xunit;

namespace HaloChat.Security.Tests;

public class PlaintextMessageCipherTests
{
    [Fact]
    public void Encrypt_SetsAlgorithmToNone()
    {
        var cipher = new PlaintextMessageCipher();

        var result = cipher.Encrypt("hello", key: "");

        Assert.Equal("none", result.Algorithm);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext()
    {
        var cipher = new PlaintextMessageCipher();
        var original = "Xin chào, đây là tin nhắn test có dấu tiếng Việt";

        var encrypted = cipher.Encrypt(original, key: "");
        var decrypted = cipher.Decrypt(encrypted, key: "");

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_DoesNotSetIvOrTag()
    {
        var cipher = new PlaintextMessageCipher();

        var result = cipher.Encrypt("hello", key: "");

        Assert.Null(result.Iv);
        Assert.Null(result.Tag);
    }
}
