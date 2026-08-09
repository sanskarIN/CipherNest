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
    private readonly ISettingsStore _settings;
    private CancellationTokenSource? _searchCts;
    private IReadOnlyList<VaultItem> _lastResults = Array.Empty<VaultItem>();

    public ObservableCollection<VaultItem> Items { get; } = [];
    public IReadOnlyList<string> SortModes { get; } = ["Favorites & title", "Recently used", "Recently modified", "Title"];
    public IReadOnlyList<string> FilterModes { get; } = ["All", "Favorites", "Review due", .. Enum.GetNames<VaultItemType>()];
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedSortMode = "Favorites & title";
    [ObservableProperty] private string selectedFilterMode = "All";
    [ObservableProperty] private string collectionFilter = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string backupReminderMessage = string.Empty;
    [ObservableProperty] private string reviewReminderMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    public VaultViewModel(IVaultService vault, ISettingsStore settings)
    {
        _vault = vault;
        _settings = settings;
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        _ = SearchDelayedAsync(value, _searchCts.Token);
    }

    partial void OnSelectedSortModeChanged(string value) => ReplaceItems(_lastResults);
    partial void OnSelectedFilterModeChanged(string value) => ReplaceItems(_lastResults);
    partial void OnCollectionFilterChanged(string value) => ReplaceItems(_lastResults);

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!_vault.IsUnlocked) { await Shell.Current.GoToAsync("//unlock"); return; }
        IsBusy = true; ErrorMessage = string.Empty;
        try
        {
            var allItems = await _vault.GetItemsAsync();
            ReplaceItems(string.IsNullOrWhiteSpace(SearchText) ? allItems : await _vault.SearchAsync(SearchText));
            var preferences = await _settings.LoadAsync();
            var backupDue = preferences.LastSuccessfulBackupUtc is null || DateTimeOffset.UtcNow - preferences.LastSuccessfulBackupUtc.Value >= TimeSpan.FromDays(Math.Clamp(preferences.BackupReminderDays, 1, 365));
            BackupReminderMessage = backupDue ? "Encrypted backup reminder: create and test a backup from Settings." : string.Empty;

            if (preferences.ReviewRemindersEnabled)
            {
                var cutoff = DateTimeOffset.UtcNow.AddDays(Math.Clamp(preferences.ReviewReminderLeadDays, 0, 365));
                var dueCount = allItems.Count(item => item.ReviewAfterUtc is { } due && due <= cutoff);
                ReviewReminderMessage = dueCount == 0 ? string.Empty : $"Review reminder: {dueCount} item(s) are due within {preferences.ReviewReminderLeadDays} day(s). Use the Review due filter to inspect overdue items.";
            }
            else
            {
                ReviewReminderMessage = string.Empty;
            }
        }
        catch (Exception ex) { ErrorMessage = $"Could not load the vault: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand] private async Task AddAsync() => await Shell.Current.GoToAsync(nameof(ItemEditorPage));

    [RelayCommand]
    private async Task EditAsync(VaultItem item)
    {
        if (item is null) return;
        try { await _vault.MarkAccessedAsync(item.Id); }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException) { ErrorMessage = $"Could not update recent-use information: {ex.Message}"; }
        await Shell.Current.GoToAsync($"{nameof(ItemEditorPage)}?id={item.Id:D}");
    }

    [RelayCommand] private async Task LockAsync() { await _vault.LockAsync(); Items.Clear(); _lastResults = Array.Empty<VaultItem>(); await Shell.Current.GoToAsync("//unlock"); }
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
        _lastResults = items;
        IEnumerable<VaultItem> filtered = items;

        if (!string.IsNullOrWhiteSpace(CollectionFilter))
            filtered = filtered.Where(item => item.Collection.Contains(CollectionFilter.Trim(), StringComparison.CurrentCultureIgnoreCase));

        filtered = SelectedFilterMode switch
        {
            "Favorites" => filtered.Where(static item => item.IsFavorite),
            "Review due" => filtered.Where(static item => item.ReviewAfterUtc is { } due && due <= DateTimeOffset.UtcNow),
            "All" => filtered,
            _ when Enum.TryParse<VaultItemType>(SelectedFilterMode, true, out var type) => filtered.Where(item => item.Type == type),
            _ => filtered
        };

        var sorted = SelectedSortMode switch
        {
            "Recently used" => filtered.OrderByDescending(static x => x.LastAccessedUtc ?? DateTimeOffset.MinValue).ThenBy(static x => x.Title, StringComparer.CurrentCultureIgnoreCase),
            "Recently modified" => filtered.OrderByDescending(static x => x.ModifiedUtc).ThenBy(static x => x.Title, StringComparer.CurrentCultureIgnoreCase),
            "Title" => filtered.OrderBy(static x => x.Title, StringComparer.CurrentCultureIgnoreCase),
            _ => filtered.OrderByDescending(static x => x.IsFavorite).ThenBy(static x => x.Title, StringComparer.CurrentCultureIgnoreCase)
        };
        Items.Clear();
        foreach (var item in sorted) Items.Add(item);
        IsEmpty = Items.Count == 0;
    }
}
