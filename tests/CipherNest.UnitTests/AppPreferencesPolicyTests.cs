using CipherNest.Application.Services;
using CipherNest.Domain.Models;

namespace CipherNest.UnitTests;

public sealed class AppPreferencesPolicyTests
{
    [Fact]
    public void Normalize_ClampsSecurityAndGeneratorBounds()
    {
        var normalized = AppPreferencesPolicy.Normalize(new AppPreferences
        {
            Theme = (AppThemePreference)999,
            Language = (AppLanguagePreference)999,
            LockTimeoutSeconds = -1,
            ClipboardClearSeconds = 9999,
            TrashRetentionDays = 0,
            RequireMasterPassphraseAfterHours = 1000,
            BackupReminderDays = -5,
            ReviewReminderLeadDays = 900,
            GeneratorPasswordLength = 1,
            GeneratorPassphraseWordCount = 100
        });

        Assert.Equal(AppThemePreference.System, normalized.Theme);
        Assert.Equal(AppLanguagePreference.System, normalized.Language);
        Assert.Equal(5, normalized.LockTimeoutSeconds);
        Assert.Equal(300, normalized.ClipboardClearSeconds);
        Assert.Equal(1, normalized.TrashRetentionDays);
        Assert.Equal(168, normalized.RequireMasterPassphraseAfterHours);
        Assert.Equal(1, normalized.BackupReminderDays);
        Assert.Equal(365, normalized.ReviewReminderLeadDays);
        Assert.Equal(8, normalized.GeneratorPasswordLength);
        Assert.Equal(16, normalized.GeneratorPassphraseWordCount);
    }

    [Fact]
    public void Normalize_RestoresACharacterGroupWhenPasswordModeHasNone()
    {
        var normalized = AppPreferencesPolicy.Normalize(new AppPreferences
        {
            GeneratorPassphraseMode = false,
            GeneratorUppercase = false,
            GeneratorLowercase = false,
            GeneratorDigits = false,
            GeneratorSymbols = false
        });

        Assert.False(normalized.GeneratorUppercase);
        Assert.True(normalized.GeneratorLowercase);
        Assert.False(normalized.GeneratorDigits);
        Assert.False(normalized.GeneratorSymbols);
    }

    [Fact]
    public void Normalize_DoesNotForceCharacterGroupsInPassphraseMode()
    {
        var normalized = AppPreferencesPolicy.Normalize(new AppPreferences
        {
            GeneratorPassphraseMode = true,
            GeneratorUppercase = false,
            GeneratorLowercase = false,
            GeneratorDigits = false,
            GeneratorSymbols = false
        });

        Assert.False(normalized.GeneratorUppercase);
        Assert.False(normalized.GeneratorLowercase);
        Assert.False(normalized.GeneratorDigits);
        Assert.False(normalized.GeneratorSymbols);
    }
}
