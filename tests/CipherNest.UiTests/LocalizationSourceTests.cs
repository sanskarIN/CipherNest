using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class LocalizationSourceTests
{
    [Fact]
    public void HindiCatalog_MatchesNeutralCatalogKeys()
    {
        var neutral = ReadCatalog("AppStrings.resx");
        var hindi = ReadCatalog("AppStrings.hi-IN.resx");

        Assert.NotEmpty(neutral);
        Assert.Equal(neutral.Keys.Order(StringComparer.Ordinal), hindi.Keys.Order(StringComparer.Ordinal));
        Assert.All(hindi, pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value), $"Hindi resource '{pair.Key}' is blank."));
    }

    [Fact]
    public void HindiCatalog_TranslatesSecurityCriticalMessages()
    {
        var neutral = ReadCatalog("AppStrings.resx");
        var hindi = ReadCatalog("AppStrings.hi-IN.resx");

        foreach (var key in new[] { "LocalOnlySummary", "AuditStatus", "RecoveryLimitation", "HindiPreferenceSaved", "SystemPreferenceSaved" })
        {
            Assert.True(neutral.ContainsKey(key), $"Neutral resource '{key}' is missing.");
            Assert.True(hindi.ContainsKey(key), $"Hindi resource '{key}' is missing.");
            Assert.NotEqual(neutral[key], hindi[key]);
        }
    }

    [Fact]
    public void LanguagePreferenceAndService_WireHindiToHiIn()
    {
        var preference = File.ReadAllText(PathAt("src", "CipherNest.Domain", "Models", "AppLanguagePreference.cs"));
        var service = File.ReadAllText(PathAt("src", "CipherNest.App", "Services", "LocalizationService.cs"));
        var settings = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.Localization.cs"));

        Assert.Contains("Hindi", preference, StringComparison.Ordinal);
        Assert.Contains("AppLanguagePreference.Hindi => CultureInfo.GetCultureInfo(\"hi-IN\")", service, StringComparison.Ordinal);
        Assert.Contains("AppLanguagePreference.Hindi => localization.Get(\"HindiPreferenceSaved\")", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalizationDocumentation_DoesNotClaimFullyTranslatedInterface()
    {
        var documentation = File.ReadAllText(PathAt("docs", "architecture", "LOCALIZATION.md"));

        Assert.Contains("Hindi", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("resource-backed", documentation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("English", documentation, StringComparison.OrdinalIgnoreCase);
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
