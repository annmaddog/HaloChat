namespace HaloChat.Security;

public record EncryptedPayload(byte[] CipherText, byte[]? Iv, byte[]? Tag, string Algorithm);
