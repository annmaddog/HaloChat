namespace HaloChat.Security;

public interface IMessageCipher
{
    EncryptedPayload Encrypt(string plaintext, string key);
    string Decrypt(EncryptedPayload payload, string key);
}
