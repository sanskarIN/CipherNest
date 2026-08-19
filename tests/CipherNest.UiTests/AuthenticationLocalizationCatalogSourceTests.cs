using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class AuthenticationLocalizationCatalogSourceTests
{
    private static readonly string[] AuthenticationKeys =
    [
        "CipherNestLogoSemanticDescription",
        "UnlockTitle",
        "BiometricUnlockButton",
        "BiometricUnlockSemanticDescription",
        "UnlockAlternativeLabel",
        "UnlockCredentialPlaceholder",
        "UnlockCredentialSemanticDescription",
        "UnlockErrorSemanticDescription",
        "UnlockButton",
        "UnlockRecoveryWarning",
        "UnlockMasterSessionRequired",
        "UnlockRateLimitFormat",
        "UnlockAuthenticationError",
        "UnlockPeriodicMasterDue",
        "UnlockBiometricPrompt",
        "UnlockBiometricFailed",
        "UnlockBiometricSecretUnavailable",
        "UnlockBiometricDataMismatch",
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
        "OnboardingStrengthInitial",
        "OnboardingMasterTooLongFormat",
        "PasswordStrengthEmpty",
        "PasswordStrengthVeryWeak",
        "PasswordStrengthWeak",
        "PasswordStrengthFair",
        "PasswordStrengthStrong",
        "PasswordStrengthVeryStrong",
        "ConfirmMasterPassphraseLabel",
        "ConfirmMasterPassphrasePlaceholder",
        "OnboardingGenerateRecoveryOption",
        "OnboardingRecoveryAcknowledgement",
        "OnboardingErrorSemanticDescription",
        "OnboardingCreateButton",
        "OnboardingMasterRequirementsErrorFormat",
        "OnboardingVaultExistsError",
        "OnboardingCreateFailureError"
    ];

    [Fact]
    public void AuthenticationCatalog_HasReviewedNeutralAndHindiValues()
    {
        var neutral = ReadCatalog("AppStrings.resx");
        var hindi = ReadCatalog("AppStrings.hi-IN.resx");

        foreach (var key in AuthenticationKeys)
        {
            Assert.True(neutral.ContainsKey(key), $"Neutral authentication localization key '{key}' is missing.");
            Assert.True(hindi.ContainsKey(key), $"Hindi authentication localization key '{key}' is missing.");
            Assert.False(string.IsNullOrWhiteSpace(neutral[key]), $"Neutral authentication localization key '{key}' is blank.");
            Assert.False(string.IsNullOrWhiteSpace(hindi[key]), $"Hindi authentication localization key '{key}' is blank.");
            Assert.NotEqual(neutral[key], hindi[key]);
        }
    }

    [Fact]
    public void AuthenticationCatalog_PreservesRequiredFormatPlaceholders()
    {
        var neutral = ReadCatalog("AppStrings.resx");
        var hindi = ReadCatalog("AppStrings.hi-IN.resx");

        foreach (var catalog in new[] { neutral, hindi })
        {
            Assert.Contains("{0}", catalog["UnlockRateLimitFormat"], StringComparison.Ordinal);
            Assert.Contains("{0:N0}", catalog["OnboardingMasterTooLongFormat"], StringComparison.Ordinal);
            Assert.Contains("{0:N0}", catalog["OnboardingMasterRequirementsErrorFormat"], StringComparison.Ordinal);
            Assert.Contains("{1:N0}", catalog["OnboardingMasterRequirementsErrorFormat"], StringComparison.Ordinal);
        }
    }

    private static Dictionary<string, string> ReadCatalog(string fileName)
    {
        var document = XDocument.Load(PathAt("src", "CipherNest.App", "Resources", "Localization", fileName));
        return document.Root!
            .Elements("data")
            .ToDictionary(
                element => (string)element.Attribute("name")!,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
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
