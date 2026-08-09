namespace CipherNest.Domain.Models;

public enum AppThemePreference
{
    System,
    Light,
    Dark
}

public sealed record AppPreferences
{
    public AppThemePreference Theme { get; init; } = AppThemePreference.System;
    public int LockTimeoutSeconds { get; init; } = 60;
    public bool LockOnBackground { get; init; } = true;
    public int ClipboardClearSeconds { get; init; } = 30;
    public bool ScreenshotProtection { get; init; } = true;
    public bool BiometricUnlockEnabled { get; init; }
    public bool ReducedMotion { get; init; }
    public bool LargerInterface { get; init; }
    public int TrashRetentionDays { get; init; } = 30;
    public int RequireMasterPassphraseAfterHours { get; init; } = 24;
    public int BackupReminderDays { get; init; } = 7;
    public bool ReviewRemindersEnabled { get; init; } = true;
    public int ReviewReminderLeadDays { get; init; } = 7;
    public bool GeneratorPassphraseMode { get; init; }
    public int GeneratorPasswordLength { get; init; } = 20;
    public int GeneratorPassphraseWordCount { get; init; } = 8;
    public bool GeneratorUppercase { get; init; } = true;
    public bool GeneratorLowercase { get; init; } = true;
    public bool GeneratorDigits { get; init; } = true;
    public bool GeneratorSymbols { get; init; } = true;
    public bool GeneratorExcludeAmbiguous { get; init; } = true;
    public DateTimeOffset? LastSuccessfulBackupUtc { get; init; }
}
