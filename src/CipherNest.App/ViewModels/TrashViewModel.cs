using System.Collections.ObjectModel;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Services;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class TrashViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private readonly ISettingsStore _settings;
    public ObservableCollection<VaultItem> Items { get; } = [];
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string deletionPassphrase = string.Empty;

    public TrashViewModel(IVaultService vault, ISettingsStore settings)
    {
        _vault = vault;
        _settings = settings;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!_vault.IsUnlocked) { await Shell.Current.GoToAsync("//unlock"); return; }
        IsBusy = true;
        try
        {
            var preferences = await _settings.LoadAsync();
            var all = await _vault.GetItemsAsync(includeTrash: true);
            var expiredIds = TrashRetentionPolicy.FindExpiredItemIds(all, DateTimeOffset.UtcNow, preferences.TrashRetentionDays);
            foreach (var id in expiredIds) await _vault.DeletePermanentlyAsync(id);

            var trash = (await _vault.GetItemsAsync(includeTrash: true)).Where(static item => item.DeletedUtc is not null).OrderByDescending(static item => item.DeletedUtc).ToArray();
            Items.Clear();
            foreach (var item in trash) Items.Add(item);
            StatusMessage = trash.Length == 0 ? "Trash is empty." : $"{trash.Length} item(s) in trash. Items older than {preferences.TrashRetentionDays} days are removed automatically when vault maintenance runs.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RestoreAsync(VaultItem item)
    {
        if (item is null) return;
        await _vault.RestoreFromTrashAsync(item.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(VaultItem item)
    {
        if (item is null) return;
        if (!await ConfirmMasterPassphraseAsync()) return;
        var confirm = await Shell.Current.DisplayAlert("Delete permanently?", "CipherNest will remove the encrypted record and attachment files. Filesystem or flash-storage remnants may still be recoverable by the operating system or forensic tools.", "Delete permanently", "Cancel");
        DeletionPassphrase = string.Empty;
        if (!confirm) return;
        await _vault.DeletePermanentlyAsync(item.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task EmptyTrashAsync()
    {
        if (Items.Count == 0) { StatusMessage = "Trash is already empty."; return; }
        if (!await ConfirmMasterPassphraseAsync()) return;
        var confirm = await Shell.Current.DisplayAlert("Empty trash permanently?", $"Permanently remove all {Items.Count} encrypted trash item(s) and their CipherNest-managed attachment files? Filesystem remnants may remain outside CipherNest's control.", "Empty trash", "Cancel");
        DeletionPassphrase = string.Empty;
        if (!confirm) return;

        IsBusy = true;
        try
        {
            foreach (var id in Items.Select(static item => item.Id).ToArray()) await _vault.DeletePermanentlyAsync(id);
            StatusMessage = "Trash emptied. CipherNest removed its encrypted records and attachment containers; physical storage remnants may remain outside application control.";
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    public void ClearSensitiveState() => DeletionPassphrase = string.Empty;

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");

    private async Task<bool> ConfirmMasterPassphraseAsync()
    {
        if (string.IsNullOrWhiteSpace(DeletionPassphrase))
        {
            StatusMessage = "Enter the current master passphrase before permanent deletion. Recovery keys are not accepted for this destructive confirmation.";
            return false;
        }
        if (!await _vault.ReauthenticateAsync(DeletionPassphrase))
        {
            DeletionPassphrase = string.Empty;
            StatusMessage = "Master-passphrase confirmation failed. Nothing was permanently deleted.";
            return false;
        }
        return true;
    }
}
