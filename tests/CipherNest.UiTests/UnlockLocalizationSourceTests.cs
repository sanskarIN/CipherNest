namespace CipherNest.UiTests;

public sealed class UnlockLocalizationSourceTests
{
    [Fact]
    public void UnlockPage_UsesReviewedLocalizationResources()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "UnlockPage.xaml"));

        Assert.Contains("xmlns:l10n=\"clr-namespace:CipherNest.App.Localization\"", xaml, StringComparison.Ordinal);
        foreach (var key in new[]
        {
            "CipherNestLogoSemanticDescription",
            "UnlockTitle",
            "LocalOnlySummary",
            "BiometricUnlockButton",
            "BiometricUnlockSemanticDescription",
            "UnlockAlternativeLabel",
            "UnlockCredentialPlaceholder",
            "UnlockCredentialSemanticDescription",
            "UnlockErrorSemanticDescription",
            "UnlockButton",
            "UnlockRecoveryWarning"
        })
        {
            Assert.Contains($"{{l10n:Translate {key}}}", xaml, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Text=\"Unlock CipherNest\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Unlock with biometrics\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Forgotten passphrases cannot be recovered", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void UnlockViewModel_UsesLocalizedSecurityStatusesAndBiometricPrompt()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "UnlockViewModel.cs"));

        foreach (var key in new[]
        {
            "UnlockMasterSessionRequired",
            "UnlockRateLimitFormat",
            "UnlockAuthenticationError",
            "UnlockPeriodicMasterDue",
            "UnlockBiometricPrompt",
            "UnlockBiometricFailed",
            "UnlockBiometricSecretUnavailable",
            "UnlockBiometricDataMismatch"
        })
        {
            Assert.Contains(key, source, StringComparison.Ordinal);
        }

        Assert.Contains("CultureInfo.CurrentUICulture", source, StringComparison.Ordinal);
        Assert.Contains("AuthenticateAsync(UnlockText(\"UnlockBiometricPrompt\"))", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Biometric authentication was cancelled or failed.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("The passphrase is incorrect or the vault cannot be authenticated.", source, StringComparison.Ordinal);
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
