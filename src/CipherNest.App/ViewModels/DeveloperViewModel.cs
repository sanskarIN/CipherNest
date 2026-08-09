using System.Text;
using CipherNest.Application.Abstractions;
using CipherNest.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class DeveloperViewModel : ObservableObject
{
    private readonly IVaultStore _store;
    private readonly IVaultService _vault;
    private readonly ISettingsStore _settings;

    [ObservableProperty] private string databaseInfo = string.Empty;
    [ObservableProperty] private string storageInfo = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;

    public string CryptoInfo => $"Cryptographic envelope version: {AppConstants.CryptoFormatVersion}";
    public string SchemaInfo => $"Database migration/schema version: {AppConstants.DatabaseSchemaVersion}";
    public string Dependencies => "Microsoft.Maui.Controls; CommunityToolkit.Mvvm; Microsoft.Data.Sqlite; Konscious.Security.Cryptography.Argon2; Microsoft.Extensions.Logging.Debug. Versions are centrally pinned in Directory.Packages.props.";

    public DeveloperViewModel(IVaultStore store, IVaultService vault, ISettingsStore settings)
    {
        _store = store;
        _vault = vault;
        _settings = settings;
    }

    [RelayCommand]
    public Task LoadAsync()
    {
        var db = _store.DatabasePath;
        DatabaseInfo = File.Exists(db) ? $"Encrypted database container: {new FileInfo(db).Length:N0} bytes" : "Encrypted database container: not present";
        var attachmentDir = Path.Combine(Path.GetDirectoryName(db)!, AppConstants.AttachmentDirectoryName);
        var encryptedAttachmentBytes = Directory.Exists(attachmentDir) ? Directory.EnumerateFiles(attachmentDir, "*.cna").Sum(static f => new FileInfo(f).Length) : 0L;
        var encryptedAttachmentCount = Directory.Exists(attachmentDir) ? Directory.EnumerateFiles(attachmentDir, "*.cna").Count() : 0;
        StorageInfo = $"Encrypted attachment containers: {encryptedAttachmentCount:N0}; {encryptedAttachmentBytes:N0} bytes. This screen never reads decrypted records.";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportDiagnosticsAsync()
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
        var path = Path.Combine(FileSystem.Current.CacheDirectory, $"CipherNest-redacted-diagnostics-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.txt");
        await File.WriteAllTextAsync(path, sb.ToString());
        StatusMessage = "Redacted diagnostic file created. Review it before sharing; platform metadata is still included.";
        await Share.Default.RequestAsync(new ShareFileRequest("CipherNest redacted diagnostics", new ShareFile(path)));
    }

    [RelayCommand]
    private async Task SimulateLockAsync()
    {
        await _vault.LockAsync();
        StatusMessage = "Vault lock lifecycle simulated. Returning to unlock screen.";
        await Shell.Current.GoToAsync("//unlock");
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//about");
}
