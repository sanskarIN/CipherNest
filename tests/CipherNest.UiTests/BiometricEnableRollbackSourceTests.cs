namespace CipherNest.UiTests;

public sealed class BiometricEnableRollbackSourceTests
{
    [Fact]
    public void EnableBiometricUnlock_RollsBackVaultAndSecureSecretBeforeReportingFailure()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.cs"));
        var enableStart = source.IndexOf("private async Task EnableBiometricUnlockAsync()", StringComparison.Ordinal);
        var disableStart = source.IndexOf("private async Task DisableBiometricUnlockAsync()", enableStart, StringComparison.Ordinal);
        var method = source[enableStart..disableStart];

        Assert.Contains("var secondaryConfigured = false;", method, StringComparison.Ordinal);
        Assert.Contains("secondaryConfigured = true;", method, StringComparison.Ordinal);
        Assert.Contains("await _vault.DisableSecondaryUnlockAsync(masterPassphrase);", method, StringComparison.Ordinal);
        Assert.Contains("await _biometrics.ClearSecondarySecretAsync();", method, StringComparison.Ordinal);
        Assert.Contains("Settings.BiometricEnable.VaultRollback", method, StringComparison.Ordinal);
        Assert.Contains("Settings.BiometricEnable.SecretRollback", method, StringComparison.Ordinal);

        var saveIndex = method.IndexOf("await _settings.SaveAsync(enabledPreferences);", StringComparison.Ordinal);
        var enabledUiIndex = method.IndexOf("BiometricUnlockEnabled = true;", StringComparison.Ordinal);
        Assert.True(saveIndex >= 0 && enabledUiIndex > saveIndex, "Biometric UI state must not be published before settings persistence succeeds.");
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
