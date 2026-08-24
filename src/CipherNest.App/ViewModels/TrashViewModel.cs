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
    private readonly IPrivacySafeExceptionReporter _exceptions;
    public ObservableCollection<VaultItem> Items { get; } = [];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DeletionPassphrase { get; set; } = string.Empty;

    public TrashViewModel(IVaultService vault, ISettingsStore settings, IPrivacySafeExceptionReporter exceptions)
    {
        _vault = vault;
        _settings = settings;
        _exceptions = exceptions;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (!_vault.IsUnlocked)
            {
                ClearSensitiveState();
                await Shell.Current.GoToAsync("//unlock");
                return;
            }

            await LoadCoreAsync();
        }
        catch (Exception ex)
        {
            _exceptions.Report("Trash.Load", ex);
            Items.Clear();
            StatusMessage = TrashText("TrashLoadFailureStatus");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreAsync(VaultItem item)
    {
        if (item is null || IsBusy) return;
        IsBusy = true;
        try
        {
            await _vault.RestoreFromTrashAsync(item.Id);
            await LoadCoreAsync();
        }
        catch (Exception ex)
        {
            _exceptions.Report("Trash.Restore", ex);
            Items.Clear();
            StatusMessage = TrashText("TrashRestoreFailureStatus");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(VaultItem item)
    {
        if (item is null || IsBusy) return;
        IsBusy = true;
        try
        {
            if (!await ConfirmMasterPassphraseAsync()) return;

            bool confirm;
            try
            {
                confirm = await Shell.Current.DisplayAlertAsync(
                    TrashText("TrashDeleteConfirmTitle"),
                    TrashText("TrashDeleteConfirmBody"),
                    TrashText("TrashDeleteConfirmAccept"),
                    TrashText("CancelButton"));
            }
            catch (Exception ex)
            {
                _exceptions.Report("Trash.Delete.Confirm", ex);
                StatusMessage = TrashText("TrashDeleteConfirmFailureStatus");
                return;
            }

            if (!confirm) return;

            try
            {
                await _vault.DeletePermanentlyAsync(item.Id);
                await LoadCoreAsync();
            }
            catch (Exception ex)
            {
                _exceptions.Report("Trash.Delete", ex);
                Items.Clear();
                StatusMessage = TrashText("TrashDeleteFailureStatus");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EmptyTrashAsync()
    {
        if (IsBusy) return;
        if (Items.Count == 0)
        {
            StatusMessage = TrashText("TrashAlreadyEmptyStatus");
            return;
        }

        IsBusy = true;
        try
        {
            if (!await ConfirmMasterPassphraseAsync()) return;

            bool confirm;
            try
            {
                confirm = await Shell.Current.DisplayAlertAsync(
                    TrashText("TrashEmptyConfirmTitle"),
                    TrashFormat("TrashEmptyConfirmBodyFormat", Items.Count),
                    TrashText("TrashEmptyConfirmAccept"),
                    TrashText("CancelButton"));
            }
            catch (Exception ex)
            {
                _exceptions.Report("Trash.Empty.Confirm", ex);
                StatusMessage = TrashText("TrashEmptyConfirmFailureStatus");
                return;
            }

            if (!confirm) return;

            try
            {
                foreach (var id in Items.Select(static item => item.Id).ToArray())
                {
                    await _vault.DeletePermanentlyAsync(id);
                }

                Items.Clear();
                StatusMessage = TrashText("TrashEmptiedStatus");
            }
            catch (Exception ex)
            {
                _exceptions.Report("Trash.Empty", ex);
                // A multi-item destructive operation can fail after an earlier item was already deleted.
                // Clear the presentation rather than showing a stale list that implies those records still exist.
                Items.Clear();
                StatusMessage = TrashText("TrashEmptyFailureStatus");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ClearSensitiveState()
    {
        DeletionPassphrase = string.Empty;
        Items.Clear();
        StatusMessage = string.Empty;
    }

    [RelayCommand] private async Task BackAsync() => await Shell.Current.GoToAsync("//vault");

    private async Task LoadCoreAsync()
    {
        var preferences = await _settings.LoadAsync();
        var all = await _vault.GetItemsAsync(includeTrash: true);
        var expiredIds = TrashRetentionPolicy.FindExpiredItemIds(all, DateTimeOffset.UtcNow, preferences.TrashRetentionDays);
        foreach (var id in expiredIds)
        {
            await _vault.DeletePermanentlyAsync(id);
        }

        var trash = (await _vault.GetItemsAsync(includeTrash: true))
            .Where(static item => item.DeletedUtc is not null)
            .OrderByDescending(static item => item.DeletedUtc)
            .ToArray();

        Items.Clear();
        foreach (var item in trash) Items.Add(item);
        StatusMessage = trash.Length == 0
            ? TrashText("TrashEmptyStatus")
            : TrashFormat("TrashStatusFormat", trash.Length, preferences.TrashRetentionDays);
    }

    private async Task<bool> ConfirmMasterPassphraseAsync()
    {
        if (string.IsNullOrWhiteSpace(DeletionPassphrase))
        {
            StatusMessage = TrashText("TrashMasterRequiredStatus");
            return false;
        }

        var passphrase = DeletionPassphrase;
        DeletionPassphrase = string.Empty;
        try
        {
            var authenticated = await _vault.ReauthenticateAsync(passphrase);
            if (!authenticated)
            {
                StatusMessage = TrashText("TrashMasterConfirmationFailedStatus");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _exceptions.Report("Trash.Reauthenticate", ex);
            StatusMessage = TrashText("TrashMasterConfirmationErrorStatus");
            return false;
        }
        finally
        {
            passphrase = string.Empty;
        }
    }

    private static string TrashText(string key) =>
        ServiceProviderHelper.GetRequiredService<ILocalizationService>().Get(key);

    private static string TrashFormat(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, TrashText(key), args);
}
