using System.Collections.ObjectModel;
using CipherNest.Application.Abstractions;
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
            var cutoff = DateTimeOffset.UtcNow.AddDays(-Math.Clamp(preferences.TrashRetentionDays, 1, 365));
            var all = await _vault.GetItemsAsync(includeTrash: true);
            foreach (var expired in all.Where(item => item.DeletedUtc is { } deleted && deleted <= cutoff))
            {
                await _vault.DeletePermanentlyAsync(expired.Id);
            }
            var trash = (await _vault.GetItemsAsync(includeTrash: true)).Where(static item => item.DeletedUtc is not null).OrderByDescending(static item => item.DeletedUtc).ToArray();
            Items.Clear();
            foreach (var item in trash) Items.Add(item);
            StatusMessage = trash.Length == 0 ? "Trash is empty." : $"{trash.Length} item(s) in trash. Items older than {preferences.TrashRetentionDays} days are removed when trash opens.";
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
        var confirm = await Shell.Current.DisplayAlert("Delete permanently?", "CipherNest will remove the encrypted record and attachment files. Filesystem or flash-storage remnants may still be recoverable by the operating system or forensic tools.", "Delete", "Cancel");
        if (!confirm) return;
        await _vault.DeletePermanentlyAsync(item.Id);
        await LoadAsync();
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");
}
