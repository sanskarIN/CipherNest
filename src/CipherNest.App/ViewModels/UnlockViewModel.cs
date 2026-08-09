using CipherNest.Application.Abstractions;
using CipherNest.Application.Exceptions;
using CipherNest.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class UnlockViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private readonly UnlockRateLimiter _limiter;

    [ObservableProperty] private string masterPassphrase = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    public UnlockViewModel(IVaultService vault, UnlockRateLimiter limiter)
    {
        _vault = vault;
        _limiter = limiter;
    }

    partial void OnMasterPassphraseChanged(string value) => UnlockCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool value) => UnlockCommand.NotifyCanExecuteChanged();

    private bool CanUnlock() => !IsBusy && MasterPassphrase.Length >= 1;

    [RelayCommand(CanExecute = nameof(CanUnlock))]
    private async Task UnlockAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var remaining = _limiter.GetRemainingDelay(DateTimeOffset.UtcNow);
            if (remaining > TimeSpan.Zero)
            {
                ErrorMessage = $"Too many failed attempts. Try again in {Math.Ceiling(remaining.TotalSeconds)} seconds.";
                return;
            }

            try
            {
                await _vault.UnlockAsync(MasterPassphrase);
                _limiter.RegisterSuccess();
                MasterPassphrase = string.Empty;
                await Shell.Current.GoToAsync("//vault");
            }
            catch (VaultAuthenticationException)
            {
                _limiter.RegisterFailure(DateTimeOffset.UtcNow);
                ErrorMessage = "The passphrase is incorrect or the vault cannot be authenticated.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
