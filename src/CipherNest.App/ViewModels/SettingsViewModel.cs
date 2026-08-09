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
    private readonly ISettingsStore _settings;
    private readonly IBackupService _backup;
    private readonly IVaultService _vault;
    private readonly IScreenshotProtectionService _screenshots;
    private readonly IPasswordGenerator _passwordGenerator;

    public IReadOnlyList<AppThemePreference> Themes { get; } = Enum.GetValues<AppThemePreference>();
    [ObservableProperty] private AppThemePreference selectedTheme;
    [ObservableProperty] private int lockTimeoutSeconds = 60;
    [ObservableProperty] private bool lockOnBackground = true;
    [ObservableProperty] private int clipboardClearSeconds = 30;
    [ObservableProperty] private bool screenshotProtection = true;
    [ObservableProperty] private bool reducedMotion;
    [ObservableProperty] private bool largerInterface;
    [ObservableProperty] private int trashRetentionDays = 30;
    [ObservableProperty] private string backupPassphrase = string.Empty;
    [ObservableProperty] private string currentMasterPassphrase = string.Empty;
    [ObservableProperty] private string newMasterPassphrase = string.Empty;
    [ObservableProperty] private string confirmNewMasterPassphrase = string.Empty;
    [ObservableProperty] private string deletionMasterPassphrase = string.Empty;
    [ObservableProperty] private string deletionConfirmationPhrase = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string screenshotSupportMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    public SettingsViewModel(ISettingsStore settings, IBackupService backup, IVaultService vault, IScreenshotProtectionService screenshots, IPasswordGenerator passwordGenerator)
    {
        _settings = settings;
        _backup = backup;
        _vault = vault;
        _screenshots = screenshots;
        _passwordGenerator = passwordGenerator;
        ScreenshotSupportMessage = screenshots.IsSupported
            ? "Screenshot blocking is supported by the current platform implementation."
            : "Reliable app-level screenshot blocking is not available through the current platform implementation; secret masking still applies.";
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var p = await _settings.LoadAsync();
        SelectedTheme = p.Theme;
        LockTimeoutSeconds = p.LockTimeoutSeconds;
        LockOnBackground = p.LockOnBackground;
        ClipboardClearSeconds = p.ClipboardClearSeconds;
        ScreenshotProtection = p.ScreenshotProtection;
        ReducedMotion = p.ReducedMotion;
        LargerInterface = p.LargerInterface;
        TrashRetentionDays = p.TrashRetentionDays;
        ApplyTheme(p.Theme);
        await _screenshots.ApplyAsync(p.ScreenshotProtection);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        LockTimeoutSeconds = Math.Clamp(LockTimeoutSeconds, 5, 3600);
        ClipboardClearSeconds = Math.Clamp(ClipboardClearSeconds, 5, 300);
        TrashRetentionDays = Math.Clamp(TrashRetentionDays, 1, 365);
        var p = new AppPreferences
        {
            Theme = SelectedTheme,
            LockTimeoutSeconds = LockTimeoutSeconds,
            LockOnBackground = LockOnBackground,
            ClipboardClearSeconds = ClipboardClearSeconds,
            ScreenshotProtection = ScreenshotProtection,
            ReducedMotion = ReducedMotion,
            LargerInterface = LargerInterface,
            TrashRetentionDays = TrashRetentionDays
        };
        await _settings.SaveAsync(p);
        ApplyTheme(p.Theme);
        await _screenshots.ApplyAsync(p.ScreenshotProtection);
        StatusMessage = "Settings saved.";
    }

    [RelayCommand]
    private async Task ExportBackupAsync()
    {
        if (!_vault.IsUnlocked) { await Shell.Current.GoToAsync("//unlock"); return; }
        if (BackupPassphrase.Length < 12) { StatusMessage = "Use a backup passphrase of at least 12 characters."; return; }
        IsBusy = true;
        try
        {
            var directory = Path.Combine(FileSystem.Current.AppDataDirectory, "Backups");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"CipherNest-{DateTimeOffset.Now:yyyyMMdd-HHmmss}{AppConstants.BackupExtension}");
            await _backup.ExportEncryptedAsync(path, BackupPassphrase);
            BackupPassphrase = string.Empty;
            StatusMessage = "Authenticated encrypted backup created, including encrypted attachments. Keep the backup passphrase separately.";
            await Share.Default.RequestAsync(new ShareFileRequest("CipherNest encrypted backup", new ShareFile(path)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (BackupPassphrase.Length < 12) { StatusMessage = "Enter the backup passphrase before restoring."; return; }
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select a CipherNest encrypted backup" });
        if (file is null) return;
        var confirm = await Shell.Current.DisplayAlert("Restore backup?", "The current vault database and attachment set will be replaced only after the backup container is authenticated and staged. Keep a separate backup before replacing important data.", "Restore", "Cancel");
        if (!confirm) return;
        IsBusy = true;
        try
        {
            await _vault.LockAsync();
            await _backup.RestoreEncryptedAsync(file.FullPath, BackupPassphrase);
            BackupPassphrase = string.Empty;
            StatusMessage = "Backup restored. Unlock the restored vault with its master passphrase or recovery key.";
            await Shell.Current.GoToAsync("//unlock");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            StatusMessage = $"Restore failed safely: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ChangeMasterPassphraseAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentMasterPassphrase)) { StatusMessage = "Enter the current master passphrase."; return; }
        if (!string.Equals(NewMasterPassphrase, ConfirmNewMasterPassphrase, StringComparison.Ordinal)) { StatusMessage = "The new passphrase confirmation does not match."; return; }
        var strength = _passwordGenerator.Evaluate(NewMasterPassphrase);
        if (NewMasterPassphrase.Length < 12 || strength.Score < 3) { StatusMessage = $"Choose a stronger new master passphrase. Current estimate: {strength.Label}."; return; }
        IsBusy = true;
        try
        {
            await _vault.ChangeMasterPassphraseAsync(CurrentMasterPassphrase, NewMasterPassphrase);
            CurrentMasterPassphrase = NewMasterPassphrase = ConfirmNewMasterPassphrase = string.Empty;
            StatusMessage = "Master passphrase changed. Existing recovery key remains valid because it independently wraps the same vault key. Create a fresh encrypted backup after security-sensitive changes.";
        }
        catch (Exception ex) when (ex is CipherNest.Application.Exceptions.VaultAuthenticationException or ArgumentException)
        {
            StatusMessage = $"Master passphrase was not changed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteVaultAsync()
    {
        if (!string.Equals(DeletionConfirmationPhrase.Trim(), DeletePhrase, StringComparison.Ordinal))
        {
            StatusMessage = $"Type exactly {DeletePhrase} before deleting the vault.";
            return;
        }
        if (string.IsNullOrWhiteSpace(DeletionMasterPassphrase)) { StatusMessage = "Confirm the current master passphrase. Recovery keys are not accepted for vault deletion."; return; }
        var confirm = await Shell.Current.DisplayAlert("Permanently delete this local vault?", "This removes CipherNest's local encrypted database and attachment files. Flash storage, filesystem snapshots, operating-system backups, shared exports, and forensic remnants can remain outside CipherNest's control. This action cannot be undone from the app.", "Delete local vault", "Cancel");
        if (!confirm) return;
        IsBusy = true;
        try
        {
            await _vault.DeleteVaultAsync(DeletionMasterPassphrase);
            DeletionMasterPassphrase = DeletionConfirmationPhrase = string.Empty;
            StatusMessage = string.Empty;
            await Shell.Current.GoToAsync("//onboarding");
        }
        catch (CipherNest.Application.Exceptions.VaultAuthenticationException)
        {
            StatusMessage = "Vault deletion was cancelled because master-passphrase confirmation failed.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Vault deletion could not finish: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] private async Task TransferAsync() => await Shell.Current.GoToAsync("//transfer");
    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");

    private static void ApplyTheme(AppThemePreference theme)
    {
        if (Microsoft.Maui.Controls.Application.Current is null) return;
        Microsoft.Maui.Controls.Application.Current.UserAppTheme = theme switch { AppThemePreference.Light => AppTheme.Light, AppThemePreference.Dark => AppTheme.Dark, _ => AppTheme.Unspecified };
    }
}
