using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class JsonSettingsStoreBoundsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestSettingsBounds", Guid.NewGuid().ToString("N"));
    private readonly string _path;

    public JsonSettingsStoreBoundsTests()
    {
        Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "settings.json");
    }

    [Fact]
    public async Task OversizedSettingsFile_FallsBackBeforeParsing()
    {
        await File.WriteAllBytesAsync(_path, new byte[JsonSettingsStore.MaximumSettingsFileBytes + 1]);
        var store = new JsonSettingsStore(_path);

        var loaded = await store.LoadAsync();

        Assert.Equal(new AppPreferences(), loaded);
    }

    [Fact]
    public async Task SaveAndLoad_StayWithinBoundAndPreserveNormalizedPreferences()
    {
        var store = new JsonSettingsStore(_path);
        var preferences = new AppPreferences
        {
            Theme = AppThemePreference.Dark,
            LockTimeoutSeconds = 120,
            ClipboardClearSeconds = 45,
            TrashRetentionDays = 60,
            GeneratorPasswordLength = 32,
            GeneratorPassphraseWordCount = 10,
            GeneratorUppercase = true,
            GeneratorLowercase = true,
            GeneratorDigits = false,
            GeneratorSymbols = true
        };

        await store.SaveAsync(preferences);
        var length = new FileInfo(_path).Length;
        var loaded = await store.LoadAsync();

        Assert.InRange(length, 1, JsonSettingsStore.MaximumSettingsFileBytes);
        Assert.Equal(AppThemePreference.Dark, loaded.Theme);
        Assert.Equal(120, loaded.LockTimeoutSeconds);
        Assert.Equal(45, loaded.ClipboardClearSeconds);
        Assert.Equal(60, loaded.TrashRetentionDays);
        Assert.Equal(32, loaded.GeneratorPasswordLength);
        Assert.Equal(10, loaded.GeneratorPassphraseWordCount);
        Assert.False(loaded.GeneratorDigits);
    }

    [Fact]
    public async Task MalformedJson_FallsBackToDefaults()
    {
        await File.WriteAllTextAsync(_path, "{not-json");
        var store = new JsonSettingsStore(_path);

        var loaded = await store.LoadAsync();

        Assert.Equal(new AppPreferences(), loaded);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
