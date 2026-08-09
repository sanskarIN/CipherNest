using System.Collections.ObjectModel;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class ItemEditorViewModel
{
    public ObservableCollection<CustomField> SecretCustomFields { get; } = [];

    partial void OnCustomFieldsTextChanged(string value) => RefreshSecretCustomFields(value);

    [RelayCommand]
    private async Task CopyUsernameAsync()
    {
        if (IsReauthenticationRequired || string.IsNullOrEmpty(Username)) return;
        var preferences = await _settings.LoadAsync();
        await _clipboard.CopySecretAsync(Username, TimeSpan.FromSeconds(preferences.ClipboardClearSeconds));
        ErrorMessage = "Username copied. CipherNest will attempt to clear it after the configured clipboard interval where the platform allows reliable clearing.";
    }

    [RelayCommand]
    private async Task CopyCustomSecretAsync(CustomField field)
    {
        if (IsReauthenticationRequired || field is null || !field.IsSecret || string.IsNullOrEmpty(field.Value)) return;
        var preferences = await _settings.LoadAsync();
        await _clipboard.CopySecretAsync(field.Value, TimeSpan.FromSeconds(preferences.ClipboardClearSeconds));
        ErrorMessage = $"Secret custom field '{field.Name}' copied. CipherNest will attempt timed clipboard clearing where supported.";
    }

    public void ClearSensitiveState()
    {
        Title = string.Empty;
        Username = string.Empty;
        Secret = string.Empty;
        Url = string.Empty;
        Notes = string.Empty;
        Collection = string.Empty;
        Tags = string.Empty;
        CustomFieldsText = string.Empty;
        ReauthenticationPassphrase = string.Empty;
        ChecklistDraft = string.Empty;
        NotePreview = string.Empty;
        ShowNotePreview = false;
        Attachments.Clear();
        SecretCustomFields.Clear();
        _existing = null;
    }

    private void RefreshSecretCustomFields(string value)
    {
        SecretCustomFields.Clear();
        if (string.IsNullOrWhiteSpace(value)) return;
        try
        {
            foreach (var field in ParseCustomFields(value).Where(static field => field.IsSecret))
                SecretCustomFields.Add(field);
        }
        catch (FormatException)
        {
            // The editor may temporarily contain an incomplete line while the user types.
            // SaveAsync performs authoritative validation before persistence.
        }
    }
}
