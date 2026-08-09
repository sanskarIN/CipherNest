using CipherNest.Application.Abstractions;
using CipherNest.App.Services;
using CipherNest.Domain.Models;
using CipherNest.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;
    private readonly IBackupService _backup;
    private readonly IVaultService _vault;
    private readonly IScreenshotProtectionService _screenshots;

    public IReadOnlyList<AppThemePreference> Themes { get; } = Enum.GetValues<AppThemePreference>();
    [ObservableProperty] private AppThemePreference selectedTheme;
    [ObservableProperty] private int lockTimeoutSeconds = 60;
    [ObservableProperty] private bool lockOnBackground = true;
    [ObservableProperty] private int clipboardClearSeconds = 30;
    [ObservableProperty] private bool screenshotProtection = true;
    [ObservableProperty] private bool reducedMotion;
    [ObservableProperty] private bool largerInterface;
    [ObservableProperty] private string backupPassphrase = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string screenshotSupportMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    public SettingsViewModel(ISettingsStore settings, IBackupService backup, IVaultService vault, IScreenshotProtectionService screenshots)
    {
        _settings = settings;
        _backup = backup;
        _vault = vault;
        _screenshots = screenshots;
        ScreenshotSupportMessage = screenshots.IsSupported
            ? "Screenshot blocking is supported on this platform."
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
        ApplyTheme(p.Theme);
        await _screenshots.ApplyAsync(p.ScreenshotProtection);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        LockTimeoutSeconds = Math.Clamp(LockTimeoutSeconds, 5, 3600);
        ClipboardClearSeconds = Math.Clamp(ClipboardClearSeconds, 5, 300);
        var p = new AppPreferences
        {
            Theme = SelectedTheme,
            LockTimeoutSeconds = LockTimeoutSeconds,
            LockOnBackground = LockOnBackground,
            ClipboardClearSeconds = ClipboardClearSeconds,
            ScreenshotProtection = ScreenshotProtection,
            ReducedMotion = ReducedMotion,
            LargerInterface = LargerInterface
        };
        await _settings.SaveAsync(p);
        ApplyTheme(p.Theme);
        await _screenshots.ApplyAsync(p.ScreenshotProtection);
        StatusMessage = "Settings saved.";
    }

    [RelayCommand]
    private async Task ExportBackupAsync()
    {
        if (!_vault.IsUnlocked)
        {
            await Shell.Current.GoToAsync("//unlock");
            return;
        }
        if (BackupPassphrase.Length < 12)
        {
            StatusMessage = "Use a backup passphrase of at least 12 characters.";
            return;
        }
        IsBusy = true;
        try
        {
            var directory = Path.Combine(FileSystem.Current.AppDataDirectory, "Backups");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"CipherNest-{DateTimeOffset.Now:yyyyMMdd-HHmmss}{AppConstants.BackupExtension}");
            await _backup.ExportEncryptedAsync(path, BackupPassphrase);
            BackupPassphrase = string.Empty;
            StatusMessage = "Encrypted backup created. Keep its passphrase separately.";
            await Share.Default.RequestAsync(new ShareFileRequest("CipherNest encrypted backup", new ShareFile(path)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackupAsync()
    {
        if (BackupPassphrase.Length < 12)
        {
            StatusMessage = "Enter the backup passphrase before restoring.";
            return;
        }
        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select a CipherNest encrypted backup" });
        if (file is null) return;
        var confirm = await Shell.Current.DisplayAlert("Restore backup?", "The current local vault database will be replaced after the backup is authenticated and validated.", "Restore", "Cancel");
        if (!confirm) return;
        IsBusy = true;
        try
        {
            await _vault.LockAsync();
            await _backup.RestoreEncryptedAsync(file.FullPath, BackupPassphrase);
            BackupPassphrase = string.Empty;
            StatusMessage = "Backup restored. Unlock the restored vault with its master passphrase.";
            await Shell.Current.GoToAsync("//unlock");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            StatusMessage = $"Restore failed safely: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteVaultAsync()
    {
        StatusMessage = "Full vault deletion is intentionally performed through the operating system app-data reset/uninstall flow in this release to avoid a partial-deletion implementation that could provide false assurances about secure erasure.";
        await Task.CompletedTask;
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");

    private static void ApplyTheme(AppThemePreference theme)
    {
        if (Microsoft.Maui.Controls.Application.Current is null) return;
        Microsoft.Maui.Controls.Application.Current.UserAppTheme = theme switch
        {
            AppThemePreference.Light => AppTheme.Light,
            AppThemePreference.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
