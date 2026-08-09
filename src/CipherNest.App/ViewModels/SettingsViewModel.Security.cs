namespace CipherNest.App.ViewModels;

public partial class SettingsViewModel
{
    public void ClearSensitiveState()
    {
        BackupPassphrase = string.Empty;
        CurrentMasterPassphrase = string.Empty;
        NewMasterPassphrase = string.Empty;
        ConfirmNewMasterPassphrase = string.Empty;
        DeletionMasterPassphrase = string.Empty;
        DeletionConfirmationPhrase = string.Empty;
    }
}
