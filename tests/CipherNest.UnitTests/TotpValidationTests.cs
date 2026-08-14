using CipherNest.Application.Validation;
using CipherNest.Domain.Models;

namespace CipherNest.UnitTests;

public sealed class TotpValidationTests
{
    private const string ValidSeed = "JBSWY3DPEHPK3PXP";

    [Fact]
    public void ValidTotpItem_HasNoValidationErrors()
    {
        var item = new VaultItem
        {
            Id = Guid.NewGuid(),
            Type = VaultItemType.OneTimePassword,
            Title = "Example TOTP",
            Secret = ValidSeed,
            TotpAlgorithm = TotpAlgorithm.Sha1,
            TotpDigits = 6,
            TotpPeriodSeconds = 30
        };

        Assert.Empty(VaultItemValidator.Validate(item));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("NOT*VALID*BASE32")]
    [InlineData("AAAAAAAAAAAAAAAAA")]
    public void TotpItem_RejectsInvalidSeed(string seed)
    {
        var errors = VaultItemValidator.Validate(new VaultItem
        {
            Id = Guid.NewGuid(),
            Type = VaultItemType.OneTimePassword,
            Title = "Invalid TOTP",
            Secret = seed
        });

        Assert.Contains("TOTP seed or settings are invalid.", errors);
    }

    [Fact]
    public void TotpItem_RejectsUnsupportedAlgorithmDigitsAndPeriod()
    {
        var baseline = new VaultItem
        {
            Id = Guid.NewGuid(),
            Type = VaultItemType.OneTimePassword,
            Title = "TOTP",
            Secret = ValidSeed
        };

        Assert.Contains("TOTP seed or settings are invalid.", VaultItemValidator.Validate(baseline with { TotpAlgorithm = (TotpAlgorithm)999 }));
        Assert.Contains("TOTP seed or settings are invalid.", VaultItemValidator.Validate(baseline with { TotpDigits = 7 }));
        Assert.Contains("TOTP seed or settings are invalid.", VaultItemValidator.Validate(baseline with { TotpPeriodSeconds = 10 }));
        Assert.Contains("TOTP seed or settings are invalid.", VaultItemValidator.Validate(baseline with { TotpPeriodSeconds = 121 }));
    }

    [Fact]
    public void NonTotpItem_DoesNotRequireTotpSeed()
    {
        var errors = VaultItemValidator.Validate(new VaultItem
        {
            Id = Guid.NewGuid(),
            Type = VaultItemType.Login,
            Title = "Login",
            Secret = "ordinary-password"
        });

        Assert.DoesNotContain("TOTP seed or settings are invalid.", errors);
    }
}
