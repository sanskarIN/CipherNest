using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"CipherNest-SettingsTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAndLoad_RoundTripsCurrentPreferences()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        var store = new JsonSettingsStore(path);
        var expected = new AppPreferences
        {
            Theme = AppThemePreference.Dark,
            Language = AppLanguagePreference.English,
            LockTimeoutSeconds = 120,
            LockOnBackground = false,
            ClipboardClearSeconds = 45,
            ScreenshotProtection = false,
            BiometricUnlockEnabled = true,
            ReducedMotion = true,
            LargerInterface = true,
            TrashRetentionDays = 14,
            RequireMasterPassphraseAfterHours = 12,
            BackupReminderDays = 3,
            ReviewRemindersEnabled = false,
            ReviewReminderLeadDays = 2,
            GeneratorPassphraseMode = true,
            GeneratorPasswordLength = 48,
            GeneratorPassphraseWordCount = 10,
            GeneratorUppercase = false,
            GeneratorLowercase = false,
            GeneratorDigits = false,
            GeneratorSymbols = false,
            GeneratorExcludeAmbiguous = false,
            LastSuccessfulBackupUtc = new DateTimeOffset(2026, 8, 10, 9, 30, 0, TimeSpan.Zero)
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.Equal(expected, actual);
        Assert.Empty(Directory.GetFiles(_directory, ".*.tmp", SearchOption.TopDirectoryOnly));
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public async Task SaveAndLoad_BareRelativeFileName_Works()
    {
        var relativePath = $"ciphernest-settings-{Guid.NewGuid():N}.json";
        var fullPath = Path.GetFullPath(relativePath);
        try
        {
            var store = new JsonSettingsStore(relativePath);
            var expected = new AppPreferences { Theme = AppThemePreference.Dark };

            await store.SaveAsync(expected);
            var actual = await store.LoadAsync();

            Assert.Equal(AppThemePreference.Dark, actual.Theme);
            Assert.True(File.Exists(fullPath));
        }
        finally
        {
            try
            {
                if (File.Exists(fullPath)) File.Delete(fullPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    [Fact]
    public void Constructor_RejectsWhitespacePath()
    {
        Assert.Throws<ArgumentException>(() => new JsonSettingsStore("   "));
    }

    [Fact]
    public async Task Load_NormalizesOutOfRangePersistedValues()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        await File.WriteAllTextAsync(path, """
        {
          "theme": 999,
          "language": 999,
          "lockTimeoutSeconds": -10,
          "clipboardClearSeconds": 9999,
          "trashRetentionDays": 0,
          "requireMasterPassphraseAfterHours": 999,
          "generatorPasswordLength": 1,
          "generatorPassphraseWordCount": 100,
          "generatorUppercase": false,
          "generatorLowercase": false,
          "generatorDigits": false,
          "generatorSymbols": false
        }
        """);

        var actual = await new JsonSettingsStore(path).LoadAsync();

        Assert.Equal(AppThemePreference.System, actual.Theme);
        Assert.Equal(AppLanguagePreference.System, actual.Language);
        Assert.Equal(5, actual.LockTimeoutSeconds);
        Assert.Equal(300, actual.ClipboardClearSeconds);
        Assert.Equal(1, actual.TrashRetentionDays);
        Assert.Equal(168, actual.RequireMasterPassphraseAfterHours);
        Assert.Equal(8, actual.GeneratorPasswordLength);
        Assert.Equal(16, actual.GeneratorPassphraseWordCount);
        Assert.True(actual.GeneratorLowercase);
    }

    [Fact]
    public async Task Load_MalformedJsonFallsBackToDefaults()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        await File.WriteAllTextAsync(path, "{ this is not valid json");

        var actual = await new JsonSettingsStore(path).LoadAsync();

        Assert.Equal(new AppPreferences(), actual);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
