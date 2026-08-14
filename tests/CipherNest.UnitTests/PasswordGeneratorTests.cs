using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class PasswordGeneratorTests
{
    private readonly PasswordGenerator _generator = new();

    [Fact]
    public void GeneratedPassword_HasRequestedLengthAndGroups()
    {
        var value = _generator.Generate(new GeneratorOptions { Length = 32, Uppercase = true, Lowercase = true, Digits = true, Symbols = true });
        Assert.Equal(32, value.Length);
        Assert.Contains(value, char.IsUpper);
        Assert.Contains(value, char.IsLower);
        Assert.Contains(value, char.IsDigit);
        Assert.Contains(value, c => char.IsPunctuation(c) || char.IsSymbol(c));
    }

    [Fact]
    public void Passphrase_UsesRequestedWordCount()
    {
        var value = _generator.Generate(new GeneratorOptions { Mode = GeneratorMode.Passphrase, WordCount = 6, Separator = "-" });
        Assert.Equal(6, value.Split('-').Length);
    }

    [Fact]
    public void Generate_RejectsUnknownMode()
    {
        var options = new GeneratorOptions { Mode = (GeneratorMode)999 };

        Assert.Throws<ArgumentOutOfRangeException>(() => _generator.Generate(options));
    }

    [Fact]
    public void Passphrase_RejectsNullSeparator()
    {
        var options = new GeneratorOptions { Mode = GeneratorMode.Passphrase, WordCount = 6, Separator = null! };

        Assert.Throws<ArgumentException>(() => _generator.Generate(options));
    }

    [Fact]
    public void CommonPassword_IsNotRatedStrong()
    {
        var result = _generator.Evaluate("Password123456789!");
        Assert.True(result.Score <= 2);
    }
}
