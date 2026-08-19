using System.Globalization;
using CipherNest.Application.Abstractions;
using CipherNest.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class OnboardingViewModel : ObservableObject
{
    private const int MinimumMasterPassphraseCharacters = 12;
    private const int MaximumMasterPassphraseCharacters = 4_096;
    private readonly IVaultService _vault;
    private readonly IPasswordGenerator _generator;
    private readonly IPrivacySafeExceptionReporter _exceptions;

    [ObservableProperty]
    public partial string MasterPassphrase { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Confirmation { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StrengthLabel { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool RecoveryLimitAcknowledged { get; set; }

    [ObservableProperty]
    public partial bool RecoveryKeyEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool RecoveryKeySaved { get; set; }

    [ObservableProperty]
    public partial string RecoveryKey { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowRecoveryKey { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    public OnboardingViewModel(IVaultService vault, IPasswordGenerator generator, IPrivacySafeExceptionReporter exceptions)
    {
        _vault = vault;
        _generator = generator;
        _exceptions = exceptions;
        StrengthLabel = OnboardingText("OnboardingStrengthInitial");
    }

    partial void OnMasterPassphraseChanged(string value)
    {
        StrengthLabel = value.Length > MaximumMasterPassphraseCharacters
            ? OnboardingFormat("OnboardingMasterTooLongFormat", MaximumMasterPassphraseCharacters)
            : PasswordStrengthLabel(value);
        CreateVaultCommand.NotifyCanExecuteChanged();
    }

    partial void OnConfirmationChanged(string value) => CreateVaultCommand.NotifyCanExecuteChanged();
    partial void OnRecoveryLimitAcknowledgedChanged(bool value) => CreateVaultCommand.NotifyCanExecuteChanged();
    partial void OnRecoveryKeySavedChanged(bool value) => ContinueCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool value) => CreateVaultCommand.NotifyCanExecuteChanged();

    private bool CanCreate() =>
        !IsBusy &&
        RecoveryLimitAcknowledged &&
        MasterPassphrase.Length is >= MinimumMasterPassphraseCharacters and <= MaximumMasterPassphraseCharacters &&
        _generator.Evaluate(MasterPassphrase).Score >= 3 &&
        string.Equals(MasterPassphrase, Confirmation, StringComparison.Ordinal);

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateVaultAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        var passphrase = MasterPassphrase;
        MasterPassphrase = string.Empty;
        Confirmation = string.Empty;
        try
        {
            var recovery = await _vault.CreateAsync(passphrase, RecoveryKeyEnabled);
            if (!string.IsNullOrEmpty(recovery))
            {
                RecoveryKey = recovery;
                ShowRecoveryKey = true;
            }
            else
            {
                await Shell.Current.GoToAsync("//vault");
            }
        }
        catch (ArgumentException)
        {
            ErrorMessage = OnboardingFormat(
                "OnboardingMasterRequirementsErrorFormat",
                MinimumMasterPassphraseCharacters,
                MaximumMasterPassphraseCharacters);
        }
        catch (InvalidOperationException)
        {
            ErrorMessage = OnboardingText("OnboardingVaultExistsError");
        }
        catch (Exception ex)
        {
            _exceptions.Report("Onboarding.CreateVault", ex);
            ErrorMessage = OnboardingText("OnboardingCreateFailureError");
        }
        finally
        {
            passphrase = string.Empty;
            IsBusy = false;
        }
    }

    private bool CanContinue() => ShowRecoveryKey && RecoveryKeySaved;

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private async Task ContinueAsync()
    {
        RecoveryKey = string.Empty;
        ShowRecoveryKey = false;
        await Shell.Current.GoToAsync("//vault");
    }

    private string PasswordStrengthLabel(string value)
    {
        if (string.IsNullOrEmpty(value)) return OnboardingText("PasswordStrengthEmpty");

        return _generator.Evaluate(value).Score switch
        {
            <= 0 => OnboardingText("PasswordStrengthVeryWeak"),
            1 => OnboardingText("PasswordStrengthWeak"),
            2 => OnboardingText("PasswordStrengthFair"),
            3 => OnboardingText("PasswordStrengthStrong"),
            _ => OnboardingText("PasswordStrengthVeryStrong")
        };
    }

    private static string OnboardingText(string key) =>
        ServiceProviderHelper.GetRequiredService<ILocalizationService>().Get(key);

    private static string OnboardingFormat(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, OnboardingText(key), args);
}
