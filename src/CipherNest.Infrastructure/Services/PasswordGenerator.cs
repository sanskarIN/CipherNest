using System.Security.Cryptography;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Models;
using CipherNest.Domain.Models;

namespace CipherNest.Infrastructure.Services;

public sealed class PasswordGenerator : IPasswordGenerator
{
    private const string Lower = "abcdefghijklmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{}:,.?";
    private const string Ambiguous = "Il1O0o|`'\"";

    private static readonly string[] Words =
    [
        "amber", "anchor", "apple", "atlas", "bamboo", "beacon", "berry", "birch", "breeze", "canyon",
        "cedar", "cloud", "coral", "crystal", "dawn", "delta", "ember", "falcon", "fern", "forest",
        "galaxy", "garden", "glacier", "harbor", "hazel", "island", "jade", "jungle", "lantern", "lemon",
        "lilac", "lotus", "maple", "meadow", "meteor", "mist", "moon", "moss", "nebula", "oasis",
        "ocean", "olive", "orchid", "pearl", "pine", "planet", "plum", "prairie", "quartz", "rain",
        "reef", "river", "robin", "sage", "shell", "sky", "solar", "sparrow", "stone", "storm",
        "sunset", "tiger", "timber", "valley", "violet", "wave", "willow", "winter", "zenith", "zephyr"
    ];

    public string Generate(GeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.Mode == GeneratorMode.Passphrase ? GeneratePassphrase(options) : GeneratePassword(options);
    }

    public PasswordStrengthResult Evaluate(string secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return new PasswordStrengthResult(0, "Empty", ["Use a long, unique passphrase or generated password."]);
        }

        var score = 0;
        var suggestions = new List<string>();
        if (secret.Length >= 12) score++;
        else suggestions.Add("Use at least 12 characters; 16 or more is better.");
        if (secret.Length >= 18) score++;
        if (secret.Any(char.IsLower) && secret.Any(char.IsUpper)) score++;
        if (secret.Any(char.IsDigit) || secret.Any(char.IsPunctuation) || secret.Any(char.IsSymbol)) score++;
        if (HasCommonPattern(secret))
        {
            score = Math.Max(0, score - 2);
            suggestions.Add("Avoid common passwords, sequences, repeated characters, names, and predictable patterns.");
        }
        if (secret.Distinct().Count() < Math.Min(6, secret.Length))
        {
            score = Math.Max(0, score - 1);
            suggestions.Add("Use more varied characters or a randomly generated passphrase.");
        }

        score = Math.Clamp(score, 0, 4);
        var label = score switch { 0 => "Very weak", 1 => "Weak", 2 => "Fair", 3 => "Strong", _ => "Very strong" };
        return new PasswordStrengthResult(score, label, suggestions);
    }

    private static string GeneratePassword(GeneratorOptions options)
    {
        if (options.Length is < 8 or > 256)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Password length must be between 8 and 256.");
        }

        var sets = new List<string>();
        if (options.Lowercase) sets.Add(Filter(Lower, options.ExcludeAmbiguous));
        if (options.Uppercase) sets.Add(Filter(Upper, options.ExcludeAmbiguous));
        if (options.Digits) sets.Add(Filter(Digits, options.ExcludeAmbiguous));
        if (options.Symbols) sets.Add(Filter(Symbols, options.ExcludeAmbiguous));
        if (sets.Count == 0)
        {
            throw new ArgumentException("At least one character group must be selected.", nameof(options));
        }
        if (options.Length < sets.Count)
        {
            throw new ArgumentException("Length is too short for the selected character groups.", nameof(options));
        }

        var pool = string.Concat(sets);
        var chars = new char[options.Length];
        var index = 0;
        foreach (var set in sets)
        {
            chars[index++] = set[RandomNumberGenerator.GetInt32(set.Length)];
        }
        while (index < chars.Length)
        {
            chars[index++] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
        }
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    private static string GeneratePassphrase(GeneratorOptions options)
    {
        if (options.WordCount is < 3 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Passphrase word count must be between 3 and 12.");
        }
        if (options.Separator.Length > 4 || options.Separator.Any(char.IsControl))
        {
            throw new ArgumentException("Passphrase separator is invalid.", nameof(options));
        }
        var words = new string[options.WordCount];
        for (var i = 0; i < words.Length; i++)
        {
            words[i] = Words[RandomNumberGenerator.GetInt32(Words.Length)];
        }
        return string.Join(options.Separator, words);
    }

    private static string Filter(string source, bool excludeAmbiguous) =>
        excludeAmbiguous ? new string(source.Where(c => !Ambiguous.Contains(c, StringComparison.Ordinal)).ToArray()) : source;

    private static bool HasCommonPattern(string secret)
    {
        var lower = secret.ToLowerInvariant();
        string[] common = ["password", "qwerty", "123456", "letmein", "admin", "welcome", "abcdef", "111111", "iloveyou"];
        return common.Any(lower.Contains) || lower.Contains("012345", StringComparison.Ordinal) || lower.Contains("987654", StringComparison.Ordinal);
    }
}
