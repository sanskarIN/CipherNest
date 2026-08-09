using CipherNest.Application.Exceptions;
using CipherNest.Infrastructure.Crypto;

namespace CipherNest.UnitTests;

public sealed class KdfResourceBoundsTests
{
    private static readonly byte[] Salt = Enumerable.Range(0, 16).Select(static value => (byte)value).ToArray();

    [Theory]
    [InlineData(16383, 3, 1)]
    [InlineData(524289, 3, 1)]
    [InlineData(65536, 0, 1)]
    [InlineData(65536, 11, 1)]
    [InlineData(65536, 3, 0)]
    [InlineData(65536, 3, 17)]
    public void DeriveKey_RejectsOutOfBoundsKdfParameters(int memoryKiB, int iterations, int parallelism)
    {
        var service = new CryptoService();
        var parameters = new KdfParameters(memoryKiB, iterations, parallelism);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.DeriveKey("Long enough passphrase".AsSpan(), Salt, parameters));
    }

    [Fact]
    public void UnwrapKey_RejectsHostileParametersBeforeArgon2Allocation()
    {
        var service = new CryptoService();
        var valid = service.CreateWrappedKey("Very Strong Master Passphrase 2026!".AsSpan());
        var hostile = valid with { Kdf = new KdfParameters(CryptoService.MaximumKdfMemoryKiB + 1, 3, 1) };

        Assert.Throws<VaultAuthenticationException>(() => service.UnwrapKey("Very Strong Master Passphrase 2026!".AsSpan(), hostile));
    }
}
