using System.Text;
using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class JsonSettingsAdversarialTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestSettingsAdversarial", Guid.NewGuid().ToString("N"));
    private readonly string _path;

    public JsonSettingsAdversarialTests()
    {
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "settings.json");
    }

    [Fact]
    public async Task DeterministicAdversarialCorpus_NeverEscapesNormalizedContract()
    {
        var corpus = new List<string>
        {
            "{}",
            "null",
            "[]",
            "true",
            "false",
            "0",
            "\"text\"",
            "{\"theme\":999,\"lockTimeoutSeconds\":-2147483648}",
            "{\"theme\":2,\"theme\":1}",
            "{\"lastSuccessfulBackupUtc\":\"not-a-date\"}",
            "{\"unknown\":{\"nested\":[1,2,3]}}",
            "{\"generatorPassphraseMode\":false,\"generatorUppercase\":false,\"generatorLowercase\":false,\"generatorDigits\":false,\"generatorSymbols\":false}",
            "{",
            "}",
            "[",
            "\"",
            "{\"x\":\u0000}"
        };

        var random = new Random(0x53E771);
        char[] alphabet = ['{', '}', '[', ']', ':', ',', '"', '\\', ' ', '\t', '\r', '\n', '0', '1', '9', '-', '+', 'e', 'E', 't', 'f', 'n', 'a', 'x', 'é', '中', '\u200B', '\0'];
        for (var caseIndex = 0; caseIndex < 192; caseIndex++)
        {
            var length = random.Next(0, 1025);
            var builder = new StringBuilder(length);
            for (var index = 0; index < length; index++)
                builder.Append(alphabet[random.Next(alphabet.Length)]);
            corpus.Add(builder.ToString());
        }

        foreach (var json in corpus)
        {
            await File.WriteAllTextAsync(_path, json, Encoding.UTF8);

            var loaded = await new JsonSettingsStore(_path).LoadAsync();

            AssertNormalized(loaded);
        }
    }

    private static void AssertNormalized(AppPreferences preferences)
    {
        Assert.True(Enum.IsDefined(preferences.Theme));
        Assert.True(Enum.IsDefined(preferences.Language));
        Assert.InRange(preferences.LockTimeoutSeconds, 5, 3600);
        Assert.InRange(preferences.ClipboardClearSeconds, 5, 300);
        Assert.InRange(preferences.TrashRetentionDays, 1, 365);
        Assert.InRange(preferences.RequireMasterPassphraseAfterHours, 1, 168);
        Assert.InRange(preferences.BackupReminderDays, 1, 365);
        Assert.InRange(preferences.ReviewReminderLeadDays, 0, 365);
        Assert.InRange(preferences.GeneratorPasswordLength, 8, 256);
        Assert.InRange(preferences.GeneratorPassphraseWordCount, 6, 16);

        if (!preferences.GeneratorPassphraseMode &&
            !preferences.GeneratorUppercase &&
            !preferences.GeneratorLowercase &&
            !preferences.GeneratorDigits &&
            !preferences.GeneratorSymbols)
        {
            Assert.Fail("Password generator mode must retain at least one enabled character group after normalization.");
        }
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
