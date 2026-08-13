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
    private readonly IPrivacySafeExceptionReporter _exceptions;
    private VaultItem? _existing;

    public IReadOnlyList<VaultItemType> Types { get; } = Enum.GetValues<VaultItemType>();
    public ObservableCollection<AttachmentReference> Attachments { get; } = [];

    [ObservableProperty]
    public partial VaultItemType SelectedType { get; set; } = VaultItemType.Login;

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Secret { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Url { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NotePreview { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ChecklistDraft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowNotePreview { get; set; }

    [ObservableProperty]
    public partial string Collection { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Tags { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CustomFieldsText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    [ObservableProperty]
    public partial bool IsSecretVisible { get; set; }

    [ObservableProperty]
    public partial bool HasReviewDate { get; set; }

    [ObservableProperty]
    public partial DateTime ReviewDate { get; set; } = DateTime.Today.AddMonths(6);

    [ObservableProperty]
    public partial bool RequiresReauthentication { get; set; }

    [ObservableProperty]
    public partial bool IsReauthenticationRequired { get; set; }

    [ObservableProperty]
    public partial string ReauthenticationPassphrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsExisting { get; set; }

    public ItemEditorViewModel(IVaultService vault, IClipboardSecurityService clipboard, ISettingsStore settings, ISafeNoteMarkupService noteMarkup, IPrivacySafeExceptionReporter exceptions)
    {
        _vault = vault;
        _clipboard = clipboard;
        _settings = settings;
        _noteMarkup = noteMarkup;
        _exceptions = exceptions;
    }

    partial void OnNotesChanged(string value) => RefreshNotePreview(value);

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("id", out var raw) || !Guid.TryParse(raw?.ToString(), out var id))
        {
            _existing = null;
            IsExisting = false;
            IsReauthenticationRequired = false;
            return;
        }
        await LoadAsync(id);
    }

    [RelayCommand]
    private async Task ReauthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(ReauthenticationPassphrase))
        {
            ErrorMessage = "Enter the current master passphrase.";
            return;
        }
        IsBusy = true;
        var passphrase = ReauthenticationPassphrase;
        ReauthenticationPassphrase = string.Empty;
        try
        {
            var authenticated = await _vault.ReauthenticateAsync(passphrase);
            if (!authenticated)
            {
                ErrorMessage = "Master-passphrase confirmation failed. Recovery keys do not satisfy per-item re-authentication.";
                return;
            }
            IsReauthenticationRequired = false;
            if (_existing is not null) Populate(_existing);
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.Reauthenticate", ex);
            ErrorMessage = "Master-passphrase confirmation could not be completed safely. Return to the vault and try again.";
        }
        finally
        {
            passphrase = string.Empty;
            IsBusy = false;
        }
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
        catch (ArgumentException)
        {
            ErrorMessage = "Checklist item could not be added. Keep the secure note within the supported size and line limits.";
        }
    }

    [RelayCommand]
    private async Task CopySecretAsync()
    {
        if (IsReauthenticationRequired || string.IsNullOrEmpty(Secret)) return;
        try
        {
            var preferences = await _settings.LoadAsync();
            await _clipboard.CopySecretAsync(Secret, TimeSpan.FromSeconds(preferences.ClipboardClearSeconds));
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.CopySecret", ex);
            ErrorMessage = "The secret could not be copied safely. The vault item remains unchanged.";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsReauthenticationRequired)
        {
            ErrorMessage = "Re-authenticate before changing this protected item.";
            return;
        }
        if (!_vault.IsUnlocked)
        {
            await Shell.Current.GoToAsync("//unlock");
            return;
        }
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            _ = _noteMarkup.Parse(Notes);
            var customFields = ParseCustomFields(CustomFieldsText);
            var now = DateTimeOffset.UtcNow;
            DateTimeOffset? reviewUtc = HasReviewDate ? new DateTimeOffset(ReviewDate.Date, TimeZoneInfo.Local.GetUtcOffset(ReviewDate.Date)).ToUniversalTime() : null;
            var item = new VaultItem
            {
                Id = _existing?.Id ?? Guid.NewGuid(),
                Type = SelectedType,
                Title = Title,
                Username = Username,
                Secret = Secret,
                Url = Url,
                Notes = Notes,
                Collection = Collection,
                Tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                IsFavorite = IsFavorite,
                CustomFields = customFields,
                Attachments = Attachments.ToArray(),
                CreatedUtc = _existing?.CreatedUtc ?? now,
                ModifiedUtc = now,
                ReviewAfterUtc = reviewUtc,
                DeletedUtc = _existing?.DeletedUtc,
                RequiresReauthentication = RequiresReauthentication,
                LastAccessedUtc = _existing?.LastAccessedUtc
            };
            await _vault.SaveItemAsync(item);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
        {
            ErrorMessage = "The item contains invalid or unsupported data. Review field lengths, secure-note limits, dates, and custom fields.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddAttachmentAsync()
    {
        if (IsReauthenticationRequired) return;
        if (_existing is null)
        {
            ErrorMessage = "Save this item first, then reopen it to add encrypted attachments.";
            return;
        }
        IsBusy = true;
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select a file to encrypt into this vault item" });
            if (result is null) return;
            await using var stream = await result.OpenReadAsync();
            var mediaType = AttachmentTypePolicy.ResolveMediaType(result.ContentType, result.FileName);
            var attachment = await _vault.AddAttachmentAsync(_existing.Id, stream, result.FileName, mediaType);
            Attachments.Add(attachment);
            _existing = await _vault.GetItemAsync(_existing.Id);
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.AddAttachment", ex);
            ErrorMessage = "Attachment was not selected or added safely. Check file access, size, and supported metadata, then try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAttachmentAsync(AttachmentReference attachment)
    {
        if (IsReauthenticationRequired || _existing is null || attachment is null) return;
        IsBusy = true;
        string? path = null;
        try
        {
            var confirm = await Shell.Current.DisplayAlertAsync("Export decrypted attachment?", "CipherNest must create a temporary plaintext copy so the operating-system share sheet can export this file. Other apps, cloud providers, backups, or the receiving destination may retain it. Continue only if you trust the destination.", "Export plaintext", "Cancel");
            if (!confirm) return;

            var exportRoot = Path.Combine(FileSystem.Current.CacheDirectory, "attachment-exports");
            Directory.CreateDirectory(exportRoot);
            var safeName = Path.GetFileName(attachment.DisplayName);
            if (string.IsNullOrWhiteSpace(safeName)) safeName = $"attachment-{attachment.Id:N}";
            path = Path.Combine(exportRoot, $"{attachment.Id:N}-{Guid.NewGuid():N}-{safeName}");
            await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await _vault.ExportAttachmentAsync(_existing.Id, attachment.Id, stream);
            }
            await Share.Default.RequestAsync(new ShareFileRequest("Export decrypted CipherNest attachment", new ShareFile(path, attachment.MediaType)));
            ErrorMessage = "The temporary plaintext export was deleted after the share request returned. CipherNest cannot delete copies retained by the operating system or destination app.";
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.ExportAttachment", ex);
            ErrorMessage = "Attachment export or sharing failed safely. The encrypted source attachment remains unchanged.";
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException)
                {
                    _exceptions.Report("ItemEditor.ExportAttachment.TempCleanup", cleanupException);
                    ErrorMessage = "CipherNest could not confirm deletion of the temporary plaintext attachment. Clear the app cache before continuing with sensitive work.";
                }
            }
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RemoveAttachmentAsync(AttachmentReference attachment)
    {
        if (IsReauthenticationRequired || _existing is null || attachment is null) return;
        IsBusy = true;
        try
        {
            var confirm = await Shell.Current.DisplayAlertAsync("Remove attachment?", "The encrypted attachment file will be removed from this item. Filesystem remnants may be outside CipherNest's control.", "Remove", "Cancel");
            if (!confirm) return;
            await _vault.RemoveAttachmentAsync(_existing.Id, attachment.Id);
            Attachments.Remove(attachment);
            _existing = await _vault.GetItemAsync(_existing.Id);
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.RemoveAttachment", ex);
            ErrorMessage = "The encrypted attachment could not be removed safely. Refresh the item before retrying.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task MoveToTrashAsync()
    {
        if (IsReauthenticationRequired || _existing is null) return;
        IsBusy = true;
        try
        {
            var confirm = await Shell.Current.DisplayAlertAsync("Move to trash?", "The item can be restored until it is permanently deleted or expires from trash retention.", "Move", "Cancel");
            if (!confirm) return;
            await _vault.MoveToTrashAsync(_existing.Id);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.MoveToTrash", ex);
            ErrorMessage = "The item could not be moved to trash safely. Refresh the vault before retrying.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync(Guid id)
    {
        try
        {
            _existing = await _vault.GetItemAsync(id);
            if (_existing is null)
            {
                ErrorMessage = "This item no longer exists.";
                return;
            }
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
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.Load", ex);
            ErrorMessage = "Could not open this item safely. The vault remains protected; return to the vault and try again.";
        }
    }

    private void Populate(VaultItem item)
    {
        SelectedType = item.Type;
        Title = item.Title;
        Username = item.Username;
        Secret = item.Secret;
        Url = item.Url;
        Notes = item.Notes;
        Collection = item.Collection;
        Tags = string.Join(", ", item.Tags);
        IsFavorite = item.IsFavorite;
        RequiresReauthentication = item.RequiresReauthentication;
        CustomFieldsText = string.Join(Environment.NewLine, item.CustomFields.Select(static field => $"{(field.IsSecret ? "[secret]" : string.Empty)}{field.Name}={field.Value}"));
        HasReviewDate = item.ReviewAfterUtc is not null;
        ReviewDate = item.ReviewAfterUtc?.ToLocalTime().Date ?? DateTime.Today.AddMonths(6);
        Attachments.Clear();
        foreach (var attachment in item.Attachments) Attachments.Add(attachment);
    }

    private void RefreshNotePreview(string value)
    {
        try
        {
            NotePreview = _noteMarkup.Parse(value).ToAccessibleText();
            if (ErrorMessage.StartsWith("Secure note exceeds", StringComparison.Ordinal)) ErrorMessage = string.Empty;
        }
        catch (ArgumentException)
        {
            NotePreview = string.Empty;
            ErrorMessage = "Secure note exceeds the supported preview or storage limits.";
        }
    }

    private static IReadOnlyList<CustomField> ParseCustomFields(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Array.Empty<CustomField>();
        var fields = new List<CustomField>();
        foreach (var raw in input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            var isSecret = line.StartsWith("[secret]", StringComparison.OrdinalIgnoreCase);
            if (isSecret) line = line[8..];
            var separator = line.IndexOf('=');
            if (separator <= 0) throw new FormatException("Each custom field must use name=value. Prefix a secret field with [secret].");
            fields.Add(new CustomField(line[..separator].Trim(), line[(separator + 1)..], isSecret));
        }
        return fields;
    }
}
