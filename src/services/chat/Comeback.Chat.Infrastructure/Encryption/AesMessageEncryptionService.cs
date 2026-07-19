namespace Comeback.Chat.Infrastructure.Encryption;

using System.Security.Cryptography;
using System.Text;
using Comeback.Chat.Application.Common.Interfaces;

public sealed class AesMessageEncryptionService : IMessageEncryptionService
{
    private readonly byte[] _key;

    public AesMessageEncryptionService(string base64Key)
        => _key = Convert.FromBase64String(base64Key);

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(aes.IV) + "." + Convert.ToBase64String(cipherBytes);
    }

    public string Decrypt(string ciphertext)
    {
        var parts = ciphertext.Split('.', 2);
        var iv = Convert.FromBase64String(parts[0]);
        var cipherBytes = Convert.FromBase64String(parts[1]);
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
