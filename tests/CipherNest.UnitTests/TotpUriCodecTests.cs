using CipherNest.Application.Models;
using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class TotpUriCodecTests
{
    private const string Secret = "JBSWY3DPEHPK3PXP";

    [Fact]
    public void Parse_ReadsCanonicalTotpUri()
    {
        var codec = new TotpUriCodec();

        var profile = codec.Parse("otpauth://totp/Acme%3Aalice%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=Acme&algorithm=SHA256&digits=8&period=60");

        Assert.Equal("alice@example.com", profile.AccountName);
        Assert.Equal("Acme", profile.Issuer);
        Assert.Equal(Secret, profile.Secret);
        Assert.Equal(TotpAlgorithm.Sha256, profile.Algorithm);
        Assert.Equal(8, profile.Digits);
        Assert.Equal(60, profile.PeriodSeconds);
    }

    [Fact]
    public void Parse_UsesStandardDefaultsAndLabelIssuer()
    {
        var profile = new TotpUriCodec().Parse("otpauth://totp/Example%3Aaccount?secret=JBSWY3DPEHPK3PXP");

        Assert.Equal("account", profile.AccountName);
        Assert.Equal("Example", profile.Issuer);
        Assert.Equal(TotpAlgorithm.Sha1, profile.Algorithm);
        Assert.Equal(6, profile.Digits);
        Assert.Equal(30, profile.PeriodSeconds);
    }

    [Fact]
    public void Format_ProducesRoundTrippableCanonicalUri()
    {
        var codec = new TotpUriCodec();
        var source = new TotpUriProfile("alice+vault@example.com", "Example Org", Secret, TotpAlgorithm.Sha512, 8, 45);

        var uri = codec.Format(source);
        var parsed = codec.Parse(uri);

        Assert.True(uri.StartsWith("otpauth://totp/", StringComparison.Ordinal));
        Assert.Contains("secret=JBSWY3DPEHPK3PXP", uri, StringComparison.Ordinal);
        Assert.Contains("algorithm=SHA512", uri, StringComparison.Ordinal);
        Assert.Contains("digits=8", uri, StringComparison.Ordinal);
        Assert.Contains("period=45", uri, StringComparison.Ordinal);
        Assert.Equal(source, parsed);
    }

    [Theory]
    [InlineData("https://totp/Example?secret=JBSWY3DPEHPK3PXP")]
    [InlineData("otpauth://hotp/Example?secret=JBSWY3DPEHPK3PXP&counter=1")]
    [InlineData("otpauth://totp/Example")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&secret=JBSWY3DPEHPK3PXP")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&counter=1")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&digits=7")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&period=5")]
    [InlineData("otpauth://totp/IssuerA%3Aaccount?secret=JBSWY3DPEHPK3PXP&issuer=IssuerB")]
    public void Parse_RejectsUnsupportedOrAmbiguousInputs(string input)
    {
        Assert.Throws<ArgumentException>(() => new TotpUriCodec().Parse(input));
    }

    [Fact]
    public void Parse_RejectsExcessiveUriAndQueryCounts()
    {
        var codec = new TotpUriCodec();
        Assert.Throws<ArgumentException>(() => codec.Parse(new string('a', TotpUriCodec.MaximumUriCharacters + 1)));

        var extra = string.Join('&', Enumerable.Range(0, TotpUriCodec.MaximumQueryPairs).Select(index => $"x{index}=1"));
        var uri = $"otpauth://totp/Example?secret={Secret}&{extra}";
        Assert.Throws<ArgumentException>(() => codec.Parse(uri));
    }

    [Fact]
    public void Parse_RejectsControlAndFormattingCharactersInDisplayMetadata()
    {
        var codec = new TotpUriCodec();

        Assert.Throws<ArgumentException>(() => codec.Parse($"otpauth://totp/Bad%0AName?secret={Secret}"));
        Assert.Throws<ArgumentException>(() => codec.Parse($"otpauth://totp/%E2%80%AEaccount?secret={Secret}"));
    }

    [Fact]
    public void Format_RejectsInvalidSecretAndEmptyAccount()
    {
        var codec = new TotpUriCodec();

        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile("account", "Issuer", "not-a-valid-secret", TotpAlgorithm.Sha1, 6, 30)));
        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile(" ", "Issuer", Secret, TotpAlgorithm.Sha1, 6, 30)));
    }
}
