using System.Collections.ObjectModel;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Services;
using CipherNest.App.Services;
using CipherNest.Domain.Models;
using CipherNest.App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class VaultViewModel : ObservableObject
{
    private const int PageSize = 50;
    private readonly IVaultService _vault;
    private readonly ISettingsStore _settings;
    private readonly IClipboardSecurityService _clipboard;
    private readonly IPrivacySafeExceptionReporter _exceptions;
    private CancellationTokenSource? _searchCts;
    private IReadOnlyList<VaultItem> _lastResults = Array.Empty<VaultItem>();
    private IReadOnlyList<VaultItem> _orderedFilteredResults = Array.Empty<VaultItem>();

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
    [ObservableProperty] private bool canLoadMore;
    [ObservableProperty] private string resultCountMessage = string.Empty;

    public VaultViewModel(IVaultService vault, ISettingsStore settings, IClipboardSecurityService clipboard, IPrivacySafeExceptionReporter exceptions)
    {
        _vault = vault;
        _settings = settings;
        _clipboard = clipboard;
        _exceptions = exceptions;
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
            var preferences = await _settings.LoadAsync();
            var allWithTrash = await _vault.GetItemsAsync(includeTrash: true);
            var expiredIds = TrashRetentionPolicy.FindExpiredItemIds(allWithTrash, DateTimeOffset.UtcNow, preferences.TrashRetentionDays);
            foreach (var id in expiredIds) await _vault.DeletePermanentlyAsync(id);

            var allItems = expiredIds.Count == 0
                ? allWithTrash.Where(static item => item.DeletedUtc is null).ToArray()
                : await _vault.GetItemsAsync();

            ReplaceItems(string.IsNullOrWhiteSpace(SearchText) ? allItems : await _vault.SearchAsync(SearchText));
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
        if (item is not null) await Shell.Current.GoToAsync($"{nameof(ItemEditorPage)}?id={item.Id:D}");
    }

    [RelayCommand]
    private void LoadMore()
    {
        if (!CanLoadMore || IsBusy) return;
        AppendNextPage();
    }

    [RelayCommand]
    private async Task LockAsync()
    {
        await _vault.LockAsync();
        try { await _clipboard.ClearAsync(); }
        catch (Exception exception) { _exceptions.Report("Vault.ManualLock.Clipboard", exception); }
        Items.Clear();
        _lastResults = Array.Empty<VaultItem>();
        _orderedFilteredResults = Array.Empty<VaultItem>();
        CanLoadMore = false;
        ResultCountMessage = string.Empty;
        await Shell.Current.GoToAsync("//unlock");
    }

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

        _orderedFilteredResults = SelectedSortMode switch
        {
            "Recently used" => filtered.OrderByDescending(static x => x.LastAccessedUtc ?? DateTimeOffset.MinValue).ThenBy(static x => x.Title, StringComparer.CurrentCultureIgnoreCase).ToArray(),
            "Recently modified" => filtered.OrderByDescending(static x => x.ModifiedUtc).ThenBy(static x => x.Title, StringComparer.CurrentCultureIgnoreCase).ToArray(),
            "Title" => filtered.OrderBy(static x => x.Title, StringComparer.CurrentCultureIgnoreCase).ToArray(),
            _ => filtered.OrderByDescending(static x => x.IsFavorite).ThenBy(static x => x.Title, StringComparer.CurrentCultureIgnoreCase).ToArray()
        };

        Items.Clear();
        AppendNextPage();
        IsEmpty = _orderedFilteredResults.Count == 0;
    }

    private void AppendNextPage()
    {
        foreach (var item in _orderedFilteredResults.Skip(Items.Count).Take(PageSize)) Items.Add(item);
        CanLoadMore = Items.Count < _orderedFilteredResults.Count;
        ResultCountMessage = _orderedFilteredResults.Count == 0
            ? "No matching items."
            : $"Showing {Items.Count:N0} of {_orderedFilteredResults.Count:N0} matching item(s).";
    }
}
