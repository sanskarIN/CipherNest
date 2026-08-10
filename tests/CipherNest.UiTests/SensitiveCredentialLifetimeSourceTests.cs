namespace CipherNest.UiTests;

public sealed class SensitiveCredentialLifetimeSourceTests
{
    [Fact]
    public void SensitiveViewModels_ClearBoundCredentialsBeforeLongRunningSecurityWork()
    {
        var unlock = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "UnlockViewModel.cs"));
        AssertBefore(unlock, "MasterPassphrase = string.Empty;", "await _vault.UnlockAsync(passphrase)");

        var onboarding = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "OnboardingViewModel.cs"));
        AssertBefore(onboarding, "MasterPassphrase = string.Empty;", "await _vault.CreateAsync(passphrase");
        AssertBefore(onboarding, "Confirmation = string.Empty;", "await _vault.CreateAsync(passphrase");

        var transfer = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TransferViewModel.cs"));
        AssertBefore(transfer, "ExportMasterPassphrase = string.Empty;", "DisplayAlertAsync(\"Create plaintext export?");

        var trash = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TrashViewModel.cs"));
        AssertBefore(trash, "DeletionPassphrase = string.Empty;", "return true;");

        var itemEditor = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.cs"));
        AssertBefore(itemEditor, "ReauthenticationPassphrase = string.Empty;", "if (!authenticated)");

        var settings = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.cs"));
        AssertBefore(settings, "CurrentMasterPassphrase = string.Empty;", "await _vault.ReauthenticateAsync(masterPassphrase)");
        AssertBefore(settings, "BackupPassphrase = string.Empty;", "DisplayAlertAsync(\"Create a consistent encrypted backup?");
        AssertBefore(settings, "BackupPassphrase = string.Empty;", "FilePicker.Default.PickAsync");
        AssertBefore(settings, "CurrentMasterPassphrase = NewMasterPassphrase = ConfirmNewMasterPassphrase = string.Empty;", "await _vault.ChangeMasterPassphraseAsync(currentMasterPassphrase, newMasterPassphrase)");
        AssertBefore(settings, "DeletionMasterPassphrase = DeletionConfirmationPhrase = string.Empty;", "DisplayAlertAsync(\"Permanently delete this local vault?");
    }

    private static void AssertBefore(string source, string earlier, string later)
    {
        var earlierIndex = source.IndexOf(earlier, StringComparison.Ordinal);
        var laterIndex = source.IndexOf(later, StringComparison.Ordinal);
        Assert.True(earlierIndex >= 0, $"Expected source to contain: {earlier}");
        Assert.True(laterIndex >= 0, $"Expected source to contain: {later}");
        Assert.True(earlierIndex < laterIndex, $"Expected '{earlier}' before '{later}'.");
    }

    private static string PathAt(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CipherNest.slnx"))) directory = directory.Parent;
        if (directory is null) throw new DirectoryNotFoundException("Could not locate the CipherNest repository root from the test output directory.");
        var path = directory.FullName;
        foreach (var segment in segments) path = Path.Combine(path, segment);
        return path;
    }
}
