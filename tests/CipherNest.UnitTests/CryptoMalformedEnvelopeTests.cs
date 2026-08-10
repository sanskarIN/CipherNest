using System.Security.Cryptography;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Exceptions;
using CipherNest.Infrastructure.Crypto;

namespace CipherNest.UnitTests;

public sealed class CryptoMalformedEnvelopeTests
{
    [Fact]
    public void Decrypt_RejectsNullRuntimeMembersWithoutNullReference()
    {
        var crypto = new CryptoService();
        var envelope = new EncryptedEnvelope(1, null!, null!, null!);
        var key = new byte[32];

        Assert.Throws<CryptographicException>(() => crypto.Decrypt(envelope, key, ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Unwrap_RejectsNullRuntimeMembersAsAuthenticationFailure()
    {
        var crypto = new CryptoService();
        var envelope = new WrappedKeyEnvelope(1, null!, null!, null!, null!, null!);

        Assert.Throws<VaultAuthenticationException>(() => crypto.UnwrapKey("valid-passphrase-value".AsSpan(), envelope));
    }

    [Fact]
    public void Unwrap_RejectsWrappedCiphertextWithWrongKeyLength()
    {
        var crypto = new CryptoService();
        var envelope = new WrappedKeyEnvelope(
            1,
            new byte[16],
            CryptoService.DefaultKdf,
            new byte[12],
            new byte[31],
            new byte[16]);

        Assert.Throws<VaultAuthenticationException>(() => crypto.UnwrapKey("valid-passphrase-value".AsSpan(), envelope));
    }
}
