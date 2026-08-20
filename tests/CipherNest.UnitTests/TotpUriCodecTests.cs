using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class TotpUriCodecTests
{
    private const string Secret = "JBSWY3DPEHPK3PXP";

    [Fact]
    public void Parse_ValidCanonicalUri_ReturnsProfile()
    {
        var codec = new TotpUriCodec();

        var profile = codec.Parse(
            "otpauth://totp/Example%20Org:alice%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=Example%20Org&algorithm=SHA256&digits=8&period=45");

        Assert.Equal("alice@example.com", profile.AccountName);
        Assert.Equal("Example Org", profile.Issuer);
        Assert.Equal(Secret, profile.Secret);
        Assert.Equal(TotpAlgorithm.Sha256, profile.Algorithm);
        Assert.Equal(8, profile.Digits);
        Assert.Equal(45, profile.PeriodSeconds);
    }

    [Fact]
    public void Parse_UsesIssuerFromLabelWhenQueryIssuerMissing()
    {
        var profile = new TotpUriCodec().Parse(
            "otpauth://totp/Example%20Org:alice%40example.com?secret=JBSWY3DPEHPK3PXP");

        Assert.Equal("Example Org", profile.Issuer);
        Assert.Equal("alice@example.com", profile.AccountName);
    }

    [Fact]
    public void Parse_RejectsIssuerMismatch()
    {
        var codec = new TotpUriCodec();

        Assert.Throws<FormatException>(() => codec.Parse(
            "otpauth://totp/Example%20Org:alice?secret=JBSWY3DPEHPK3PXP&issuer=Other"));
    }

    [Fact]
    public void Parse_AllowsUnknownWellFormedParametersWithinBound()
    {
        var parameterName = new string('x', 64);
        var profile = new TotpUriCodec().Parse($"otpauth://totp/account?secret={Secret}&{parameterName}=future-value");

        Assert.Equal("account", profile.AccountName);
        Assert.Equal(Secret, profile.Secret);
    }

    [Fact]
    public void Format_ProducesRoundTrippableCanonicalUri()
    {
        var codec = new TotpUriCodec();
        var source = new TotpUriProfile("alice+vault@example.com", "Example Org", Secret, TotpAlgorithm.Sha512, 8, 45);

        var uri = codec.Format(source);
        var parsed = codec.Parse(uri);

        Assert.StartsWith("otpauth://totp/", uri, StringComparison.Ordinal);
        Assert.Contains("secret=JBSWY3DPEHPK3PXP", uri, StringComparison.Ordinal);
        Assert.Contains("issuer=Example%20Org", uri, StringComparison.Ordinal);
        Assert.Contains("algorithm=SHA512", uri, StringComparison.Ordinal);
        Assert.Contains("digits=8", uri, StringComparison.Ordinal);
        Assert.Contains("period=45", uri, StringComparison.Ordinal);
        Assert.Equal(source, parsed);
    }

    [Fact]
    public void Format_WithoutIssuer_UsesAccountOnlyLabel()
    {
        var codec = new TotpUriCodec();
        var uri = codec.Format(new TotpUriProfile("account@example.com", string.Empty, Secret, TotpAlgorithm.Sha1, 6, 30));

        Assert.StartsWith("otpauth://totp/account%40example.com?", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("&issuer=", uri, StringComparison.Ordinal);
        Assert.Equal(string.Empty, codec.Parse(uri).Issuer);
    }

    [Theory]
    [InlineData("https://totp/Example?secret=JBSWY3DPEHPK3PXP")]
    [InlineData("otpauth://hotp/Example?secret=JBSWY3DPEHPK3PXP&counter=1")]
    [InlineData("otpauth://totp/Example")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&secret=JBSWY3DPEHPK3PXP")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&SECRET=JBSWY3DPEHPK3PXP")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&counter=1")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&algorithm=SHA3")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&digits=7")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&period=5")]
    public void Parse_InvalidUri_Throws(string uri)
    {
        Assert.Throws<FormatException>(() => new TotpUriCodec().Parse(uri));
    }
}
