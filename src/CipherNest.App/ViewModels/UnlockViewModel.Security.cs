namespace CipherNest.App.ViewModels;

public partial class UnlockViewModel
{
    public void ClearSensitiveState()
    {
        MasterPassphrase = string.Empty;
    }
}
