using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class TotpAuditTests
{
    [Fact]
    public void Analyze_DoesNotClassifyTotpSeedsAsWeakOrReusedPasswords()
    {
        var seed = "JBSWY3DPEHPK3PXP";
        var first = new VaultItem
        {
            Id = Guid.NewGuid(),
            Type = VaultItemType.OneTimePassword,
            Title = "First OTP",
            Secret = seed
        };
        var second = first with { Id = Guid.NewGuid(), Title = "Second OTP" };

        var findings = new SecurityAuditService(new PasswordGenerator()).Analyze([first, second], DateTimeOffset.UtcNow);

        Assert.DoesNotContain(findings, finding => finding.Kind == SecurityFindingKind.WeakSecret);
        Assert.DoesNotContain(findings, finding => finding.Kind == SecurityFindingKind.ReusedSecret);
    }

    [Fact]
    public void Analyze_DuplicateTotpSignatureIncludesAlgorithmDigitsAndPeriod()
    {
        var baseline = new VaultItem
        {
            Id = Guid.NewGuid(),
            Type = VaultItemType.OneTimePassword,
            Title = "OTP",
            Secret = "JBSWY3DPEHPK3PXP",
            TotpAlgorithm = TotpAlgorithm.Sha1,
            TotpDigits = 6,
            TotpPeriodSeconds = 30
        };

        var differentPeriod = baseline with { Id = Guid.NewGuid(), TotpPeriodSeconds = 60 };
        var findings = new SecurityAuditService(new PasswordGenerator()).Analyze([baseline, differentPeriod], DateTimeOffset.UtcNow);

        Assert.DoesNotContain(findings, finding => finding.Kind == SecurityFindingKind.DuplicateEntry);
    }
}
