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
    public void Parse_UsesExplicitIssuerWhenLabelHasNoIssuer()
    {
        var profile = new TotpUriCodec().Parse("otpauth://totp/alice%2Bvault%40example.com?secret=JBSWY3DPEHPK3PXP&issuer=Example+Org");

        Assert.Equal("alice+vault@example.com", profile.AccountName);
        Assert.Equal("Example Org", profile.Issuer);
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

        Assert.True(uri.StartsWith("otpauth://totp/", StringComparison.Ordinal));
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

        Assert.True(uri.StartsWith("otpauth://totp/account%40example.com?", StringComparison.Ordinal));
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
    [InlineData("otpauth://totp/IssuerA%3Aaccount?secret=JBSWY3DPEHPK3PXP&issuer=IssuerB")]
    [InlineData("otpauth://user@totp/Example?secret=JBSWY3DPEHPK3PXP")]
    [InlineData("otpauth://totp:123/Example?secret=JBSWY3DPEHPK3PXP")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP#fragment")]
    [InlineData("otpauth://totp/folder/Example?secret=JBSWY3DPEHPK3PXP")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&broken")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&=value")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&bad%ZZ=value")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&issuer=Bad%ZZIssuer")]
    [InlineData("otpauth://totp/%3Aaccount?secret=JBSWY3DPEHPK3PXP")]
    [InlineData("otpauth://totp/Issuer%3Aaccount%3Aextra?secret=JBSWY3DPEHPK3PXP&issuer=Issuer")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&&issuer=Example")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&issuer=Example&")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&future=bad%ZZvalue")]
    [InlineData("otpauth://totp/Example?secret=JBSWY3DPEHPK3PXP&future=bad%0Avalue")]
    public void Parse_RejectsUnsupportedOrAmbiguousInputs(string input)
    {
        Assert.Throws<ArgumentException>(() => new TotpUriCodec().Parse(input));
    }

    [Fact]
    public void Parse_EnforcesQueryNameBound()
    {
        var codec = new TotpUriCodec();
        var overlongName = new string('x', 65);

        Assert.Throws<ArgumentException>(() => codec.Parse($"otpauth://totp/Example?secret={Secret}&{overlongName}=1"));
    }

    [Fact]
    public void Parse_AcceptsExactQueryPairBound()
    {
        var codec = new TotpUriCodec();
        var extras = string.Join('&', Enumerable.Range(0, TotpUriCodec.MaximumQueryPairs - 1).Select(index => $"x{index}=1"));
        var profile = codec.Parse($"otpauth://totp/Example?secret={Secret}&{extras}");

        Assert.Equal("Example", profile.AccountName);
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
    public void Parse_AcceptsExactAccountAndIssuerBounds()
    {
        var codec = new TotpUriCodec();
        var account = new string('a', TotpUriCodec.MaximumAccountNameCharacters);
        var issuer = new string('i', TotpUriCodec.MaximumIssuerCharacters);

        var profile = codec.Parse($"otpauth://totp/{issuer}%3A{account}?secret={Secret}&issuer={issuer}");

        Assert.Equal(account, profile.AccountName);
        Assert.Equal(issuer, profile.Issuer);
    }

    [Fact]
    public void Parse_RejectsAccountAndIssuerAboveBounds()
    {
        var codec = new TotpUriCodec();
        var account = new string('a', TotpUriCodec.MaximumAccountNameCharacters + 1);
        var issuer = new string('i', TotpUriCodec.MaximumIssuerCharacters + 1);

        Assert.Throws<ArgumentException>(() => codec.Parse($"otpauth://totp/{account}?secret={Secret}"));
        Assert.Throws<ArgumentException>(() => codec.Parse($"otpauth://totp/{issuer}%3Aaccount?secret={Secret}&issuer={issuer}"));
    }

    [Fact]
    public void Parse_RejectsControlAndFormattingCharactersInDisplayMetadata()
    {
        var codec = new TotpUriCodec();

        Assert.Throws<ArgumentException>(() => codec.Parse($"otpauth://totp/Bad%0AName?secret={Secret}"));
        Assert.Throws<ArgumentException>(() => codec.Parse($"otpauth://totp/%E2%80%AEaccount?secret={Secret}"));
        Assert.Throws<ArgumentException>(() => codec.Parse($"otpauth://totp/account?secret={Secret}&issuer=Bad%0DIssuer"));
    }

    [Fact]
    public void Format_RejectsInvalidSecretSettingsAndMetadata()
    {
        var codec = new TotpUriCodec();

        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile("account", "Issuer", "not-a-valid-secret", TotpAlgorithm.Sha1, 6, 30)));
        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile(" ", "Issuer", Secret, TotpAlgorithm.Sha1, 6, 30)));
        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile(new string('a', TotpUriCodec.MaximumAccountNameCharacters + 1), "Issuer", Secret, TotpAlgorithm.Sha1, 6, 30)));
        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile("account", new string('i', TotpUriCodec.MaximumIssuerCharacters + 1), Secret, TotpAlgorithm.Sha1, 6, 30)));
        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile("account", "Issuer", Secret, TotpAlgorithm.Sha1, 7, 30)));
        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile("account", "Issuer", Secret, TotpAlgorithm.Sha1, 6, 121)));
        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile("account", "Bad\u202EIssuer", Secret, TotpAlgorithm.Sha1, 6, 30)));
        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile("account:extra", "Issuer", Secret, TotpAlgorithm.Sha1, 6, 30)));
        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile("account", "Issuer:extra", Secret, TotpAlgorithm.Sha1, 6, 30)));
        Assert.Throws<ArgumentException>(() => codec.Format(new TotpUriProfile("account", "Issuer", Secret, (TotpAlgorithm)99, 6, 30)));
    }

    [Fact]
    public void Format_RejectsEncodedOutputAboveUriCeiling()
    {
        var codec = new TotpUriCodec();
        var account = new string('界', TotpUriCodec.MaximumAccountNameCharacters);
        var issuer = new string('界', TotpUriCodec.MaximumIssuerCharacters);
        var profile = new TotpUriProfile(account, issuer, Secret, TotpAlgorithm.Sha1, 6, 30);

        Assert.Throws<ArgumentException>(() => codec.Format(profile));
    }
}
