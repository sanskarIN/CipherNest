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
    [ObservableProperty] private VaultItemType selectedType = VaultItemType.Login;
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string username = string.Empty;
    [ObservableProperty] private string secret = string.Empty;
    [ObservableProperty] private string url = string.Empty;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private string tags = string.Empty;
    [ObservableProperty] private bool isFavorite;
    [ObservableProperty] private bool isSecretVisible;
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
            _existing = null;
            IsExisting = false;
            return;
        }
        await LoadAsync(id);
    }

    [RelayCommand]
    private void ToggleSecret() => IsSecretVisible = !IsSecretVisible;

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
        if (!_vault.IsUnlocked)
        {
            await Shell.Current.GoToAsync("//unlock");
            return;
        }
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var now = DateTimeOffset.UtcNow;
            var item = new VaultItem
            {
                Id = _existing?.Id ?? Guid.NewGuid(),
                Type = SelectedType,
                Title = Title,
                Username = Username,
                Secret = Secret,
                Url = Url,
                Notes = Notes,
                Tags = Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                IsFavorite = IsFavorite,
                CustomFields = _existing?.CustomFields ?? Array.Empty<CustomField>(),
                Attachments = _existing?.Attachments ?? Array.Empty<AttachmentReference>(),
                CreatedUtc = _existing?.CreatedUtc ?? now,
                ModifiedUtc = now,
                ReviewAfterUtc = _existing?.ReviewAfterUtc,
                DeletedUtc = _existing?.DeletedUtc,
                RequiresReauthentication = _existing?.RequiresReauthentication ?? false
            };
            await _vault.SaveItemAsync(item);
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task MoveToTrashAsync()
    {
        if (_existing is null) return;
        var confirm = await Shell.Current.DisplayAlert("Move to trash?", "The item can be restored until it is permanently deleted.", "Move", "Cancel");
        if (!confirm) return;
        await _vault.MoveToTrashAsync(_existing.Id);
        await Shell.Current.GoToAsync("..");
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
            SelectedType = _existing.Type;
            Title = _existing.Title;
            Username = _existing.Username;
            Secret = _existing.Secret;
            Url = _existing.Url;
            Notes = _existing.Notes;
            Tags = string.Join(", ", _existing.Tags);
            IsFavorite = _existing.IsFavorite;
            IsExisting = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Could not open this item: {ex.Message}";
        }
    }
}
