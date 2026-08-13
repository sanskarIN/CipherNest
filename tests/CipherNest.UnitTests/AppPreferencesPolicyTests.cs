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

    [Theory]
    [InlineData(-1000)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(300)]
    [InlineData(365)]
    [InlineData(3600)]
    [InlineData(3601)]
    [InlineData(1000000)]
    public void Normalize_ProducesSupportedRangesAndIsIdempotent(int value)
    {
        var preferences = new AppPreferences
        {
            Theme = (AppThemePreference)value,
            Language = (AppLanguagePreference)value,
            LockTimeoutSeconds = value,
            ClipboardClearSeconds = value,
            TrashRetentionDays = value,
            RequireMasterPassphraseAfterHours = value,
            BackupReminderDays = value,
            ReviewReminderLeadDays = value,
            GeneratorPasswordLength = value,
            GeneratorPassphraseWordCount = value,
            GeneratorPassphraseMode = false,
            GeneratorUppercase = false,
            GeneratorLowercase = false,
            GeneratorDigits = false,
            GeneratorSymbols = false
        };

        var normalized = AppPreferencesPolicy.Normalize(preferences);

        Assert.True(Enum.IsDefined(normalized.Theme));
        Assert.True(Enum.IsDefined(normalized.Language));
        Assert.InRange(normalized.LockTimeoutSeconds, 5, 3600);
        Assert.InRange(normalized.ClipboardClearSeconds, 5, 300);
        Assert.InRange(normalized.TrashRetentionDays, 1, 365);
        Assert.InRange(normalized.RequireMasterPassphraseAfterHours, 1, 168);
        Assert.InRange(normalized.BackupReminderDays, 1, 365);
        Assert.InRange(normalized.ReviewReminderLeadDays, 0, 365);
        Assert.InRange(normalized.GeneratorPasswordLength, 8, 256);
        Assert.InRange(normalized.GeneratorPassphraseWordCount, 6, 16);
        Assert.True(
            normalized.GeneratorUppercase ||
            normalized.GeneratorLowercase ||
            normalized.GeneratorDigits ||
            normalized.GeneratorSymbols);
        Assert.Equal(normalized, AppPreferencesPolicy.Normalize(normalized));
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
