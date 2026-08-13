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
    private readonly SessionSecurityState _sessionSecurity;

    [ObservableProperty]
    public partial string MasterPassphrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool BiometricUnlockAvailable { get; set; }

    public UnlockViewModel(IVaultService vault, UnlockRateLimiter limiter, IBiometricUnlockService biometrics, ISettingsStore settings, SessionSecurityState sessionSecurity)
    {
        _vault = vault;
        _limiter = limiter;
        _biometrics = biometrics;
        _settings = settings;
        _sessionSecurity = sessionSecurity;
    }

    public async Task LoadAsync()
    {
        var preferences = await _settings.LoadAsync();
        var maximumAge = TimeSpan.FromHours(Math.Clamp(preferences.RequireMasterPassphraseAfterHours, 1, 168));
        var masterRequired = _sessionSecurity.RequiresMasterAuthentication(DateTimeOffset.UtcNow, maximumAge);
        BiometricUnlockAvailable = !masterRequired
            && preferences.BiometricUnlockEnabled
            && _biometrics.IsSupported
            && await _biometrics.IsAvailableAsync()
            && await _vault.IsSecondaryUnlockConfiguredAsync();
        if (masterRequired && preferences.BiometricUnlockEnabled)
            ErrorMessage = "Enter the master passphrase to begin this security session. Biometric unlock becomes available for later locks during the configured period.";
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

            var passphrase = MasterPassphrase;
            MasterPassphrase = string.Empty;
            try
            {
                await _vault.UnlockAsync(passphrase);
                var isMaster = await _vault.ReauthenticateAsync(passphrase);
                if (isMaster) _sessionSecurity.RecordMasterAuthentication(DateTimeOffset.UtcNow);
                _limiter.RegisterSuccess();
                await Shell.Current.GoToAsync("//vault");
            }
            catch (VaultAuthenticationException)
            {
                _limiter.RegisterFailure(DateTimeOffset.UtcNow);
                ErrorMessage = "The passphrase is incorrect or the vault cannot be authenticated.";
            }
            finally
            {
                passphrase = string.Empty;
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
            var preferences = await _settings.LoadAsync();
            var maximumAge = TimeSpan.FromHours(Math.Clamp(preferences.RequireMasterPassphraseAfterHours, 1, 168));
            if (_sessionSecurity.RequiresMasterAuthentication(DateTimeOffset.UtcNow, maximumAge))
            {
                BiometricUnlockAvailable = false;
                ErrorMessage = "The periodic master-passphrase check is due. Unlock with the master passphrase first.";
                return;
            }
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
