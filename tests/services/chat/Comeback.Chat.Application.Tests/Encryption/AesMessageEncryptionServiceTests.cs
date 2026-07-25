namespace Comeback.Chat.Application.Tests.Encryption;

using Comeback.Chat.Infrastructure.Encryption;
using FluentAssertions;
using Xunit;

public sealed class AesMessageEncryptionServiceTests
{
    private static string NewKey() => Convert.ToBase64String(new byte[32]); // 32-byte AES-256 key

    private readonly AesMessageEncryptionService _sut = new(NewKey());

    [Fact]
    public void Encrypt_ThenDecrypt_RoundTripsPlaintext()
    {
        var cipher = _sut.Encrypt("Zdravo svete");

        _sut.Decrypt(cipher).Should().Be("Zdravo svete");
    }

    [Fact]
    public void TryDecrypt_ValidCiphertext_ReturnsTrueAndPlaintext()
    {
        var cipher = _sut.Encrypt("poruka");

        _sut.TryDecrypt(cipher, out var plaintext).Should().BeTrue();
        plaintext.Should().Be("poruka");
    }

    [Fact]
    public void TryDecrypt_WithoutSeparator_ReturnsFalse()
    {
        // A legacy/plain value that is not in the "iv.cipher" format must not throw.
        _sut.TryDecrypt("not-encrypted", out var plaintext).Should().BeFalse();
        plaintext.Should().BeEmpty();
    }

    [Fact]
    public void TryDecrypt_MalformedIv_ReturnsFalse()
    {
        // Valid base64 parts but a wrong-sized IV — mirrors a corrupt row / key mismatch.
        _sut.TryDecrypt("AAAA.BBBB", out var plaintext).Should().BeFalse();
        plaintext.Should().BeEmpty();
    }

    [Fact]
    public void TryDecrypt_CiphertextFromDifferentKey_DoesNotThrow()
    {
        // Simulates an AES key rotation between deployments: the old ciphertext no longer decrypts.
        var otherKey = new AesMessageEncryptionService(Convert.ToBase64String(Enumerable.Repeat((byte)7, 32).ToArray()));
        var cipherFromOtherKey = otherKey.Encrypt("staro");

        var act = () => _sut.TryDecrypt(cipherFromOtherKey, out _);

        act.Should().NotThrow();
    }
}
