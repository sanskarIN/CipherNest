using System.Collections.ObjectModel;
using CipherNest.Application.Abstractions;
using CipherNest.Domain.Models;
using CipherNest.App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class VaultViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private CancellationTokenSource? _searchCts;
    public ObservableCollection<VaultItem> Items { get; } = [];
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    public VaultViewModel(IVaultService vault) => _vault = vault;
    partial void OnSearchTextChanged(string value) { _searchCts?.Cancel(); _searchCts?.Dispose(); _searchCts = new CancellationTokenSource(); _ = SearchDelayedAsync(value, _searchCts.Token); }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!_vault.IsUnlocked) { await Shell.Current.GoToAsync("//unlock"); return; }
        IsBusy = true; ErrorMessage = string.Empty;
        try { ReplaceItems(await _vault.GetItemsAsync()); }
        catch (Exception ex) { ErrorMessage = $"Could not load the vault: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand] private async Task AddAsync() => await Shell.Current.GoToAsync(nameof(ItemEditorPage));
    [RelayCommand] private async Task EditAsync(VaultItem item) { if (item is not null) await Shell.Current.GoToAsync($"{nameof(ItemEditorPage)}?id={item.Id:D}"); }
    [RelayCommand] private async Task LockAsync() { await _vault.LockAsync(); Items.Clear(); await Shell.Current.GoToAsync("//unlock"); }
    [RelayCommand] private async Task GeneratorAsync() => await Shell.Current.GoToAsync("//generator");
    [RelayCommand] private async Task AuditAsync() => await Shell.Current.GoToAsync("//audit");
    [RelayCommand] private async Task TrashAsync() => await Shell.Current.GoToAsync("//trash");
    [RelayCommand] private async Task SettingsAsync() => await Shell.Current.GoToAsync("//settings");
    [RelayCommand] private async Task AboutAsync() => await Shell.Current.GoToAsync("//about");

    private async Task SearchDelayedAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(150, cancellationToken);
            if (!_vault.IsUnlocked) return;
            var results = await _vault.SearchAsync(query, cancellationToken);
            await MainThread.InvokeOnMainThreadAsync(() => ReplaceItems(results));
        }
        catch (OperationCanceledException) { }
    }

    private void ReplaceItems(IReadOnlyList<VaultItem> items)
    {
        Items.Clear();
        foreach (var item in items) Items.Add(item);
        IsEmpty = Items.Count == 0;
    }
}
