using CipherNest.Infrastructure.Crypto;

namespace CipherNest.UnitTests;

public sealed class CryptoPassphraseBoundsTests
{
    [Fact]
    public void WrappedKey_RejectsPassphraseBelowMinimumBeforeKdfWork()
    {
        var crypto = new CryptoService();
        var passphrase = new string('a', CryptoService.MinimumPassphraseCharacters - 1);

        Assert.Throws<ArgumentException>(() => crypto.CreateWrappedKey(passphrase.AsSpan()));
    }

    [Fact]
    public void WrappedKey_RejectsPassphraseAboveMaximumBeforeKdfWork()
    {
        var crypto = new CryptoService();
        var passphrase = new string('a', CryptoService.MaximumPassphraseCharacters + 1);

        Assert.Throws<ArgumentException>(() => crypto.CreateWrappedKey(passphrase.AsSpan()));
    }

    [Fact]
    public void WrappedKey_RejectsWhitespaceOnlyPassphraseBeforeKdfWork()
    {
        var crypto = new CryptoService();
        var passphrase = new string(' ', CryptoService.MinimumPassphraseCharacters);

        Assert.Throws<ArgumentException>(() => crypto.CreateWrappedKey(passphrase.AsSpan()));
    }
}
