using System.Collections.ObjectModel;
using CipherNest.Application.Abstractions;
using CipherNest.App.Services;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class ItemEditorViewModel : ObservableObject, IQueryAttributable
{
    private readonly IVaultService _vault;
    private readonly IClipboardSecurityService _clipboard;
    private readonly ISettingsStore _settings;
    private readonly ISafeNoteMarkupService _noteMarkup;
    private VaultItem? _existing;

    public IReadOnlyList<VaultItemType> Types { get; } = Enum.GetValues<VaultItemType>();
    public ObservableCollection<AttachmentReference> Attachments { get; } = [];
    [ObservableProperty] private VaultItemType selectedType = VaultItemType.Login;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string secret = string.Empty;
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string notePreview = string.Empty;
    [ObservableProperty] private string checklistDraft = string.Empty;
    [ObservableProperty] private bool showNotePreview;
    [ObservableProperty] private string collection = string.Empty;
    [ObservableProperty] private string tags = string.Empty;
    [ObservableProperty] private string customFieldsText = string.Empty;
    [ObservableProperty] private bool isFavorite;
    [ObservableProperty] private bool isSecretVisible;
    [ObservableProperty] private bool hasReviewDate;
    [ObservableProperty] private DateTime reviewDate = DateTime.Today.AddMonths(6);
    [ObservableProperty] private bool requiresReauthentication;
    [ObservableProperty] private bool isReauthenticationRequired;
    [ObservableProperty] private string reauthenticationPassphrase = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isExisting;

    public ItemEditorViewModel(IVaultService vault, IClipboardSecurityService clipboard, ISettingsStore settings, ISafeNoteMarkupService noteMarkup)
    {
        _vault = vault;
        _clipboard = clipboard;
        _settings = settings;
        _noteMarkup = noteMarkup;
    }

    partial void OnNotesChanged(string value) => RefreshNotePreview(value);

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("id", out var raw) || !Guid.TryParse(raw?.ToString(), out var id))
        {
            _existing = null; IsExisting = false; IsReauthenticationRequired = false; return;
        }
        await LoadAsync(id);
    }

    [RelayCommand]
    private async Task ReauthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(ReauthenticationPassphrase)) { ErrorMessage = "Enter the current master passphrase."; return; }
        IsBusy = true;
        try
        {
            if (!await _vault.ReauthenticateAsync(ReauthenticationPassphrase))
            {
                ErrorMessage = "Master-passphrase confirmation failed. Recovery keys do not satisfy per-item re-authentication.";
                return;
            }
            ReauthenticationPassphrase = string.Empty;
            IsReauthenticationRequired = false;
            if (_existing is not null) Populate(_existing);
            ErrorMessage = string.Empty;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand] private void ToggleSecret() => IsSecretVisible = !IsSecretVisible;

    [RelayCommand]
    private void ToggleNotePreview() => ShowNotePreview = !ShowNotePreview;

    [RelayCommand]
    private void AddChecklistItem()
    {
        if (string.IsNullOrWhiteSpace(ChecklistDraft)) return;
        try
        {
            Notes = _noteMarkup.AppendChecklistItem(Notes, ChecklistDraft);
            ChecklistDraft = string.Empty;
            ErrorMessage = string.Empty;
        }
        catch (ArgumentException ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    private async Task CopySecretAsync()
    {
        if (IsReauthenticationRequired || string.IsNullOrEmpty(Secret)) return;
        var preferences = await _settings.LoadAsync();
        await _clipboard.CopySecretAsync(Secret, TimeSpan.FromSeconds(preferences.ClipboardClearSeconds));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsReauthenticationRequired) { ErrorMessage = "Re-authenticate before changing this protected item."; return; }
        if (!_vault.IsUnlocked) { await Shell.Current.GoToAsync("//unlock"); return; }
        IsBusy = true; ErrorMessage = string.Empty;
        try
        {
            _ = _noteMarkup.Parse(Notes);
            var customFields = ParseCustomFields(CustomFieldsText);
            var now = DateTimeOffset.UtcNow;
            DateTimeOffset? reviewUtc = HasReviewDate ? new DateTimeOffset(ReviewDate.Date, TimeZoneInfo.Local.GetUtcOffset(ReviewDate.Date)).ToUniversalTime() : null;
            var item = new VaultItem
            {
                Id = _existing?.Id ?? Guid.NewGuid(), Type = SelectedType, Title = Title, Username = Username, Secret = Secret, Url = Url, Notes = Notes,
                Collection = Collection, Tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), IsFavorite = IsFavorite,
                CustomFields = customFields, Attachments = Attachments.ToArray(), CreatedUtc = _existing?.CreatedUtc ?? now, ModifiedUtc = now, ReviewAfterUtc = reviewUtc,
                DeletedUtc = _existing?.DeletedUtc, RequiresReauthentication = RequiresReauthentication, LastAccessedUtc = _existing?.LastAccessedUtc
            };
            await _vault.SaveItemAsync(item);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAttachmentAsync()
    {
        if (IsReauthenticationRequired) return;
        if (_existing is null) { ErrorMessage = "Save this item first, then reopen it to add encrypted attachments."; return; }
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select a file to encrypt into this vault item" }); if (result is null) return;
        IsBusy = true;
        try
        {
            await using var stream = await result.OpenReadAsync();
            var mediaType = AttachmentTypePolicy.ResolveMediaType(result.ContentType, result.FileName);
            var attachment = await _vault.AddAttachmentAsync(_existing.Id, stream, result.FileName, mediaType);
            Attachments.Add(attachment); _existing = await _vault.GetItemAsync(_existing.Id); ErrorMessage = string.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or ArgumentException) { ErrorMessage = $"Attachment was not added: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ExportAttachmentAsync(AttachmentReference attachment)
    {
        if (IsReauthenticationRequired || _existing is null || attachment is null) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Export decrypted attachment?", "CipherNest must create a temporary plaintext copy so the operating-system share sheet can export this file. Other apps, cloud providers, backups, or the receiving destination may retain it. Continue only if you trust the destination.", "Export plaintext", "Cancel");
        if (!confirm) return;

        var exportRoot = Path.Combine(FileSystem.Current.CacheDirectory, "attachment-exports");
        Directory.CreateDirectory(exportRoot);
        var safeName = Path.GetFileName(attachment.DisplayName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = $"attachment-{attachment.Id:N}";
        var path = Path.Combine(exportRoot, $"{attachment.Id:N}-{safeName}");
        IsBusy = true;
        try
        {
            await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
                await _vault.ExportAttachmentAsync(_existing.Id, attachment.Id, stream);
            await Share.Default.RequestAsync(new ShareFileRequest("Export decrypted CipherNest attachment", new ShareFile(path, attachment.MediaType)));
            ErrorMessage = "The temporary plaintext export was deleted after the share request returned. CipherNest cannot delete copies retained by the operating system or destination app.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            ErrorMessage = $"Attachment export failed: {ex.Message}";
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { ErrorMessage = "Export finished, but CipherNest could not confirm deletion of the temporary plaintext file. Clear the app cache before continuing with sensitive work."; }
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveAttachmentAsync(AttachmentReference attachment)
    {
        if (IsReauthenticationRequired || _existing is null || attachment is null) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Remove attachment?", "The encrypted attachment file will be removed from this item. Filesystem remnants may be outside CipherNest's control.", "Remove", "Cancel"); if (!confirm) return;
        await _vault.RemoveAttachmentAsync(_existing.Id, attachment.Id); Attachments.Remove(attachment); _existing = await _vault.GetItemAsync(_existing.Id);
    }

    [RelayCommand]
    private async Task MoveToTrashAsync()
    {
        if (IsReauthenticationRequired || _existing is null) return;
        var confirm = await Shell.Current.DisplayAlertAsync("Move to trash?", "The item can be restored until it is permanently deleted or expires from trash retention.", "Move", "Cancel"); if (!confirm) return;
        await _vault.MoveToTrashAsync(_existing.Id); await Shell.Current.GoToAsync("..");
    }

    private async Task LoadAsync(Guid id)
    {
        try
        {
            _existing = await _vault.GetItemAsync(id);
            if (_existing is null) { ErrorMessage = "This item no longer exists."; return; }
            await _vault.MarkAccessedAsync(id);
            _existing = _existing with { LastAccessedUtc = DateTimeOffset.UtcNow };
            IsExisting = true;
            RequiresReauthentication = _existing.RequiresReauthentication;
            if (_existing.RequiresReauthentication)
            {
                IsReauthenticationRequired = true;
                Title = "Protected item";
                return;
            }
            Populate(_existing);
        }
        catch (Exception ex) { ErrorMessage = $"Could not open this item: {ex.Message}"; }
    }

    private void Populate(VaultItem item)
    {
        SelectedType = item.Type; Title = item.Title; Username = item.Username; Secret = item.Secret; Url = item.Url; Notes = item.Notes; Collection = item.Collection;
        Tags = string.Join(", ", item.Tags); IsFavorite = item.IsFavorite; RequiresReauthentication = item.RequiresReauthentication;
        CustomFieldsText = string.Join(Environment.NewLine, item.CustomFields.Select(static field => $"{(field.IsSecret ? "[secret]" : string.Empty)}{field.Name}={field.Value}"));
        HasReviewDate = item.ReviewAfterUtc is not null; ReviewDate = item.ReviewAfterUtc?.ToLocalTime().Date ?? DateTime.Today.AddMonths(6);
        Attachments.Clear(); foreach (var attachment in item.Attachments) Attachments.Add(attachment);
    }

    private void RefreshNotePreview(string value)
    {
        try
        {
            NotePreview = _noteMarkup.Parse(value).ToAccessibleText();
            if (ErrorMessage.StartsWith("Secure note exceeds", StringComparison.Ordinal)) ErrorMessage = string.Empty;
        }
        catch (ArgumentException ex)
        {
            NotePreview = string.Empty;
            ErrorMessage = ex.Message;
        }
    }

    private static IReadOnlyList<CustomField> ParseCustomFields(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Array.Empty<CustomField>();
        var fields = new List<CustomField>();
        foreach (var raw in input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim(); var isSecret = line.StartsWith("[secret]", StringComparison.OrdinalIgnoreCase); if (isSecret) line = line[8..];
            var separator = line.IndexOf('='); if (separator <= 0) throw new FormatException("Each custom field must use name=value. Prefix a secret field with [secret].");
            fields.Add(new CustomField(line[..separator].Trim(), line[(separator + 1)..], isSecret));
        }
        return fields;
    }
}
