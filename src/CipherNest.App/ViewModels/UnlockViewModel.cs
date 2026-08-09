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
    private readonly IBiometricUnlockService _biometrics;
    private readonly ISettingsStore _settings;

    [ObservableProperty] private string masterPassphrase = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool biometricUnlockAvailable;

    public UnlockViewModel(IVaultService vault, UnlockRateLimiter limiter, IBiometricUnlockService biometrics, ISettingsStore settings)
    {
        _vault = vault;
        _limiter = limiter;
        _biometrics = biometrics;
        _settings = settings;
    }

    public async Task LoadAsync()
    {
        var preferences = await _settings.LoadAsync();
        BiometricUnlockAvailable = preferences.BiometricUnlockEnabled
            && _biometrics.IsSupported
            && await _biometrics.IsAvailableAsync()
            && await _vault.IsSecondaryUnlockConfiguredAsync();
    }

    partial void OnMasterPassphraseChanged(string value) => UnlockCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool value)
    {
        UnlockCommand.NotifyCanExecuteChanged();
        BiometricUnlockCommand.NotifyCanExecuteChanged();
    }

    private bool CanUnlock() => !IsBusy && MasterPassphrase.Length >= 1;
    private bool CanBiometricUnlock() => !IsBusy && BiometricUnlockAvailable;

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
        finally { IsBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanBiometricUnlock))]
    private async Task BiometricUnlockAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            if (!await _biometrics.AuthenticateAsync("Authenticate to unlock your local CipherNest vault."))
            {
                ErrorMessage = "Biometric authentication was cancelled or failed. Use the master passphrase instead.";
                return;
            }

            var secret = await _biometrics.ReadSecondarySecretAsync();
            if (string.IsNullOrWhiteSpace(secret))
            {
                BiometricUnlockAvailable = false;
                ErrorMessage = "The protected biometric unlock secret is unavailable. Unlock with the master passphrase and re-enable biometrics in Settings.";
                return;
            }

            try
            {
                await _vault.UnlockWithSecondarySecretAsync(secret);
                _limiter.RegisterSuccess();
                await Shell.Current.GoToAsync("//vault");
            }
            catch (VaultAuthenticationException)
            {
                await _biometrics.ClearSecondarySecretAsync();
                BiometricUnlockAvailable = false;
                ErrorMessage = "Biometric unlock data no longer matches this vault. Use the master passphrase and configure biometrics again.";
            }
            finally { secret = string.Empty; }
        }
        finally { IsBusy = false; }
    }
}
