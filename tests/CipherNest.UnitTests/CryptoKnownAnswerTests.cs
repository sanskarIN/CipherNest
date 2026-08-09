using CipherNest.Infrastructure.Crypto;

namespace CipherNest.UnitTests;

public sealed class CryptoKnownAnswerTests
{
    [Fact]
    public void Argon2id_DefaultParameters_MatchKnownAnswer()
    {
        var service = new CryptoService();
        var salt = Enumerable.Range(0, 16).Select(static value => (byte)value).ToArray();

        var derived = service.DeriveKey("CipherNest known answer 2026!".AsSpan(), salt, CryptoService.DefaultKdf);

        Assert.Equal("fcb4490def165d2cd21b4ddc4ed5a7608bf668bc1ca9d3c3421875beea35c60f", Convert.ToHexString(derived).ToLowerInvariant());
    }
}
