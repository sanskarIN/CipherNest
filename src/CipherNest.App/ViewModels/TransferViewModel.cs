using System.Collections.ObjectModel;
using CipherNest.Application.Abstractions;
using CipherNest.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class TransferViewModel : ObservableObject
{
    private const string ExportPhrase = "EXPORT PLAINTEXT";
    private readonly IPlaintextTransferService _transfer;
    private readonly IVaultService _vault;
    private readonly IPrivacySafeExceptionReporter _exceptions;
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

    public TransferViewModel(IPlaintextTransferService transfer, IVaultService vault, IPrivacySafeExceptionReporter exceptions)
    {
        _transfer = transfer;
        _vault = vault;
        _exceptions = exceptions;
    }

    [RelayCommand]
    private async Task PickCsvAsync()
    {
        IsBusy = true;
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select a CSV file to map and import" });
            if (result is null) return;
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
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException)
        {
            _exceptions.Report("Transfer.PickCsv", ex);
            StatusMessage = "CSV could not be selected or opened safely. Check file access and format, then try again.";
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

        bool confirmed;
        try
        {
            confirmed = await Shell.Current.DisplayAlertAsync("Import plaintext CSV?", "The selected CSV is plaintext outside CipherNest. Imported fields will be encrypted in the vault, but CipherNest cannot remove the original source file. Review the mappings first.", "Import", "Cancel");
        }
        catch (Exception ex)
        {
            _exceptions.Report("Transfer.ImportConfirm", ex);
            StatusMessage = "Import confirmation could not be shown safely. No import was started.";
            return;
        }
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
            _exceptions.Report("Transfer.ImportCsv", ex);
            StatusMessage = "Import stopped safely. No additional rows will be imported from this file until you retry.";
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

        bool authenticated;
        try
        {
            authenticated = await _vault.ReauthenticateAsync(ExportMasterPassphrase);
        }
        catch (Exception ex)
        {
            _exceptions.Report("Transfer.ExportPlaintext.Reauthenticate", ex);
            StatusMessage = "Master-passphrase confirmation could not be completed safely. The plaintext export was not started.";
            return;
        }
        finally
        {
            ExportMasterPassphrase = string.Empty;
        }

        if (!authenticated)
        {
            StatusMessage = "Master-passphrase confirmation failed. Recovery keys are not accepted for plaintext export confirmation.";
            return;
        }

        bool confirmed;
        try
        {
            confirmed = await Shell.Current.DisplayAlertAsync("Create plaintext export?", "This file will contain readable vault fields and may be copied by the share target, backups, search indexing, antivirus, or the operating system. Encrypted backup is safer. Continue only if you need plaintext interoperability.", "Export plaintext", "Cancel");
        }
        catch (Exception ex)
        {
            _exceptions.Report("Transfer.ExportPlaintext.Confirm", ex);
            StatusMessage = "Plaintext-export confirmation could not be shown safely. No plaintext file was created.";
            return;
        }
        if (!confirmed)
        {
            ExportConfirmationPhrase = string.Empty;
            return;
        }

        ExportConfirmationPhrase = string.Empty;
        IsBusy = true;
        string? plaintextPath = null;
        try
        {
            var directory = Path.Combine(FileSystem.Current.CacheDirectory, "plaintext-exports");
            Directory.CreateDirectory(directory);
            plaintextPath = Path.Combine(directory, $"CipherNest-PLAINTEXT-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.csv");
            await using (var stream = new FileStream(plaintextPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
            {
                await _transfer.ExportCsvAsync(stream);
            }
            StatusMessage = "Plaintext CSV was created temporarily for the operating-system share flow. CipherNest will attempt to delete its staging copy as soon as sharing returns. Copies created by the receiving app remain outside CipherNest's control.";
            await Share.Default.RequestAsync(new ShareFileRequest("CipherNest plaintext export — sensitive", new ShareFile(plaintextPath)));
        }
        catch (Exception ex)
        {
            _exceptions.Report("Transfer.ExportPlaintext", ex);
            StatusMessage = "Plaintext export or sharing failed safely. Use encrypted backup unless plaintext interoperability is required.";
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(plaintextPath))
            {
                try
                {
                    if (File.Exists(plaintextPath)) File.Delete(plaintextPath);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _exceptions.Report("Transfer.ExportPlaintext.TempCleanup", ex);
                    StatusMessage += " CipherNest could not confirm removal of its temporary plaintext staging file; use 'Clean plaintext export cache' before sensitive work continues.";
                }
            }
            IsBusy = false;
        }
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
            _exceptions.Report("Transfer.CleanPlaintextCache", ex);
            StatusMessage = "CipherNest could not confirm complete removal of its plaintext export cache. Avoid sensitive work until storage access can be checked.";
        }
        return Task.CompletedTask;
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//settings");

    private static string? Guess(IReadOnlyList<string> headers, params string[] names) => headers.FirstOrDefault(h => names.Any(n => h.Equals(n, StringComparison.OrdinalIgnoreCase)));
}
