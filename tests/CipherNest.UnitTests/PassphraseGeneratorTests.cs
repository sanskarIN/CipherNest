using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class PassphraseGeneratorTests
{
    [Fact]
    public void WordList_ContainsExactly256UniqueLowercaseWords()
    {
        Assert.Equal(256, PassphraseWordList.Words.Count);
        Assert.Equal(256, PassphraseWordList.Words.Distinct(StringComparer.Ordinal).Count());
        Assert.All(PassphraseWordList.Words, word =>
        {
            Assert.InRange(word.Length, 3, 20);
            Assert.All(word, ch => Assert.InRange(ch, 'a', 'z'));
        });
    }

    [Fact]
    public void GeneratePassphrase_UsesRequestedWordCount()
    {
        var generator = new PasswordGenerator();
        var value = generator.Generate(new GeneratorOptions { Mode = GeneratorMode.Passphrase, WordCount = 8, Separator = "-" });

        var words = value.Split('-');
        Assert.Equal(8, words.Length);
        Assert.All(words, word => Assert.Contains(word, PassphraseWordList.Words));
    }

    [Fact]
    public void GeneratePassphrase_RejectsTooFewWords()
    {
        var generator = new PasswordGenerator();
        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(new GeneratorOptions { Mode = GeneratorMode.Passphrase, WordCount = 5 }));
    }
}
