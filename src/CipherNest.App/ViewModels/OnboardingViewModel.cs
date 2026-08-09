using CipherNest.Application.Abstractions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class OnboardingViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private readonly IPasswordGenerator _generator;

    [ObservableProperty] private string masterPassphrase = string.Empty;
    [ObservableProperty] private string confirmation = string.Empty;
    [ObservableProperty] private string strengthLabel = "Enter a long unique master passphrase.";
    [ObservableProperty] private bool recoveryLimitAcknowledged;
    [ObservableProperty] private bool recoveryKeyEnabled = true;
    [ObservableProperty] private bool recoveryKeySaved;
    [ObservableProperty] private string recoveryKey = string.Empty;
    [ObservableProperty] private bool showRecoveryKey;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string errorMessage = string.Empty;

    public OnboardingViewModel(IVaultService vault, IPasswordGenerator generator)
    {
        _vault = vault;
        _generator = generator;
    }

    partial void OnMasterPassphraseChanged(string value) { StrengthLabel = _generator.Evaluate(value).Label; CreateVaultCommand.NotifyCanExecuteChanged(); }
    partial void OnConfirmationChanged(string value) => CreateVaultCommand.NotifyCanExecuteChanged();
    partial void OnRecoveryLimitAcknowledgedChanged(bool value) => CreateVaultCommand.NotifyCanExecuteChanged();
    partial void OnRecoveryKeySavedChanged(bool value) => ContinueCommand.NotifyCanExecuteChanged();
    partial void OnIsBusyChanged(bool value) => CreateVaultCommand.NotifyCanExecuteChanged();

    private bool CanCreate() => !IsBusy && RecoveryLimitAcknowledged && MasterPassphrase.Length >= 12 && _generator.Evaluate(MasterPassphrase).Score >= 3 && string.Equals(MasterPassphrase, Confirmation, StringComparison.Ordinal);

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateVaultAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var recovery = await _vault.CreateAsync(MasterPassphrase, RecoveryKeyEnabled);
            MasterPassphrase = string.Empty;
            Confirmation = string.Empty;
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
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
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
}
