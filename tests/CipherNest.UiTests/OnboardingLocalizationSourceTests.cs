namespace CipherNest.UiTests;

public sealed class OnboardingLocalizationSourceTests
{
    [Fact]
    public void OnboardingPage_UsesReviewedRecoveryAndSetupResources()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "OnboardingPage.xaml"));

        Assert.Contains("xmlns:l10n=\"clr-namespace:CipherNest.App.Localization\"", xaml, StringComparison.Ordinal);
        foreach (var key in new[]
        {
            "CipherNestLogoSemanticDescription",
            "OnboardingCreateVaultTitle",
            "OnboardingLocalSummary",
            "OnboardingSaveRecoveryKeyTitle",
            "OnboardingRecoveryKeyExplanation",
            "OnboardingRecoveryKeySemanticDescription",
            "OnboardingRecoveryKeySavedAcknowledgement",
            "OnboardingContinueButton",
            "OnboardingRecoveryLimitationTitle",
            "OnboardingRecoveryLimitationBody",
            "MasterPassphraseLabel",
            "MasterPassphrasePlaceholder",
            "ConfirmMasterPassphraseLabel",
            "ConfirmMasterPassphrasePlaceholder",
            "OnboardingGenerateRecoveryOption",
            "OnboardingRecoveryAcknowledgement",
            "OnboardingErrorSemanticDescription",
            "OnboardingCreateButton"
        })
        {
            Assert.Contains($"{{l10n:Translate {key}}}", xaml, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Text=\"Save your recovery key now\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("CipherNest cannot retrieve it later", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Recovery limitation\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Create vault\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void OnboardingViewModel_LocalizesStrengthAndFailureStatuses()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "OnboardingViewModel.cs"));

        foreach (var key in new[]
        {
            "OnboardingStrengthInitial",
            "OnboardingMasterTooLongFormat",
            "PasswordStrengthEmpty",
            "PasswordStrengthVeryWeak",
            "PasswordStrengthWeak",
            "PasswordStrengthFair",
            "PasswordStrengthStrong",
            "PasswordStrengthVeryStrong",
            "OnboardingMasterRequirementsErrorFormat",
            "OnboardingVaultExistsError",
            "OnboardingCreateFailureError"
        })
        {
            Assert.Contains(key, source, StringComparison.Ordinal);
        }

        Assert.Contains("CultureInfo.CurrentUICulture", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Master passphrase cannot exceed {MaximumMasterPassphraseCharacters:N0} characters.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("A local vault already exists or could not be initialized safely.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("The local vault could not be created safely.", source, StringComparison.Ordinal);
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
