namespace CipherNest.App.ViewModels;

public partial class OnboardingViewModel
{
    public void ClearSensitiveState()
    {
        MasterPassphrase = string.Empty;
        Confirmation = string.Empty;
        RecoveryKey = string.Empty;
        ShowRecoveryKey = false;
        RecoveryKeySaved = false;
    }
}
