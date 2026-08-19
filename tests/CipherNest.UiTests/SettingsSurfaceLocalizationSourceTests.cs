using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class SettingsSurfaceLocalizationSourceTests
{
    private static readonly string[] SurfaceKeys =
    [
        "SettingsAppearanceAccessibilityTitle",
        "SettingsThemeLabel",
        "SettingsLanguageLabel",
        "SettingsLanguageSummary",
        "SettingsSaveLanguageButton",
        "SettingsReducedMotionLabel",
        "SettingsLargerInterfaceLabel",
        "SettingsLockPrivacyTitle",
        "SettingsLockTimeoutLabel",
        "SettingsLockOnBackgroundLabel",
        "SettingsClipboardClearLabel",
        "SettingsScreenshotProtectionLabel",
        "SettingsTrashRetentionLabel",
        "SettingsLocalRemindersTitle",
        "SettingsBackupReminderLabel",
        "SettingsReviewRemindersLabel",
        "SettingsReviewReminderLeadLabel",
        "SettingsReviewReminderSummary",
        "SettingsSaveSettingsButton",
        "SettingsGeneratorDefaultsTitle",
        "SettingsGeneratorDefaultsSummary",
        "SettingsConfigureGeneratorButton",
        "SettingsStorageCacheTitle",
        "SettingsStorageCacheSummary",
        "SettingsRefreshStorageButton",
        "SettingsClearCacheButton",
        "SettingsFundingTitle",
        "SettingsFundingBadgeSemanticDescription",
        "SettingsFundingSummary",
        "SettingsFundingButton",
        "SettingsFundingButtonSemanticDescription",
        "SettingsAboutLegalTitle",
        "SettingsAboutLegalSummary",
        "SettingsOpenAboutLegalButton"
    ];

    [Fact]
    public void SettingsPage_UsesResourcesForRemainingFixedSurface()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "SettingsPage.xaml"));

        foreach (var key in SurfaceKeys)
        {
            Assert.Contains($"{{l10n:Translate {key}}}", xaml, StringComparison.Ordinal);
        }

        string[] removedLiterals =
        [
            "Text=\"Appearance &amp; accessibility\"",
            "Text=\"Lock &amp; privacy\"",
            "Text=\"Local reminders\"",
            "Text=\"Generator defaults\"",
            "Text=\"Storage &amp; cache\"",
            "Text=\"☕ Support CipherNest development\"",
            "Text=\"About, legal &amp; acknowledgements\"",
            "SemanticProperties.Description=\"BMC Support CipherNest badge\"",
            "SemanticProperties.Description=\"Open the About page with the Buy Me a Coffee support link\""
        ];

        foreach (var literal in removedLiterals)
        {
            Assert.DoesNotContain(literal, xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RemainingSettingsCatalog_HasReviewedHindiParity()
    {
        var neutral = ReadCatalog("AppStrings.resx");
        var hindi = ReadCatalog("AppStrings.hi-IN.resx");

        foreach (var key in SurfaceKeys)
        {
            Assert.True(neutral.ContainsKey(key), $"Neutral Settings localization key '{key}' is missing.");
            Assert.True(hindi.ContainsKey(key), $"Hindi Settings localization key '{key}' is missing.");
            Assert.False(string.IsNullOrWhiteSpace(neutral[key]));
            Assert.False(string.IsNullOrWhiteSpace(hindi[key]));
            Assert.NotEqual(neutral[key], hindi[key]);
        }

        Assert.Contains("does not send vault details", neutral["SettingsReviewReminderSummary"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("never intentionally deletes the encrypted vault database", neutral["SettingsStorageCacheSummary"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("optional", neutral["SettingsFundingSummary"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("never changes", neutral["SettingsFundingSummary"], StringComparison.OrdinalIgnoreCase);
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