using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class SettingsSecurityLocalizationSourceTests
{
    private static readonly string[] SecurityKeys =
    [
        "SettingsBiometricTitle",
        "SettingsBiometricSummary",
        "SettingsRequireMasterAfterLabel",
        "SettingsRequireMasterRange",
        "SettingsCurrentMasterPlaceholder",
        "SettingsEnableBiometricsButton",
        "SettingsDisableBiometricsButton",
        "SettingsSecurityReviewTitle",
        "SettingsSecurityReviewSummary",
        "SettingsRunSecurityAuditButton",
        "SettingsSecurityPrivacyInfoButton",
        "SettingsBackupRestoreTitle",
        "SettingsBackupRestoreSummary",
        "SettingsBackupPassphrasePlaceholder",
        "SettingsCreateBackupButton",
        "SettingsRestoreBackupButton",
        "SettingsImportExportTitle",
        "SettingsImportExportSummary",
        "SettingsOpenImportExportButton",
        "SettingsChangeMasterTitle",
        "SettingsNewMasterPlaceholder",
        "SettingsConfirmNewMasterPlaceholder",
        "SettingsChangeMasterButton",
        "SettingsDangerZoneTitle",
        "SettingsDangerZoneBody",
        "SettingsDeletionPhrasePlaceholder",
        "SettingsDeleteVaultButton",
        "SettingsStatusSemanticDescription"
    ];

    [Fact]
    public void SettingsPage_UsesReviewedResourcesForSecurityDecisionSurfaces()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "SettingsPage.xaml"));

        Assert.Contains("xmlns:l10n=\"clr-namespace:CipherNest.App.Localization\"", xaml, StringComparison.Ordinal);
        Assert.Contains("{l10n:Translate SettingsTitle}", xaml, StringComparison.Ordinal);
        Assert.Contains("{l10n:Translate BackButton}", xaml, StringComparison.Ordinal);
        foreach (var key in SecurityKeys)
        {
            Assert.Contains($"{{l10n:Translate {key}}}", xaml, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Biometrics are optional and never replace your master passphrase", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Use a separate strong backup passphrase", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Deleting the local vault cannot guarantee physical erasure", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Change master passphrase\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SettingsSecurityCatalog_HasDistinctReviewedHindiValuesAndKeepsDeletePhraseLiteral()
    {
        var neutral = ReadCatalog("AppStrings.resx");
        var hindi = ReadCatalog("AppStrings.hi-IN.resx");

        foreach (var key in SecurityKeys)
        {
            Assert.True(neutral.ContainsKey(key), $"Neutral Settings localization key '{key}' is missing.");
            Assert.True(hindi.ContainsKey(key), $"Hindi Settings localization key '{key}' is missing.");
            Assert.False(string.IsNullOrWhiteSpace(neutral[key]));
            Assert.False(string.IsNullOrWhiteSpace(hindi[key]));
            Assert.NotEqual(neutral[key], hindi[key]);
        }

        Assert.Contains("DELETE MY VAULT", neutral["SettingsDeletionPhrasePlaceholder"], StringComparison.Ordinal);
        Assert.Contains("DELETE MY VAULT", hindi["SettingsDeletionPhrasePlaceholder"], StringComparison.Ordinal);
        Assert.Contains("cannot guarantee physical erasure", neutral["SettingsDangerZoneBody"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("never replace your master passphrase or recovery key", neutral["SettingsBiometricSummary"], StringComparison.OrdinalIgnoreCase);
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
