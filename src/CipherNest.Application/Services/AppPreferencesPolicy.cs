using CipherNest.Domain.Models;

namespace CipherNest.Application.Services;

public static class AppPreferencesPolicy
{
    public static AppPreferences Normalize(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var theme = Enum.IsDefined(preferences.Theme) ? preferences.Theme : AppThemePreference.System;
        var language = Enum.IsDefined(preferences.Language) ? preferences.Language : AppLanguagePreference.System;
        var uppercase = preferences.GeneratorUppercase;
        var lowercase = preferences.GeneratorLowercase;
        var digits = preferences.GeneratorDigits;
        var symbols = preferences.GeneratorSymbols;

        if (!preferences.GeneratorPassphraseMode && !uppercase && !lowercase && !digits && !symbols)
            lowercase = true;

        return preferences with
        {
            Theme = theme,
            Language = language,
            LockTimeoutSeconds = Math.Clamp(preferences.LockTimeoutSeconds, 5, 3600),
            ClipboardClearSeconds = Math.Clamp(preferences.ClipboardClearSeconds, 5, 300),
            TrashRetentionDays = Math.Clamp(preferences.TrashRetentionDays, 1, 365),
            RequireMasterPassphraseAfterHours = Math.Clamp(preferences.RequireMasterPassphraseAfterHours, 1, 168),
            BackupReminderDays = Math.Clamp(preferences.BackupReminderDays, 1, 365),
            ReviewReminderLeadDays = Math.Clamp(preferences.ReviewReminderLeadDays, 0, 365),
            GeneratorPasswordLength = Math.Clamp(preferences.GeneratorPasswordLength, 8, 256),
            GeneratorPassphraseWordCount = Math.Clamp(preferences.GeneratorPassphraseWordCount, 6, 16),
            GeneratorUppercase = uppercase,
            GeneratorLowercase = lowercase,
            GeneratorDigits = digits,
            GeneratorSymbols = symbols
        };
    }
}
