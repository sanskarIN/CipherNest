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
    [ObservableProperty] private AppThemePreference selectedTheme;
    [ObservableProperty] private int lockTimeoutSeconds = 60;
    [ObservableProperty] private bool lockOnBackground = true;
    [ObservableProperty] private int clipboardClearSeconds = 30;
    [ObservableProperty] private bool screenshotProtection = true;
    [ObservableProperty] private bool biometricUnlockEnabled;
    [ObservableProperty] private bool biometricAvailable;
    [ObservableProperty] private bool reducedMotion;
    [ObservableProperty] private bool largerInterface;
    [ObservableProperty] private int trashRetentionDays = 30;
    [ObservableProperty] private int backupReminderDays = 7;
    [ObservableProperty] private bool reviewRemindersEnabled = true;
    [ObservableProperty] private int reviewReminderLeadDays = 7;
    [ObservableProperty] private int requireMasterPassphraseAfterHours = 24;
    [ObservableProperty] private string backupPassphrase = string.Empty;
    [ObservableProperty] private string currentMasterPassphrase = string.Empty;
    [ObservableProperty] private string newMasterPassphrase = string.Empty;
    [ObservableProperty] private string confirmNewMasterPassphrase = string.Empty;
    [ObservableProperty] private string deletionMasterPassphrase = string.Empty;
    [ObservableProperty] private string deletionConfirmationPhrase = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string screenshotSupportMessage = string.Empty;
    [ObservableProperty] private string biometricSupportMessage = string.Empty;
    [ObservableProperty] private string storageUsageMessage = "Calculating local storage…";
    [ObservableProperty] private bool isBusy;

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
        ScreenshotSupportMessage = screenshots.IsSupported ? "Screenshot blocking is supported by the current platform implementation." : "Reliable app-level screenshot blocking is not available through the current platform implementation; secret masking still applies.";
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
                ? (configured ? "Biometric unlock is configured. The master passphrase is still required for sensitive settings and periodically according to this security setting." : "Biometric authentication is available on this device but is not configured for CipherNest.")
                : "Biometric unlock is not available through the current platform/device implementation. Use the master passphrase.";
            ApplyTheme(_loadedPreferences.Theme);
            await _screenshots.ApplyAsync(_loadedPreferences.ScreenshotProtection);
            await RefreshStorageAsync();
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.Load", ex);
            StatusMessage = "Settings could not be loaded completely. CipherNest kept safe defaults where possible; return to the vault and retry.";
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
            StatusMessage = "Settings saved.";
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.Save", ex);
            StatusMessage = "Settings could not be saved or applied completely. Review the values and try again.";
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
        if (string.IsNullOrWhiteSpace(CurrentMasterPassphrase)) { StatusMessage = "Enter the current master passphrase before enabling biometric unlock."; return; }

        var masterPassphrase = CurrentMasterPassphrase;
        CurrentMasterPassphrase = string.Empty;
        try
        {
            if (!_biometrics.IsSupported || !await _biometrics.IsAvailableAsync()) { StatusMessage = "Biometric authentication is not available on this platform or device."; return; }
            if (!await _vault.ReauthenticateAsync(masterPassphrase)) { StatusMessage = "Master-passphrase confirmation failed."; return; }
            if (!await _biometrics.AuthenticateAsync("Confirm your identity to enable biometric vault unlock.")) { StatusMessage = "Biometric authentication was cancelled or failed."; return; }

            var bytes = RandomNumberGenerator.GetBytes(48);
            string secret;
            try { secret = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_'); }
            finally { CryptographicOperations.ZeroMemory(bytes); }

            IsBusy = true;
            try
            {
                await _biometrics.StoreSecondarySecretAsync(secret);
                try
                {
                    await _vault.EnableSecondaryUnlockAsync(masterPassphrase, secret);
                }
                catch
                {
                    try { await _biometrics.ClearSecondarySecretAsync(); }
                    catch (Exception rollbackException) { _exceptions.Report("Settings.BiometricEnable.Rollback", rollbackException); }
                    throw;
                }
                BiometricUnlockEnabled = true;
                _sessionSecurity.RecordMasterAuthentication(DateTimeOffset.UtcNow);
                _loadedPreferences = _loadedPreferences with { BiometricUnlockEnabled = true };
                await _settings.SaveAsync(_loadedPreferences);
                BiometricSupportMessage = "Biometric unlock is configured. CipherNest stores an independent random secondary secret in OS secure storage; it does not store the master passphrase.";
                StatusMessage = "Biometric unlock enabled.";
            }
            finally { secret = string.Empty; IsBusy = false; }
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.BiometricEnable", ex);
            BiometricUnlockEnabled = false;
            StatusMessage = "Biometric unlock could not be enabled safely. Use the master passphrase and try again after checking device biometric availability.";
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
        if (string.IsNullOrWhiteSpace(CurrentMasterPassphrase)) { StatusMessage = "Enter the current master passphrase before disabling biometric unlock."; return; }

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
            BiometricSupportMessage = "Biometric unlock is disabled.";
            StatusMessage = "Biometric unlock disabled.";
        }
        catch (CipherNest.Application.Exceptions.VaultAuthenticationException)
        {
            StatusMessage = "Master-passphrase confirmation failed.";
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.BiometricDisable", ex);
            StatusMessage = "Biometric unlock could not be disabled completely. Keep using the master passphrase and retry after checking secure-storage access.";
        }
        finally { masterPassphrase = string.Empty; IsBusy = false; }
    }

    [RelayCommand]
    private async Task ExportBackupAsync()
    {
        if (!_vault.IsUnlocked) { await Shell.Current.GoToAsync("//unlock"); return; }
        if (BackupPassphrase.Length is < 12 or > MaximumPassphraseCharacters) { StatusMessage = $"Use a backup passphrase between 12 and {MaximumPassphraseCharacters:N0} characters."; return; }

        var backupPassphrase = BackupPassphrase;
        BackupPassphrase = string.Empty;
        try
        {
            var confirm = await Shell.Current.DisplayAlertAsync("Create a consistent encrypted backup?", "CipherNest will lock the vault before taking the database and attachment snapshot so edits cannot race with the backup. You will unlock again afterward.", "Lock and back up", "Cancel");
            if (!confirm) return;

            IsBusy = true;
            try
            {
                await _vault.LockAsync();
                var directory = Path.Combine(FileSystem.Current.AppDataDirectory, "Backups");
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, $"CipherNest-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}{AppConstants.BackupExtension}");
                await _backup.ExportEncryptedAsync(path, backupPassphrase);
                _loadedPreferences = (await _settings.LoadAsync()) with { LastSuccessfulBackupUtc = DateTimeOffset.UtcNow };
                await _settings.SaveAsync(_loadedPreferences);
                StatusMessage = "Authenticated encrypted backup created, including encrypted attachments. The vault remains locked. Keep the backup passphrase separately and periodically test restore using disposable data.";
                await Share.Default.RequestAsync(new ShareFileRequest("CipherNest encrypted backup", new ShareFile(path)));
                await Shell.Current.GoToAsync("//unlock");
            }
            finally { IsBusy = false; }
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.BackupExport", ex);
            StatusMessage = "Encrypted backup or sharing could not be completed safely. The vault remains protected; review storage access and try again.";
        }
        finally
        {
            backupPassphrase = string.Empty;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (BackupPassphrase.Length is < 12 or > MaximumPassphraseCharacters) { StatusMessage = $"Enter a backup passphrase between 12 and {MaximumPassphraseCharacters:N0} characters before restoring."; return; }

        var backupPassphrase = BackupPassphrase;
        BackupPassphrase = string.Empty;
        string? tempPath = null;
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select a CipherNest encrypted backup" });
            if (file is null) return;
            var confirm = await Shell.Current.DisplayAlertAsync("Restore backup?", "The current vault database and attachment set will be replaced only after the backup container is authenticated and staged. Keep a separate backup before replacing important data.", "Restore", "Cancel");
            if (!confirm) return;

            IsBusy = true;
            tempPath = Path.Combine(FileSystem.Current.CacheDirectory, $"restore-{Guid.NewGuid():N}{AppConstants.BackupExtension}");
            try
            {
                await _vault.LockAsync();
                await using (var source = await file.OpenReadAsync())
                await using (var destination = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true)) await source.CopyToAsync(destination);
                await _backup.RestoreEncryptedAsync(tempPath, backupPassphrase);
                await _biometrics.ClearSecondarySecretAsync();
                _sessionSecurity.Clear();
                _loadedPreferences = (await _settings.LoadAsync()) with { BiometricUnlockEnabled = false };
                await _settings.SaveAsync(_loadedPreferences);
                BiometricUnlockEnabled = false;
                StatusMessage = "Backup restored. Unlock the restored vault with its master passphrase or recovery key. Biometric unlock was disabled locally because restored vault metadata may not match this device's secure-storage entry.";
                await Shell.Current.GoToAsync("//unlock");
            }
            finally { IsBusy = false; }
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.BackupRestore", ex);
            StatusMessage = "Backup selection, confirmation, restore, or staging failed safely. The active vault was not intentionally replaced by this failed restore attempt.";
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
        if (string.IsNullOrWhiteSpace(CurrentMasterPassphrase)) { StatusMessage = "Enter the current master passphrase."; return; }
        if (!string.Equals(NewMasterPassphrase, ConfirmNewMasterPassphrase, StringComparison.Ordinal)) { StatusMessage = "The new passphrase confirmation does not match."; return; }
        if (NewMasterPassphrase.Length > MaximumPassphraseCharacters) { StatusMessage = $"The new master passphrase cannot exceed {MaximumPassphraseCharacters:N0} characters."; return; }
        var strength = _passwordGenerator.Evaluate(NewMasterPassphrase); if (NewMasterPassphrase.Length < 12 || strength.Score < 3) { StatusMessage = $"Choose a stronger new master passphrase. Current estimate: {strength.Label}."; return; }

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
            StatusMessage = "Master passphrase changed. CipherNest ended the current security session; unlock again with the new master passphrase before biometric convenience unlock can resume. Create a fresh encrypted backup after security-sensitive changes.";
            await Shell.Current.GoToAsync("//unlock");
        }
        catch (CipherNest.Application.Exceptions.VaultAuthenticationException)
        {
            StatusMessage = "Master passphrase was not changed because current-master authentication failed.";
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.ChangeMasterPassphrase", ex);
            StatusMessage = "Master passphrase was not changed because the requested security transition could not be completed safely.";
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
        if (!string.Equals(DeletionConfirmationPhrase.Trim(), DeletePhrase, StringComparison.Ordinal)) { StatusMessage = $"Type exactly {DeletePhrase} before deleting the vault."; return; }
        if (string.IsNullOrWhiteSpace(DeletionMasterPassphrase)) { StatusMessage = "Confirm the current master passphrase. Recovery keys are not accepted for vault deletion."; return; }

        var deletionMasterPassphrase = DeletionMasterPassphrase;
        DeletionMasterPassphrase = DeletionConfirmationPhrase = string.Empty;
        try
        {
            var confirm = await Shell.Current.DisplayAlertAsync("Permanently delete this local vault?", "This removes CipherNest's local encrypted database and attachment files. Flash storage, filesystem snapshots, operating-system backups, shared exports, and forensic remnants can remain outside CipherNest's control. This action cannot be undone from the app.", "Delete local vault", "Cancel");
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
                StatusMessage = "Vault deletion was cancelled because master-passphrase confirmation failed.";
            }
            catch (Exception ex)
            {
                _exceptions.Report("Settings.DeleteVault", ex);
                StatusMessage = "Vault deletion or local secure-storage cleanup could not finish safely. CipherNest did not report the operation as complete.";
            }
            finally { IsBusy = false; }
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.DeleteVault.Confirm", ex);
            StatusMessage = "Vault-deletion confirmation could not be shown safely. No deletion was started.";
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
