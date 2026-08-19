using System.Globalization;
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
            ErrorMessage = UnlockText("UnlockMasterSessionRequired");
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
                ErrorMessage = UnlockFormat("UnlockRateLimitFormat", Math.Ceiling(remaining.TotalSeconds));
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
                ErrorMessage = UnlockText("UnlockAuthenticationError");
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
                ErrorMessage = UnlockText("UnlockPeriodicMasterDue");
                return;
            }
            if (!await _biometrics.AuthenticateAsync(UnlockText("UnlockBiometricPrompt")))
            {
                ErrorMessage = UnlockText("UnlockBiometricFailed");
                return;
            }

            var secret = await _biometrics.ReadSecondarySecretAsync();
            if (string.IsNullOrWhiteSpace(secret))
            {
                BiometricUnlockAvailable = false;
                ErrorMessage = UnlockText("UnlockBiometricSecretUnavailable");
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
                ErrorMessage = UnlockText("UnlockBiometricDataMismatch");
            }
            finally { secret = string.Empty; }
        }
        finally { IsBusy = false; }
    }

    private static string UnlockText(string key) =>
        ServiceProviderHelper.GetRequiredService<ILocalizationService>().Get(key);

    private static string UnlockFormat(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, UnlockText(key), args);
}
