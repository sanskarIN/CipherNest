using System.Collections.ObjectModel;
using CipherNest.Application.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class TransferViewModel : ObservableObject
{
    private const string ExportPhrase = "EXPORT PLAINTEXT";
    private readonly IPlaintextTransferService _transfer;
    private readonly IVaultService _vault;
    private FileResult? _selectedCsv;

    public ObservableCollection<string> Headers { get; } = [];
    [ObservableProperty] private string? titleColumn;
    [ObservableProperty] private string? usernameColumn;
    [ObservableProperty] private string? secretColumn;
    [ObservableProperty] private string? urlColumn;
    [ObservableProperty] private string? notesColumn;
    [ObservableProperty] private string? tagsColumn;
    [ObservableProperty] private string? collectionColumn;
    [ObservableProperty] private string? typeColumn;
    [ObservableProperty] private string selectedFileName = "No CSV selected.";
    [ObservableProperty] private string exportMasterPassphrase = string.Empty;
    [ObservableProperty] private string exportConfirmationPhrase = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    public TransferViewModel(IPlaintextTransferService transfer, IVaultService vault)
    {
        _transfer = transfer;
        _vault = vault;
    }

    [RelayCommand]
    private async Task PickCsvAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select a CSV file to map and import" });
        if (result is null) return;
        IsBusy = true;
        try
        {
            await using var stream = await result.OpenReadAsync();
            var headers = await _transfer.ReadHeadersAsync(stream);
            Headers.Clear();
            foreach (var header in headers) Headers.Add(header);
            _selectedCsv = result;
            SelectedFileName = result.FileName;
            TitleColumn = Guess(headers, "title", "name");
            UsernameColumn = Guess(headers, "username", "user", "email");
            SecretColumn = Guess(headers, "password", "secret");
            UrlColumn = Guess(headers, "url", "website");
            NotesColumn = Guess(headers, "notes", "note");
            TagsColumn = Guess(headers, "tags", "tag");
            CollectionColumn = Guess(headers, "collection", "folder");
            TypeColumn = Guess(headers, "type");
            StatusMessage = "Review every mapping before importing. Unmapped columns are ignored.";
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            StatusMessage = $"CSV could not be opened safely: {ex.Message}";
            _selectedCsv = null;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (_selectedCsv is null || string.IsNullOrWhiteSpace(TitleColumn))
        {
            StatusMessage = "Select a CSV and map its title column first.";
            return;
        }
        var confirmed = await Shell.Current.DisplayAlertAsync("Import plaintext CSV?", "The selected CSV is plaintext outside CipherNest. Imported fields will be encrypted in the vault, but CipherNest cannot remove the original source file. Review the mappings first.", "Import", "Cancel");
        if (!confirmed) return;
        IsBusy = true;
        try
        {
            await using var stream = await _selectedCsv.OpenReadAsync();
            var result = await _transfer.ImportCsvAsync(stream, new CsvImportMapping(TitleColumn!, UsernameColumn, SecretColumn, UrlColumn, NotesColumn, TagsColumn, CollectionColumn, TypeColumn));
            StatusMessage = $"Imported {result.Imported} item(s); skipped {result.Skipped}. " + (result.Warnings.Count == 0 ? string.Empty : string.Join(" ", result.Warnings.Take(3)));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            StatusMessage = $"Import stopped safely: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ExportPlaintextAsync()
    {
        if (!string.Equals(ExportConfirmationPhrase.Trim(), ExportPhrase, StringComparison.Ordinal))
        {
            StatusMessage = $"Type exactly {ExportPhrase} to acknowledge that the export will contain plaintext secrets.";
            return;
        }

        var authenticated = await _vault.ReauthenticateAsync(ExportMasterPassphrase);
        ExportMasterPassphrase = string.Empty;
        if (!authenticated)
        {
            StatusMessage = "Master-passphrase confirmation failed. Recovery keys are not accepted for plaintext export confirmation.";
            return;
        }

        var confirmed = await Shell.Current.DisplayAlertAsync("Create plaintext export?", "This file will contain readable vault fields and may be copied by the share target, backups, search indexing, antivirus, or the operating system. Encrypted backup is safer. Continue only if you need plaintext interoperability.", "Export plaintext", "Cancel");
        if (!confirmed)
        {
            ExportConfirmationPhrase = string.Empty;
            return;
        }

        IsBusy = true;
        try
        {
            var directory = Path.Combine(FileSystem.Current.CacheDirectory, "plaintext-exports");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"CipherNest-PLAINTEXT-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.csv");
            await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
            {
                await _transfer.ExportCsvAsync(stream);
            }
            ExportConfirmationPhrase = string.Empty;
            StatusMessage = "Plaintext CSV created in temporary app cache. After sharing, delete every copy you no longer need and use 'Clean plaintext export cache'. Attachments are not included in plaintext CSV exports.";
            await Share.Default.RequestAsync(new ShareFileRequest("CipherNest plaintext export — sensitive", new ShareFile(path)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            StatusMessage = $"Plaintext export failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task CleanPlaintextCacheAsync()
    {
        var directory = Path.Combine(FileSystem.Current.CacheDirectory, "plaintext-exports");
        try
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            StatusMessage = "CipherNest's plaintext export cache was removed. Copies created by other apps, backups, filesystem snapshots, or share targets remain outside CipherNest's control.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusMessage = $"Cache cleanup could not finish: {ex.Message}";
        }
        return Task.CompletedTask;
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//settings");

    private static string? Guess(IReadOnlyList<string> headers, params string[] names) => headers.FirstOrDefault(h => names.Any(n => h.Equals(n, StringComparison.OrdinalIgnoreCase)));
}
