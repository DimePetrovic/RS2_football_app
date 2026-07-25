namespace Comeback.Chat.Application.Common.Interfaces;
public interface IMessageEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);

    /// <summary>
    /// Safe decrypt: returns false (instead of throwing) when the ciphertext is malformed
    /// or was encrypted with a different key, so a single bad row cannot fail a whole list query.
    /// </summary>
    bool TryDecrypt(string ciphertext, out string plaintext);
}
