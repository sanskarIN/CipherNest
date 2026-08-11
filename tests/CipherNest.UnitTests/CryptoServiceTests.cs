using System.Security.Cryptography;
using CipherNest.Application.Exceptions;
using CipherNest.Infrastructure.Crypto;

namespace CipherNest.UnitTests;

public sealed class CryptoServiceTests
{
    private readonly CryptoService _crypto = new();

    [Fact]
    public void WrappedKey_RoundTrips_WithCorrectPassphrase()
    {
        const string passphrase = "correct horse battery staple 2026";
        var wrapped = _crypto.CreateWrappedKey(passphrase);
        var key = _crypto.UnwrapKey(passphrase, wrapped);
        try { Assert.Equal(32, key.Length); }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Fact]
    public void WrappedKey_RejectsWrongPassphrase()
    {
        var wrapped = _crypto.CreateWrappedKey("correct horse battery staple 2026");
        Assert.Throws<VaultAuthenticationException>(() => _crypto.UnwrapKey("different strong passphrase 2026", wrapped));
    }

    [Fact]
    public void WrappedKey_RejectsTooShortAttemptAsAuthenticationFailure()
    {
        var wrapped = _crypto.CreateWrappedKey("correct horse battery staple 2026");
        Assert.Throws<VaultAuthenticationException>(() => _crypto.UnwrapKey("short", wrapped));
    }

    [Fact]
    public void WrappedKey_RejectsOversizedAttemptAsAuthenticationFailure()
    {
        var wrapped = _crypto.CreateWrappedKey("correct horse battery staple 2026");
        var attempt = new string('x', CryptoService.MaximumPassphraseCharacters + 1);
        Assert.Throws<VaultAuthenticationException>(() => _crypto.UnwrapKey(attempt, wrapped));
    }

    [Fact]
    public void RecordEnvelope_RejectsTampering()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            var plain = "classified test value"u8.ToArray();
            var aad = Guid.NewGuid().ToByteArray();
            var envelope = _crypto.Encrypt(plain, key, aad);
            envelope.Ciphertext[0] ^= 0x01;
            Assert.Throws<AuthenticationTagMismatchException>(() => _crypto.Decrypt(envelope, key, aad));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [Fact]
    public void AssociatedData_IsAuthenticated()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            var envelope = _crypto.Encrypt("value"u8, key, "item-a"u8);
            Assert.Throws<AuthenticationTagMismatchException>(() => _crypto.Decrypt(envelope, key, "item-b"u8));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}
