namespace Comeback.Chat.Application.Common.Interfaces;
public interface IMessageEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
