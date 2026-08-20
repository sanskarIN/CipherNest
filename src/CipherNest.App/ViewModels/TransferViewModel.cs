using System.Collections.ObjectModel;
using System.Globalization;
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
    private readonly ILocalizationService _localization;
    private FileResult? _selectedCsv;

    public ObservableCollection<string> Headers { get; } = [];

    [ObservableProperty]
    public partial string? TitleColumn { get; set; }

    [ObservableProperty]
    public partial string? UsernameColumn { get; set; }

    [ObservableProperty]
    public partial string? SecretColumn { get; set; }

    [ObservableProperty]
    public partial string? UrlColumn { get; set; }

    [ObservableProperty]
    public partial string? NotesColumn { get; set; }

    [ObservableProperty]
    public partial string? TagsColumn { get; set; }

    [ObservableProperty]
    public partial string? CollectionColumn { get; set; }

    [ObservableProperty]
    public partial string? TypeColumn { get; set; }

    [ObservableProperty]
    public partial string SelectedFileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ExportMasterPassphrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ExportConfirmationPhrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public TransferViewModel(IPlaintextTransferService transfer, IVaultService vault, IPrivacySafeExceptionReporter exceptions, ILocalizationService localization)
    {
        _transfer = transfer;
        _vault = vault;
        _exceptions = exceptions;
        _localization = localization;
        SelectedFileName = Text("TransferNoCsvSelected");
    }

    [RelayCommand]
    private async Task PickCsvAsync()
    {
        IsBusy = true;
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = Text("TransferFilePickerTitle") });
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
            StatusMessage = Text("TransferReviewMappingsStatus");
        }
        catch (Exception ex)
        {
            _exceptions.Report("Transfer.PickCsv", ex);
            ResetCsvSelection();
            StatusMessage = Text("TransferCsvSelectFailureStatus");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (_selectedCsv is null || string.IsNullOrWhiteSpace(TitleColumn))
        {
            StatusMessage = Text("TransferSelectAndMapTitleStatus");
            return;
        }

        bool confirmed;
        try
        {
            confirmed = await Shell.Current.DisplayAlertAsync(
                Text("TransferImportConfirmTitle"),
                Text("TransferImportConfirmBody"),
                Text("TransferImportConfirmAccept"),
                Text("CancelButton"));
        }
        catch (Exception ex)
        {
            _exceptions.Report("Transfer.ImportConfirm", ex);
            StatusMessage = Text("TransferImportConfirmFailureStatus");
            return;
        }
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await using var stream = await _selectedCsv.OpenReadAsync();
            var result = await _transfer.ImportCsvAsync(stream, new CsvImportMapping(TitleColumn!, UsernameColumn, SecretColumn, UrlColumn, NotesColumn, TagsColumn, CollectionColumn, TypeColumn));
            StatusMessage = result.Warnings.Count == 0
                ? Format("TransferImportResultFormat", result.Imported, result.Skipped)
                : Format("TransferImportResultWithWarningsFormat", result.Imported, result.Skipped);
        }
        catch (Exception ex)
        {
            _exceptions.Report("Transfer.ImportCsv", ex);
            StatusMessage = Text("TransferImportFailureStatus");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportPlaintextAsync()
    {
        if (!string.Equals(ExportConfirmationPhrase.Trim(), ExportPhrase, StringComparison.Ordinal))
        {
            StatusMessage = Format("TransferExportPhraseRequiredFormat", ExportPhrase);
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
            StatusMessage = Text("TransferMasterConfirmFailureStatus");
            return;
        }
        finally
        {
            ExportMasterPassphrase = string.Empty;
        }

        if (!authenticated)
        {
            StatusMessage = Text("TransferMasterFailedStatus");
            return;
        }

        bool confirmed;
        try
        {
            confirmed = await Shell.Current.DisplayAlertAsync(
                Text("TransferExportConfirmTitle"),
                Text("TransferExportConfirmBody"),
                Text("TransferExportConfirmAccept"),
                Text("CancelButton"));
        }
        catch (Exception ex)
        {
            _exceptions.Report("Transfer.ExportPlaintext.Confirm", ex);
            StatusMessage = Text("TransferExportConfirmFailureStatus");
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
            StatusMessage = Text("TransferExportTemporaryStatus");
            await Share.Default.RequestAsync(new ShareFileRequest(Text("TransferShareTitle"), new ShareFile(plaintextPath)));
        }
        catch (Exception ex)
        {
            _exceptions.Report("Transfer.ExportPlaintext", ex);
            StatusMessage = Text("TransferExportFailureStatus");
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
                    StatusMessage += Text("TransferCleanupWarningSuffix");
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
            StatusMessage = Text("TransferCacheCleanedStatus");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _exceptions.Report("Transfer.CleanPlaintextCache", ex);
            StatusMessage = Text("TransferCacheCleanFailureStatus");
        }
        return Task.CompletedTask;
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//settings");

    private void ResetCsvSelection()
    {
        _selectedCsv = null;
        Headers.Clear();
        SelectedFileName = Text("TransferNoCsvSelected");
        TitleColumn = null;
        UsernameColumn = null;
        SecretColumn = null;
        UrlColumn = null;
        NotesColumn = null;
        TagsColumn = null;
        CollectionColumn = null;
        TypeColumn = null;
    }

    private string Text(string key) => _localization.Get(key);

    private string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, Text(key), args);

    private static string? Guess(IReadOnlyList<string> headers, params string[] names) => headers.FirstOrDefault(h => names.Any(n => h.Equals(n, StringComparison.OrdinalIgnoreCase)));
}
