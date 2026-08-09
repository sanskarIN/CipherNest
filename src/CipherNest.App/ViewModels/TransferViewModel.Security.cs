namespace CipherNest.App.ViewModels;

public partial class TransferViewModel
{
    public void ClearSensitiveState()
    {
        ExportMasterPassphrase = string.Empty;
        ExportConfirmationPhrase = string.Empty;
    }
}
