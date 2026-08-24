using System.Security.Cryptography;
using CipherNest.Application.Abstractions;
using CipherNest.App.Services;
using CipherNest.Domain.Models;
using CipherNest.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private const string DeletePhrase = "DELETE MY VAULT";
    private const int MaximumPassphraseCharacters = 4_096;
    private readonly ISettingsStore _settings;
    private readonly IBackupService _backup;
    private readonly IVaultService _vault;
    private readonly IScreenshotProtectionService _screenshots;
    private readonly IPasswordGenerator _passwordGenerator;
    private readonly IBiometricUnlockService _biometrics;
    private readonly SessionSecurityState _sessionSecurity;
    private readonly IStorageMaintenanceService _storage;
    private readonly IClipboardSecurityService _clipboard;
    private readonly IPrivacySafeExceptionReporter _exceptions;
    private AppPreferences _loadedPreferences = new();

    public IReadOnlyList<AppThemePreference> Themes { get; } = Enum.GetValues<AppThemePreference>();

    [ObservableProperty]
    public partial AppThemePreference SelectedTheme { get; set; }

    [ObservableProperty]
    public partial int LockTimeoutSeconds { get; set; } = 60;

    [ObservableProperty]
    public partial bool LockOnBackground { get; set; } = true;

    [ObservableProperty]
    public partial int ClipboardClearSeconds { get; set; } = 30;

    [ObservableProperty]
    public partial bool ScreenshotProtection { get; set; } = true;

    [ObservableProperty]
    public partial bool BiometricUnlockEnabled { get; set; }

    [ObservableProperty]
    public partial bool BiometricAvailable { get; set; }

    [ObservableProperty]
    public partial bool ReducedMotion { get; set; }

    [ObservableProperty]
    public partial bool LargerInterface { get; set; }

    [ObservableProperty]
    public partial int TrashRetentionDays { get; set; } = 30;

    [ObservableProperty]
    public partial int BackupReminderDays { get; set; } = 7;

    [ObservableProperty]
    public partial bool ReviewRemindersEnabled { get; set; } = true;

    [ObservableProperty]
    public partial int ReviewReminderLeadDays { get; set; } = 7;

    [ObservableProperty]
    public partial int RequireMasterPassphraseAfterHours { get; set; } = 24;

    [ObservableProperty]
    public partial string BackupPassphrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrentMasterPassphrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewMasterPassphrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmNewMasterPassphrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DeletionMasterPassphrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DeletionConfirmationPhrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ScreenshotSupportMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BiometricSupportMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StorageUsageMessage { get; set; } = "Calculating local storage…";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public SettingsViewModel(
        ISettingsStore settings,
        IBackupService backup,
        IVaultService vault,
        IScreenshotProtectionService screenshots,
        IPasswordGenerator passwordGenerator,
        IBiometricUnlockService biometrics,
        SessionSecurityState sessionSecurity,
        IStorageMaintenanceService storage,
        IClipboardSecurityService clipboard,
        IPrivacySafeExceptionReporter exceptions)
    {
        _settings = settings;
        _backup = backup;
        _vault = vault;
        _screenshots = screenshots;
        _passwordGenerator = passwordGenerator;
        _biometrics = biometrics;
        _sessionSecurity = sessionSecurity;
        _storage = storage;
        _clipboard = clipboard;
        _exceptions = exceptions;
        ScreenshotSupportMessage = screenshots.IsSupported
            ? SettingsText("SettingsScreenshotSupported")
            : SettingsText("SettingsScreenshotUnavailable");
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            _loadedPreferences = await _settings.LoadAsync();
            SelectedTheme = _loadedPreferences.Theme;
            LockTimeoutSeconds = _loadedPreferences.LockTimeoutSeconds;
            LockOnBackground = _loadedPreferences.LockOnBackground;
            ClipboardClearSeconds = _loadedPreferences.ClipboardClearSeconds;
            ScreenshotProtection = _loadedPreferences.ScreenshotProtection;
            BiometricUnlockEnabled = _loadedPreferences.BiometricUnlockEnabled;
            ReducedMotion = _loadedPreferences.ReducedMotion;
            LargerInterface = _loadedPreferences.LargerInterface;
            TrashRetentionDays = _loadedPreferences.TrashRetentionDays;
            BackupReminderDays = _loadedPreferences.BackupReminderDays;
            ReviewRemindersEnabled = _loadedPreferences.ReviewRemindersEnabled;
            ReviewReminderLeadDays = _loadedPreferences.ReviewReminderLeadDays;
            RequireMasterPassphraseAfterHours = _loadedPreferences.RequireMasterPassphraseAfterHours;
            BiometricAvailable = _biometrics.IsSupported && await _biometrics.IsAvailableAsync();
            var configured = await _vault.IsSecondaryUnlockConfiguredAsync();
            if (!configured && BiometricUnlockEnabled)
            {
                BiometricUnlockEnabled = false;
                _loadedPreferences = _loadedPreferences with { BiometricUnlockEnabled = false };
                await _settings.SaveAsync(_loadedPreferences);
            }
            BiometricSupportMessage = BiometricAvailable
                ? (configured ? SettingsText("SettingsBiometricConfigured") : SettingsText("SettingsBiometricAvailableNotConfigured"))
                : SettingsText("SettingsBiometricUnavailable");
            ApplyTheme(_loadedPreferences.Theme);
            await _screenshots.ApplyAsync(_loadedPreferences.ScreenshotProtection);
            await RefreshStorageAsync();
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.Load", ex);
            StatusMessage = SettingsText("SettingsLoadFailure");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            LockTimeoutSeconds = Math.Clamp(LockTimeoutSeconds, 5, 3600);
            ClipboardClearSeconds = Math.Clamp(ClipboardClearSeconds, 5, 300);
            TrashRetentionDays = Math.Clamp(TrashRetentionDays, 1, 365);
            BackupReminderDays = Math.Clamp(BackupReminderDays, 1, 365);
            ReviewReminderLeadDays = Math.Clamp(ReviewReminderLeadDays, 0, 365);
            RequireMasterPassphraseAfterHours = Math.Clamp(RequireMasterPassphraseAfterHours, 1, 168);
            _loadedPreferences = _loadedPreferences with
            {
                Theme = SelectedTheme,
                LockTimeoutSeconds = LockTimeoutSeconds,
                LockOnBackground = LockOnBackground,
                ClipboardClearSeconds = ClipboardClearSeconds,
                ScreenshotProtection = ScreenshotProtection,
                BiometricUnlockEnabled = BiometricUnlockEnabled,
                ReducedMotion = ReducedMotion,
                LargerInterface = LargerInterface,
                TrashRetentionDays = TrashRetentionDays,
                BackupReminderDays = BackupReminderDays,
                ReviewRemindersEnabled = ReviewRemindersEnabled,
                ReviewReminderLeadDays = ReviewReminderLeadDays,
                RequireMasterPassphraseAfterHours = RequireMasterPassphraseAfterHours
            };
            await _settings.SaveAsync(_loadedPreferences);
            ApplyTheme(_loadedPreferences.Theme);
            await _screenshots.ApplyAsync(_loadedPreferences.ScreenshotProtection);
            StatusMessage = SettingsText("SettingsSavedStatus");
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.Save", ex);
            StatusMessage = SettingsText("SettingsSaveFailure");
        }
    }

    [RelayCommand]
    private async Task RefreshStorageAsync()
    {
        try
        {
            var usage = await _storage.GetUsageAsync();
            StorageUsageMessage = $"Encrypted app data: {FormatBytes(usage.AppDataBytes)} · Temporary cache: {FormatBytes(usage.CacheBytes)} · Total local footprint: {FormatBytes(usage.TotalBytes)}";
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.StorageUsage", ex);
            StorageUsageMessage = "Storage usage could not be measured safely.";
        }
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        IsBusy = true;
        try
        {
            var confirm = await Shell.Current.DisplayAlertAsync("Clear temporary cache?", "This removes CipherNest-managed temporary cache files such as completed plaintext export/share staging files when the operating system still allows access. It does not delete the encrypted vault, encrypted attachments, or backups stored in app data.", "Clear cache", "Cancel");
            if (!confirm) return;
            var deleted = await _storage.ClearCacheAsync();
            StatusMessage = $"Temporary cache cleanup completed. Approximately {FormatBytes(deleted)} was removed where permitted.";
            await RefreshStorageAsync();
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.ClearCache", ex);
            StatusMessage = "Temporary cache cleanup could not be completed safely. The encrypted vault was not intentionally removed.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task EnableBiometricUnlockAsync()
    {
        if (!_vault.IsUnlocked) { await Shell.Current.GoToAsync("//unlock"); return; }
        if (string.IsNullOrWhiteSpace(CurrentMasterPassphrase)) { StatusMessage = SettingsText("SettingsEnableBiometricMasterRequired"); return; }

        var masterPassphrase = CurrentMasterPassphrase;
        CurrentMasterPassphrase = string.Empty;
        try
        {
            if (!_biometrics.IsSupported || !await _biometrics.IsAvailableAsync()) { StatusMessage = SettingsText("SettingsBiometricUnavailableStatus"); return; }
            if (!await _vault.ReauthenticateAsync(masterPassphrase)) { StatusMessage = SettingsText("SettingsMasterConfirmationFailed"); return; }
            if (!await _biometrics.AuthenticateAsync(SettingsText("SettingsEnableBiometricPrompt"))) { StatusMessage = SettingsText("SettingsBiometricAuthenticationFailed"); return; }

            var bytes = RandomNumberGenerator.GetBytes(48);
            string secret;
            try { secret = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_'); }
            finally { CryptographicOperations.ZeroMemory(bytes); }

            IsBusy = true;
            var secondaryConfigured = false;
            try
            {
                await _biometrics.StoreSecondarySecretAsync(secret);
                try
                {
                    await _vault.EnableSecondaryUnlockAsync(masterPassphrase, secret);
                    secondaryConfigured = true;
                    var enabledPreferences = _loadedPreferences with { BiometricUnlockEnabled = true };
                    await _settings.SaveAsync(enabledPreferences);
                    _loadedPreferences = enabledPreferences;
                }
                catch
                {
                    if (secondaryConfigured)
                    {
                        try { await _vault.DisableSecondaryUnlockAsync(masterPassphrase); }
                        catch (Exception rollbackException) { _exceptions.Report("Settings.BiometricEnable.VaultRollback", rollbackException); }
                    }
                    try { await _biometrics.ClearSecondarySecretAsync(); }
                    catch (Exception rollbackException) { _exceptions.Report("Settings.BiometricEnable.SecretRollback", rollbackException); }
                    _loadedPreferences = _loadedPreferences with { BiometricUnlockEnabled = false };
                    throw;
                }

                BiometricUnlockEnabled = true;
                _sessionSecurity.RecordMasterAuthentication(DateTimeOffset.UtcNow);
                BiometricSupportMessage = SettingsText("SettingsBiometricConfiguredSecureStorage");
                StatusMessage = SettingsText("SettingsBiometricEnabledStatus");
            }
            finally { secret = string.Empty; IsBusy = false; }
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.BiometricEnable", ex);
            BiometricUnlockEnabled = false;
            StatusMessage = SettingsText("SettingsBiometricEnableFailure");
        }
        finally
        {
            masterPassphrase = string.Empty;
        }
    }

    [RelayCommand]
    private async Task DisableBiometricUnlockAsync()
    {
        if (!_vault.IsUnlocked) { await Shell.Current.GoToAsync("//unlock"); return; }
        if (string.IsNullOrWhiteSpace(CurrentMasterPassphrase)) { StatusMessage = SettingsText("SettingsDisableBiometricMasterRequired"); return; }

        var masterPassphrase = CurrentMasterPassphrase;
        CurrentMasterPassphrase = string.Empty;
        IsBusy = true;
        try
        {
            await _vault.DisableSecondaryUnlockAsync(masterPassphrase);
            await _biometrics.ClearSecondarySecretAsync();
            BiometricUnlockEnabled = false;
            _sessionSecurity.RecordMasterAuthentication(DateTimeOffset.UtcNow);
            _loadedPreferences = _loadedPreferences with { BiometricUnlockEnabled = false };
            await _settings.SaveAsync(_loadedPreferences);
            BiometricSupportMessage = SettingsText("SettingsBiometricDisabledStatus");
            StatusMessage = SettingsText("SettingsBiometricDisabledStatus");
        }
        catch (CipherNest.Application.Exceptions.VaultAuthenticationException)
        {
            StatusMessage = SettingsText("SettingsMasterConfirmationFailed");
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.BiometricDisable", ex);
            StatusMessage = SettingsText("SettingsBiometricDisableFailure");
        }
        finally { masterPassphrase = string.Empty; IsBusy = false; }
    }

    [RelayCommand]
    private async Task ExportBackupAsync()
    {
        if (IsBusy) return;
        if (!_vault.IsUnlocked) { await Shell.Current.GoToAsync("//unlock"); return; }
        if (BackupPassphrase.Length is < 12 or > MaximumPassphraseCharacters) { StatusMessage = SettingsFormat("SettingsBackupPassphraseRangeFormat", MaximumPassphraseCharacters); return; }

        var backupPassphrase = BackupPassphrase;
        BackupPassphrase = string.Empty;
        IsBusy = true;
        try
        {
            bool confirm;
            try
            {
                confirm = await Shell.Current.DisplayAlertAsync(
                    SettingsText("SettingsBackupConfirmTitle"),
                    SettingsText("SettingsBackupConfirmBody"),
                    SettingsText("SettingsBackupConfirmAccept"),
                    SettingsText("CancelButton"));
            }
            catch (Exception ex)
            {
                _exceptions.Report("Settings.BackupExport.Confirm", ex);
                StatusMessage = SettingsText("SettingsBackupFailure");
                return;
            }
            if (!confirm) return;

            string path;
            try
            {
                await _vault.LockAsync();
                var directory = Path.Combine(FileSystem.Current.AppDataDirectory, "Backups");
                Directory.CreateDirectory(directory);
                path = Path.Combine(directory, $"CipherNest-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}{AppConstants.BackupExtension}");
                await _backup.ExportEncryptedAsync(path, backupPassphrase);
            }
            catch (Exception ex)
            {
                _exceptions.Report("Settings.BackupExport", ex);
                StatusMessage = SettingsText("SettingsBackupFailure");
                return;
            }

            // From this point onward the encrypted backup exists. Secondary reminder/share/navigation
            // failures must not overwrite that successful creation state with a false backup failure.
            StatusMessage = SettingsText("SettingsBackupCreatedStatus");

            try
            {
                _loadedPreferences = (await _settings.LoadAsync()) with { LastSuccessfulBackupUtc = DateTimeOffset.UtcNow };
                await _settings.SaveAsync(_loadedPreferences);
            }
            catch (Exception ex)
            {
                _exceptions.Report("Settings.BackupExport.Reminder", ex);
            }

            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest(SettingsText("SettingsBackupShareTitle"), new ShareFile(path)));
            }
            catch (Exception ex)
            {
                _exceptions.Report("Settings.BackupExport.Share", ex);
            }

            try
            {
                await Shell.Current.GoToAsync("//unlock");
            }
            catch (Exception ex)
            {
                _exceptions.Report("Settings.BackupExport.Navigation", ex);
            }
        }
        finally
        {
            backupPassphrase = string.Empty;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (BackupPassphrase.Length is < 12 or > MaximumPassphraseCharacters) { StatusMessage = SettingsFormat("SettingsRestorePassphraseRangeFormat", MaximumPassphraseCharacters); return; }

        var backupPassphrase = BackupPassphrase;
        BackupPassphrase = string.Empty;
        string? tempPath = null;
        var restoreCompleted = false;
        var biometricSecretCleanupFailed = false;
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = SettingsText("SettingsRestorePickerTitle") });
            if (file is null) return;
            var confirm = await Shell.Current.DisplayAlertAsync(
                SettingsText("SettingsRestoreConfirmTitle"),
                SettingsText("SettingsRestoreConfirmBody"),
                SettingsText("SettingsRestoreConfirmAccept"),
                SettingsText("CancelButton"));
            if (!confirm) return;

            IsBusy = true;
            tempPath = Path.Combine(FileSystem.Current.CacheDirectory, $"restore-{Guid.NewGuid():N}{AppConstants.BackupExtension}");
            try
            {
                await _vault.LockAsync();
                await using var source = await file.OpenReadAsync();
                await CipherNest.Infrastructure.Services.BackupStagingPolicy.CopyToNewFileAsync(source, tempPath);
                await _backup.RestoreEncryptedAsync(tempPath, backupPassphrase);
                restoreCompleted = true;
                _sessionSecurity.Clear();
                BiometricUnlockEnabled = false;
                try
                {
                    await _biometrics.ClearSecondarySecretAsync();
                }
                catch (Exception cleanupException)
                {
                    biometricSecretCleanupFailed = true;
                    _exceptions.Report("Settings.BackupRestore.BiometricCleanup", cleanupException);
                }
                _loadedPreferences = (await _settings.LoadAsync()) with { BiometricUnlockEnabled = false };
                await _settings.SaveAsync(_loadedPreferences);
                StatusMessage = biometricSecretCleanupFailed
                    ? SettingsText("SettingsRestoreCleanupWarning")
                    : SettingsText("SettingsRestoreSuccess");
                await Shell.Current.GoToAsync("//unlock");
            }
            finally { IsBusy = false; }
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.BackupRestore", ex);
            StatusMessage = restoreCompleted
                ? SettingsText("SettingsRestorePostCleanupFailure")
                : SettingsText("SettingsRestoreFailure");
        }
        finally
        {
            if (tempPath is not null)
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException) { _exceptions.Report("Settings.RestoreBackup.TempCleanup", cleanupException); }
            }
            backupPassphrase = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ChangeMasterPassphraseAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentMasterPassphrase)) { StatusMessage = SettingsText("SettingsChangeMasterCurrentRequired"); return; }
        if (!string.Equals(NewMasterPassphrase, ConfirmNewMasterPassphrase, StringComparison.Ordinal)) { StatusMessage = SettingsText("SettingsChangeMasterMismatch"); return; }
        if (NewMasterPassphrase.Length > MaximumPassphraseCharacters) { StatusMessage = SettingsFormat("SettingsChangeMasterTooLongFormat", MaximumPassphraseCharacters); return; }
        var strength = _passwordGenerator.Evaluate(NewMasterPassphrase);
        if (NewMasterPassphrase.Length < 12 || strength.Score < 3) { StatusMessage = SettingsFormat("SettingsChangeMasterWeakFormat", LocalizedStrengthLabel(strength.Score)); return; }

        var currentMasterPassphrase = CurrentMasterPassphrase;
        var newMasterPassphrase = NewMasterPassphrase;
        CurrentMasterPassphrase = NewMasterPassphrase = ConfirmNewMasterPassphrase = string.Empty;
        IsBusy = true;
        try
        {
            await _vault.ChangeMasterPassphraseAsync(currentMasterPassphrase, newMasterPassphrase);
            _sessionSecurity.Clear();
            await _vault.LockAsync();
            try { await _clipboard.ClearAsync(); }
            catch (Exception exception) { _exceptions.Report("Settings.ChangeMasterPassphrase.Clipboard", exception); }
            StatusMessage = SettingsText("SettingsChangeMasterSuccess");
            await Shell.Current.GoToAsync("//unlock");
        }
        catch (CipherNest.Application.Exceptions.VaultAuthenticationException)
        {
            StatusMessage = SettingsText("SettingsChangeMasterAuthFailure");
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.ChangeMasterPassphrase", ex);
            StatusMessage = SettingsText("SettingsChangeMasterFailure");
        }
        finally
        {
            currentMasterPassphrase = string.Empty;
            newMasterPassphrase = string.Empty;
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteVaultAsync()
    {
        if (!string.Equals(DeletionConfirmationPhrase.Trim(), DeletePhrase, StringComparison.Ordinal)) { StatusMessage = SettingsFormat("SettingsDeleteExactPhraseFormat", DeletePhrase); return; }
        if (string.IsNullOrWhiteSpace(DeletionMasterPassphrase)) { StatusMessage = SettingsText("SettingsDeleteMasterRequired"); return; }

        var deletionMasterPassphrase = DeletionMasterPassphrase;
        DeletionMasterPassphrase = DeletionConfirmationPhrase = string.Empty;
        try
        {
            var confirm = await Shell.Current.DisplayAlertAsync(
                SettingsText("SettingsDeleteConfirmTitle"),
                SettingsText("SettingsDeleteConfirmBody"),
                SettingsText("SettingsDeleteConfirmAccept"),
                SettingsText("CancelButton"));
            if (!confirm) return;

            IsBusy = true;
            try
            {
                await _vault.DeleteVaultAsync(deletionMasterPassphrase);
                await _biometrics.ClearSecondarySecretAsync();
                _sessionSecurity.Clear();
                StatusMessage = string.Empty;
                await Shell.Current.GoToAsync("//onboarding");
            }
            catch (CipherNest.Application.Exceptions.VaultAuthenticationException)
            {
                StatusMessage = SettingsText("SettingsDeleteAuthFailure");
            }
            catch (Exception ex)
            {
                _exceptions.Report("Settings.DeleteVault", ex);
                StatusMessage = SettingsText("SettingsDeleteFailure");
            }
            finally { IsBusy = false; }
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.DeleteVault.Confirm", ex);
            StatusMessage = SettingsText("SettingsDeleteConfirmFailure");
        }
        finally
        {
            deletionMasterPassphrase = string.Empty;
        }
    }

    [RelayCommand] private async Task TransferAsync() => await Shell.Current.GoToAsync("//transfer");
    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return $"{value:0.##} {units[unit]}";
    }

    private static void ApplyTheme(AppThemePreference theme)
    {
        if (Microsoft.Maui.Controls.Application.Current is null) return;
        Microsoft.Maui.Controls.Application.Current.UserAppTheme = theme switch { AppThemePreference.Light => AppTheme.Light, AppThemePreference.Dark => AppTheme.Dark, _ => AppTheme.Unspecified };
    }
}
