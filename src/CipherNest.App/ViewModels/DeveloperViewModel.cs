using System.Text;
using CipherNest.Application.Abstractions;
using CipherNest.App.Services;
using CipherNest.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class DeveloperViewModel : ObservableObject
{
    private readonly IVaultStore _store;
    private readonly IVaultService _vault;
    private readonly ISettingsStore _settings;
    private readonly IPrivacySafeExceptionReporter _exceptions;

    [ObservableProperty]
    public partial string DatabaseInfo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StorageInfo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public string CryptoInfo => $"Cryptographic envelope version: {AppConstants.CryptoFormatVersion}";
    public string SchemaInfo => $"Database migration/schema version: {AppConstants.DatabaseSchemaVersion}";
    public string Dependencies => "Microsoft.Maui.Controls; CommunityToolkit.Mvvm; Microsoft.Data.Sqlite; Konscious.Security.Cryptography.Argon2; Microsoft.Extensions.Logging.Debug. Versions are centrally pinned in Directory.Packages.props.";

    public DeveloperViewModel(
        IVaultStore store,
        IVaultService vault,
        ISettingsStore settings,
        IPrivacySafeExceptionReporter exceptions)
    {
        _store = store;
        _vault = vault;
        _settings = settings;
        _exceptions = exceptions;
    }

    [RelayCommand]
    public Task LoadAsync()
    {
        try
        {
            var db = _store.DatabasePath;
            DatabaseInfo = File.Exists(db)
                ? $"Encrypted database container: {new FileInfo(db).Length:N0} bytes"
                : "Encrypted database container: not present";

            var databaseDirectory = Path.GetDirectoryName(db);
            if (string.IsNullOrWhiteSpace(databaseDirectory))
            {
                throw new InvalidOperationException("The vault database directory is unavailable.");
            }

            var attachmentDir = Path.Combine(databaseDirectory, AppConstants.AttachmentDirectoryName);
            var attachmentFiles = Directory.Exists(attachmentDir)
                ? Directory.EnumerateFiles(attachmentDir, "*.cna", SearchOption.TopDirectoryOnly).ToArray()
                : [];
            var encryptedAttachmentBytes = attachmentFiles.Sum(static file => new FileInfo(file).Length);
            StorageInfo = $"Encrypted attachment containers: {attachmentFiles.Length:N0}; {encryptedAttachmentBytes:N0} bytes. This screen never reads decrypted records.";
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _exceptions.Report("Developer.Load", ex);
            DatabaseInfo = "Encrypted database container: unavailable";
            StorageInfo = "Encrypted attachment container statistics are unavailable. This screen never reads decrypted records.";
            StatusMessage = "Developer storage metadata could not be read safely.";
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportDiagnosticsAsync()
    {
        string? path = null;
        var shareCompleted = false;
        var operationFailed = false;

        try
        {
            var preferences = await _settings.LoadAsync();
            var sb = new StringBuilder();
            sb.AppendLine("CipherNest Redacted Diagnostics");
            sb.AppendLine($"GeneratedUtc: {DateTimeOffset.UtcNow:O}");
            sb.AppendLine($"AppVersion: {AppConstants.Version}");
            sb.AppendLine($"CryptoFormat: {AppConstants.CryptoFormatVersion}");
            sb.AppendLine($"DatabaseSchema: {AppConstants.DatabaseSchemaVersion}");
            sb.AppendLine($"Platform: {DeviceInfo.Current.Platform}");
            sb.AppendLine($"OSVersion: {DeviceInfo.Current.VersionString}");
            sb.AppendLine($"DeviceIdiom: {DeviceInfo.Current.Idiom}");
            sb.AppendLine($"VaultUnlocked: {_vault.IsUnlocked}");
            sb.AppendLine($"LockOnBackground: {preferences.LockOnBackground}");
            sb.AppendLine($"LockTimeoutSeconds: {preferences.LockTimeoutSeconds}");
            sb.AppendLine($"ClipboardClearSeconds: {preferences.ClipboardClearSeconds}");
            sb.AppendLine($"ScreenshotProtectionPreference: {preferences.ScreenshotProtection}");
            sb.AppendLine(DatabaseInfo);
            sb.AppendLine(StorageInfo);
            sb.AppendLine("Vault records, titles, usernames, secrets, URLs, notes, tags, custom fields, keys, salts, nonces, recovery keys, database paths, attachment names, and backup passphrases are intentionally omitted.");

            path = Path.Combine(FileSystem.Current.CacheDirectory, $"CipherNest-redacted-diagnostics-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.txt");
            await File.WriteAllTextAsync(path, sb.ToString());
            StatusMessage = "Redacted diagnostic file created. Review it before sharing; platform metadata is still included.";
            await Share.Default.RequestAsync(new ShareFileRequest("CipherNest redacted diagnostics", new ShareFile(path)));
            shareCompleted = true;
        }
        catch (Exception ex)
        {
            operationFailed = true;
            _exceptions.Report("Developer.ExportDiagnostics", ex);
            StatusMessage = "Redacted diagnostics could not be created or shared safely. No vault contents were included.";
        }
        finally
        {
            var cleanupFailed = false;
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch (Exception ex)
                {
                    cleanupFailed = true;
                    _exceptions.Report("Developer.ExportDiagnostics.Cleanup", ex);
                }
            }

            if (cleanupFailed)
            {
                StatusMessage = shareCompleted
                    ? "The diagnostic share request completed, but CipherNest could not confirm deletion of its temporary cache copy. Use Settings → Storage & cache → Clear temporary cache."
                    : "Redacted diagnostics did not complete, and CipherNest could not confirm deletion of its temporary cache copy. Use Settings → Storage & cache → Clear temporary cache.";
            }
            else if (shareCompleted)
            {
                StatusMessage = "Redacted diagnostic share request completed and the temporary app-cache copy was deleted where permitted.";
            }
            else if (!operationFailed)
            {
                StatusMessage = "Redacted diagnostics ended before a share request was completed.";
            }
        }
    }

    [RelayCommand]
    private async Task SimulateLockAsync()
    {
        try
        {
            await _vault.LockAsync();
            StatusMessage = "Vault lock lifecycle simulated. Returning to unlock screen.";
            try
            {
                await Shell.Current.GoToAsync("//unlock");
            }
            catch (Exception ex)
            {
                _exceptions.Report("Developer.SimulateLock.Navigation", ex);
                StatusMessage = "The vault was locked, but the unlock screen could not be opened automatically.";
            }
        }
        catch (Exception ex)
        {
            _exceptions.Report("Developer.SimulateLock", ex);
            StatusMessage = "Vault lock simulation could not be completed safely.";
        }
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//about");
}
