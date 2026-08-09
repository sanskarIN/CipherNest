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
    private VaultItem? _existing;

    public IReadOnlyList<VaultItemType> Types { get; } = Enum.GetValues<VaultItemType>();
    public ObservableCollection<AttachmentReference> Attachments { get; } = [];
    [ObservableProperty] private VaultItemType selectedType = VaultItemType.Login;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string secret = string.Empty;
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string collection = string.Empty;
    [ObservableProperty] private string tags = string.Empty;
    [ObservableProperty] private string customFieldsText = string.Empty;
    [ObservableProperty] private bool isFavorite;
    [ObservableProperty] private bool isSecretVisible;
    [ObservableProperty] private bool hasReviewDate;
    [ObservableProperty] private DateTime reviewDate = DateTime.Today.AddMonths(6);
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isExisting;

    public ItemEditorViewModel(IVaultService vault, IClipboardSecurityService clipboard, ISettingsStore settings)
    {
        _vault = vault;
        _clipboard = clipboard;
        _settings = settings;
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("id", out var raw) || !Guid.TryParse(raw?.ToString(), out var id))
        {
            _existing = null; IsExisting = false; return;
        }
        await LoadAsync(id);
    }

    [RelayCommand] private void ToggleSecret() => IsSecretVisible = !IsSecretVisible;

    [RelayCommand]
    private async Task CopySecretAsync()
    {
        if (string.IsNullOrEmpty(Secret)) return;
        var preferences = await _settings.LoadAsync();
        await _clipboard.CopySecretAsync(Secret, TimeSpan.FromSeconds(preferences.ClipboardClearSeconds));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!_vault.IsUnlocked) { await Shell.Current.GoToAsync("//unlock"); return; }
        IsBusy = true; ErrorMessage = string.Empty;
        try
        {
            var customFields = ParseCustomFields(CustomFieldsText);
            var now = DateTimeOffset.UtcNow;
            var reviewUtc = HasReviewDate ? new DateTimeOffset(ReviewDate.Date, TimeZoneInfo.Local.GetUtcOffset(ReviewDate.Date)).ToUniversalTime() : null;
            var item = new VaultItem
            {
                Id = _existing?.Id ?? Guid.NewGuid(), Type = SelectedType, Title = Title, Username = Username, Secret = Secret, Url = Url, Notes = Notes,
                Collection = Collection,
                Tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                IsFavorite = IsFavorite, CustomFields = customFields, Attachments = Attachments.ToArray(), CreatedUtc = _existing?.CreatedUtc ?? now, ModifiedUtc = now,
                ReviewAfterUtc = reviewUtc, DeletedUtc = _existing?.DeletedUtc, RequiresReauthentication = _existing?.RequiresReauthentication ?? false
            };
            await _vault.SaveItemAsync(item);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)
        {
            ErrorMessage = ex.Message;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddAttachmentAsync()
    {
        if (_existing is null)
        {
            ErrorMessage = "Save this item first, then reopen it to add encrypted attachments.";
            return;
        }
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select a file to encrypt into this vault item" });
        if (result is null) return;
        IsBusy = true;
        try
        {
            await using var stream = await result.OpenReadAsync();
            var attachment = await _vault.AddAttachmentAsync(_existing.Id, stream, result.FileName, "application/octet-stream");
            Attachments.Add(attachment);
            _existing = await _vault.GetItemAsync(_existing.Id);
            ErrorMessage = string.Empty;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or ArgumentException)
        {
            ErrorMessage = $"Attachment was not added: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RemoveAttachmentAsync(AttachmentReference attachment)
    {
        if (_existing is null || attachment is null) return;
        var confirm = await Shell.Current.DisplayAlert("Remove attachment?", "The encrypted attachment file will be removed from this item. Filesystem remnants may be outside CipherNest's control.", "Remove", "Cancel");
        if (!confirm) return;
        await _vault.RemoveAttachmentAsync(_existing.Id, attachment.Id);
        Attachments.Remove(attachment);
        _existing = await _vault.GetItemAsync(_existing.Id);
    }

    [RelayCommand]
    private async Task MoveToTrashAsync()
    {
        if (_existing is null) return;
        var confirm = await Shell.Current.DisplayAlert("Move to trash?", "The item can be restored until it is permanently deleted or expires from trash retention.", "Move", "Cancel");
        if (!confirm) return;
        await _vault.MoveToTrashAsync(_existing.Id);
        await Shell.Current.GoToAsync("..");
    }

    private async Task LoadAsync(Guid id)
    {
        try
        {
            _existing = await _vault.GetItemAsync(id);
            if (_existing is null) { ErrorMessage = "This item no longer exists."; return; }
            SelectedType = _existing.Type; Title = _existing.Title; Username = _existing.Username; Secret = _existing.Secret; Url = _existing.Url; Notes = _existing.Notes;
            Collection = _existing.Collection; Tags = string.Join(", ", _existing.Tags); IsFavorite = _existing.IsFavorite;
            CustomFieldsText = string.Join(Environment.NewLine, _existing.CustomFields.Select(static field => $"{(field.IsSecret ? "[secret]" : string.Empty)}{field.Name}={field.Value}"));
            HasReviewDate = _existing.ReviewAfterUtc is not null;
            ReviewDate = _existing.ReviewAfterUtc?.ToLocalTime().Date ?? DateTime.Today.AddMonths(6);
            Attachments.Clear(); foreach (var attachment in _existing.Attachments) Attachments.Add(attachment);
            IsExisting = true;
        }
        catch (Exception ex) { ErrorMessage = $"Could not open this item: {ex.Message}"; }
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
