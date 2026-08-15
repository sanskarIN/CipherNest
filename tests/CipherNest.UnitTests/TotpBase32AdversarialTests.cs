using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class TotpBase32AdversarialTests
{
    private const string ValidSeed = "JBSWY3DPEHPK3PXP";

    public static IEnumerable<object[]> HostileSecrets => BuildHostileSecrets().Select(static (value, index) => new object[] { index, value });

    [Fact]
    public void Corpus_IsExactly128DistinctDeterministicInputs()
    {
        var corpus = BuildHostileSecrets();

        Assert.Equal(128, corpus.Count);
        Assert.Equal(corpus.Count, corpus.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [MemberData(nameof(HostileSecrets))]
    public void Generate_RejectsHostileBase32BeforeCodeGeneration(int caseId, string secret)
    {
        Assert.InRange(caseId, 0, 127);
        var service = new TotpService();

        Assert.Throws<ArgumentException>(() => service.Generate(
            secret,
            TotpAlgorithm.Sha1,
            6,
            30,
            DateTimeOffset.FromUnixTimeSeconds(59)));
    }

    private static IReadOnlyList<string> BuildHostileSecrets()
    {
        var corpus = new List<string>
        {
            string.Empty,
            " ",
            "----------------",
            new('A', 15),
            new('A', 17),
            new('A', 19),
            new('A', 22),
            new('A', 1025),
            new('A', 4097),
            ValidSeed + "=",
            "JBSWY3DP=EHPK3PXP",
            new string('A', 18) + "=",
            new string('A', 20) + "===",
            new string('A', 21) + "====",
            new string('A', 23) + "==",
            new string('A', 17) + "B",
            new string('A', 19) + "B",
            new string('A', 20) + "B",
            new string('A', 22) + "B",
            "JBSWY3DPEHPK3PX0",
            "JBSWY3DPEHPK3PX1",
            "JBSWY3DPEHPK3PX8",
            "JBSWY3DPEHPK3PX9",
            ValidSeed + "\u200B",
            ValidSeed + "\u202E",
            ValidSeed + "\u2066",
            ValidSeed + "\0",
            ValidSeed + "\u007F",
            ValidSeed + "\u00E9",
            ValidSeed + "\uFF21",
            ValidSeed + "\uD800",
            ValidSeed + "\uDC00"
        };

        ReadOnlySpan<char> invalidReplacementCharacters = ['!', '@', '#', '$', '%', '^'];
        foreach (var replacement in invalidReplacementCharacters)
        {
            for (var index = 0; index < ValidSeed.Length; index++)
            {
                var candidate = ValidSeed.ToCharArray();
                candidate[index] = replacement;
                corpus.Add(new string(candidate));
            }
        }

        return corpus;
    }
}
