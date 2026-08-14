using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class TotpServiceTests
{
    private const string Sha1Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
    private const string Sha256Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA====";
    private const string Sha512Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNA=";

    [Theory]
    [InlineData(59L, TotpAlgorithm.Sha1, Sha1Secret, "94287082")]
    [InlineData(59L, TotpAlgorithm.Sha256, Sha256Secret, "46119246")]
    [InlineData(59L, TotpAlgorithm.Sha512, Sha512Secret, "90693936")]
    [InlineData(1111111109L, TotpAlgorithm.Sha1, Sha1Secret, "07081804")]
    [InlineData(1111111109L, TotpAlgorithm.Sha256, Sha256Secret, "68084774")]
    [InlineData(1111111109L, TotpAlgorithm.Sha512, Sha512Secret, "25091201")]
    [InlineData(1111111111L, TotpAlgorithm.Sha1, Sha1Secret, "14050471")]
    [InlineData(1111111111L, TotpAlgorithm.Sha256, Sha256Secret, "67062674")]
    [InlineData(1111111111L, TotpAlgorithm.Sha512, Sha512Secret, "99943326")]
    [InlineData(1234567890L, TotpAlgorithm.Sha1, Sha1Secret, "89005924")]
    [InlineData(1234567890L, TotpAlgorithm.Sha256, Sha256Secret, "91819424")]
    [InlineData(1234567890L, TotpAlgorithm.Sha512, Sha512Secret, "93441116")]
    [InlineData(2000000000L, TotpAlgorithm.Sha1, Sha1Secret, "69279037")]
    [InlineData(2000000000L, TotpAlgorithm.Sha256, Sha256Secret, "90698825")]
    [InlineData(2000000000L, TotpAlgorithm.Sha512, Sha512Secret, "38618901")]
    [InlineData(20000000000L, TotpAlgorithm.Sha1, Sha1Secret, "65353130")]
    [InlineData(20000000000L, TotpAlgorithm.Sha256, Sha256Secret, "77737706")]
    [InlineData(20000000000L, TotpAlgorithm.Sha512, Sha512Secret, "47863826")]
    public void Generate_MatchesRfc6238Vectors(long unixSeconds, TotpAlgorithm algorithm, string secret, string expected)
    {
        var result = new TotpService().Generate(secret, algorithm, 8, 30, DateTimeOffset.FromUnixTimeSeconds(unixSeconds));

        Assert.Equal(expected, result.Code);
        Assert.InRange(result.SecondsRemaining, 1, 30);
        Assert.True(result.ValidUntilUtc > DateTimeOffset.FromUnixTimeSeconds(unixSeconds));
    }

    [Fact]
    public void Generate_AcceptsFormattedLowercaseBase32()
    {
        var result = new TotpService().Generate("gezd gnbv-gy3t qojq gezd gnbv gy3t qojq", TotpAlgorithm.Sha1, 8, 30, DateTimeOffset.FromUnixTimeSeconds(59));

        Assert.Equal("94287082", result.Code);
    }

    [Fact]
    public void Generate_AcceptsMaximumNormalizedSeedWithBoundedGrouping()
    {
        var normalized = new string('A', 1024);
        var grouped = string.Join('-', Enumerable.Range(0, 128).Select(index => normalized.Substring(index * 8, 8)));

        var result = new TotpService().Generate(grouped, TotpAlgorithm.Sha512, 8, 30, DateTimeOffset.FromUnixTimeSeconds(59));

        Assert.Equal(8, result.Code.Length);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    public void Generate_ProducesConfiguredDigitCount(int digits)
    {
        var result = new TotpService().Generate(Sha1Secret, TotpAlgorithm.Sha1, digits, 30, DateTimeOffset.FromUnixTimeSeconds(59));

        Assert.Equal(digits, result.Code.Length);
        Assert.All(result.Code, character => Assert.True(char.IsAsciiDigit(character)));
    }

    [Fact]
    public void Generate_RejectsMalformedSeedBeforeHmacWork()
    {
        var service = new TotpService();

        Assert.Throws<ArgumentException>(() => service.Generate("NOT*BASE32*SECRET", TotpAlgorithm.Sha1, 6, 30, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => service.Generate(new string('A', 4097), TotpAlgorithm.Sha1, 6, 30, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => service.Generate("AAAAAAAAAAAAAAAAA", TotpAlgorithm.Sha1, 6, 30, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => service.Generate("JBSWY3DPEHPK3PXP=", TotpAlgorithm.Sha1, 6, 30, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => service.Generate("JBSWY3DP=EHPK3PXP", TotpAlgorithm.Sha1, 6, 30, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Generate_RejectsUnsupportedSettingsAndPreEpochTime()
    {
        var service = new TotpService();

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Generate(Sha1Secret, TotpAlgorithm.Sha1, 7, 30, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Generate(Sha1Secret, TotpAlgorithm.Sha1, 6, 10, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Generate(Sha1Secret, (TotpAlgorithm)999, 6, 30, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Generate(Sha1Secret, TotpAlgorithm.Sha1, 6, 30, DateTimeOffset.FromUnixTimeSeconds(-1)));
    }
}
