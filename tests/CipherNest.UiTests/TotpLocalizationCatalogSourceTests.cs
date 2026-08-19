using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class TotpLocalizationCatalogSourceTests
{
    private static readonly string[] TotpKeys =
    [
        "TotpHeading",
        "TotpSeedSummary",
        "TotpImportHeading",
        "TotpImportSummary",
        "TotpImportPlaceholder",
        "TotpImportButton",
        "TotpCopySetupUriButton",
        "TotpAlgorithmLabel",
        "TotpDigitsLabel",
        "TotpRefreshCodeButton",
        "TotpCopyCodeButton",
        "TotpImportSemanticDescription",
        "TotpCopySetupUriSemanticDescription",
        "TotpCurrentCodeSemanticDescription",
        "TotpCopyCodeSemanticDescription",
        "TotpSafetyWarning"
    ];

    [Fact]
    public void TotpSecurityCatalog_HasNeutralAndHindiEntries()
    {
        var neutral = ReadCatalog("AppStrings.resx");
        var hindi = ReadCatalog("AppStrings.hi-IN.resx");

        foreach (var key in TotpKeys)
        {
            Assert.True(neutral.ContainsKey(key), $"Neutral TOTP localization key '{key}' is missing.");
            Assert.True(hindi.ContainsKey(key), $"Hindi TOTP localization key '{key}' is missing.");
            Assert.False(string.IsNullOrWhiteSpace(neutral[key]), $"Neutral TOTP localization key '{key}' is blank.");
            Assert.False(string.IsNullOrWhiteSpace(hindi[key]), $"Hindi TOTP localization key '{key}' is blank.");
        }
    }

    [Fact]
    public void TotpSecurityCatalog_TranslatesSensitiveWarningsAndActions()
    {
        var neutral = ReadCatalog("AppStrings.resx");
        var hindi = ReadCatalog("AppStrings.hi-IN.resx");

        foreach (var key in TotpKeys.Where(key => key != "TotpImportPlaceholder"))
        {
            Assert.NotEqual(neutral[key], hindi[key]);
        }

        Assert.Equal("otpauth://totp/...", neutral["TotpImportPlaceholder"]);
        Assert.Equal(neutral["TotpImportPlaceholder"], hindi["TotpImportPlaceholder"]);
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
