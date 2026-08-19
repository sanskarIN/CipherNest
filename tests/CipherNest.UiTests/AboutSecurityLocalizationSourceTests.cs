using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class AboutSecurityLocalizationSourceTests
{
    private static readonly string[] AboutSecurityKeys =
    [
        "AboutSecurityStatusTitle",
        "AboutSecurityStatusBody",
        "AboutSecurityPrivacyButton",
        "AboutPrivacyTermsTitle",
        "AboutPrivacyTermsBody"
    ];

    [Fact]
    public void AboutPage_UsesReviewedSecurityAndPrivacyResources()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AboutPage.xaml"));

        Assert.Contains("xmlns:l10n=\"clr-namespace:CipherNest.App.Localization\"", xaml, StringComparison.Ordinal);
        Assert.Contains("{l10n:Translate CipherNestLogoSemanticDescription}", xaml, StringComparison.Ordinal);
        foreach (var key in AboutSecurityKeys)
        {
            Assert.Contains($"{{l10n:Translate {key}}}", xaml, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Text=\"Security status\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("this release has not completed an independent professional security audit", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("User-initiated plaintext export leaves the protected vault boundary", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void AboutSecurityCatalog_HasDistinctReviewedHindiValues()
    {
        var neutral = ReadCatalog("AppStrings.resx");
        var hindi = ReadCatalog("AppStrings.hi-IN.resx");

        foreach (var key in AboutSecurityKeys)
        {
            Assert.True(neutral.ContainsKey(key), $"Neutral About localization key '{key}' is missing.");
            Assert.True(hindi.ContainsKey(key), $"Hindi About localization key '{key}' is missing.");
            Assert.False(string.IsNullOrWhiteSpace(neutral[key]));
            Assert.False(string.IsNullOrWhiteSpace(hindi[key]));
            Assert.NotEqual(neutral[key], hindi[key]);
        }

        Assert.Contains("not completed an independent professional security audit", neutral["AboutSecurityStatusBody"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("plaintext export leaves the protected vault boundary", neutral["AboutPrivacyTermsBody"], StringComparison.OrdinalIgnoreCase);
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
