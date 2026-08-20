using System.Collections.ObjectModel;
using System.Globalization;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Services;
using CipherNest.App.Services;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class TrashViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private readonly ISettingsStore _settings;
    public ObservableCollection<VaultItem> Items { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DeletionPassphrase { get; set; } = string.Empty;

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
            StatusMessage = trash.Length == 0
                ? TrashText("TrashEmptyStatus")
                : TrashFormat("TrashStatusFormat", trash.Length, preferences.TrashRetentionDays);
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
        var confirm = await Shell.Current.DisplayAlertAsync(
            TrashText("TrashDeleteConfirmTitle"),
            TrashText("TrashDeleteConfirmBody"),
            TrashText("TrashDeleteConfirmAccept"),
            TrashText("CancelButton"));
        if (!confirm) return;
        await _vault.DeletePermanentlyAsync(item.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task EmptyTrashAsync()
    {
        if (Items.Count == 0) { StatusMessage = TrashText("TrashAlreadyEmptyStatus"); return; }
        if (!await ConfirmMasterPassphraseAsync()) return;
        var confirm = await Shell.Current.DisplayAlertAsync(
            TrashText("TrashEmptyConfirmTitle"),
            TrashFormat("TrashEmptyConfirmBodyFormat", Items.Count),
            TrashText("TrashEmptyConfirmAccept"),
            TrashText("CancelButton"));
        if (!confirm) return;

        IsBusy = true;
        try
        {
            foreach (var id in Items.Select(static item => item.Id).ToArray()) await _vault.DeletePermanentlyAsync(id);
            Items.Clear();
            StatusMessage = TrashText("TrashEmptiedStatus");
        }
        finally { IsBusy = false; }
    }

    public void ClearSensitiveState() => DeletionPassphrase = string.Empty;

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");

    private async Task<bool> ConfirmMasterPassphraseAsync()
    {
        if (string.IsNullOrWhiteSpace(DeletionPassphrase))
        {
            StatusMessage = TrashText("TrashMasterRequiredStatus");
            return false;
        }

        var authenticated = await _vault.ReauthenticateAsync(DeletionPassphrase);
        DeletionPassphrase = string.Empty;
        if (!authenticated)
        {
            StatusMessage = TrashText("TrashMasterConfirmationFailedStatus");
            return false;
        }

        return true;
    }

    private static string TrashText(string key) =>
        ServiceProviderHelper.GetRequiredService<ILocalizationService>().Get(key);

    private static string TrashFormat(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, TrashText(key), args);
}
