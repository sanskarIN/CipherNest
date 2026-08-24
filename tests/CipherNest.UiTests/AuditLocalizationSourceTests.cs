using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class AuditLocalizationSourceTests
{
    private static readonly string[] AuditKeys =
    [
        "AuditTitle",
        "AuditEmptyView",
        "AuditSeverityFormat",
        "AuditRunAgainButton",
        "AuditInitialSummary",
        "AuditNoFindingsSummary",
        "AuditFindingsSummaryFormat",
        "AuditFailureSummary",
        "AuditKindMissingTitle",
        "AuditKindWeakSecret",
        "AuditKindExpiredReview",
        "AuditKindReusedSecret",
        "AuditKindDuplicateEntry",
        "AuditMessageMissingTitle",
        "AuditMessageWeakSecret",
        "AuditMessageExpiredReview",
        "AuditMessageReusedSecret",
        "AuditMessageDuplicateEntry"
    ];

    [Fact]
    public void LocalizationService_RegistersAuditFeatureCatalog()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Services", "LocalizationService.cs"));
        Assert.Contains("CipherNest.App.Resources.Localization.AuditStrings", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditPage_UsesLocalizedFixedInterfaceText()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AuditPage.xaml"));

        Assert.Contains("xmlns:l10n=\"clr-namespace:CipherNest.App.Localization\"", xaml, StringComparison.Ordinal);
        Assert.Contains("{l10n:Translate AuditTitle}", xaml, StringComparison.Ordinal);
        Assert.Contains("{l10n:Translate BackButton}", xaml, StringComparison.Ordinal);
        Assert.Contains("{l10n:Translate AuditEmptyView}", xaml, StringComparison.Ordinal);
        Assert.Contains("{l10n:Translate AuditRunAgainButton}", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Security audit\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptyView=\"No findings from the local checks.\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("StringFormat='Severity: {0}/4'", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditViewModel_LocalizesSummariesFindingsAndSeverity()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "AuditViewModel.cs"));

        Assert.Contains("ILocalizationService _localization", source, StringComparison.Ordinal);
        Assert.Contains("AuditText(\"AuditInitialSummary\")", source, StringComparison.Ordinal);
        Assert.Contains("AuditText(\"AuditNoFindingsSummary\")", source, StringComparison.Ordinal);
        Assert.Contains("AuditText(\"AuditFindingsSummaryFormat\")", source, StringComparison.Ordinal);
        Assert.Contains("AuditText(\"AuditFailureSummary\")", source, StringComparison.Ordinal);
        Assert.Contains("AuditText(\"AuditSeverityFormat\")", source, StringComparison.Ordinal);
        Assert.Contains("CultureInfo.CurrentUICulture", source, StringComparison.Ordinal);

        foreach (var key in AuditKeys.Skip(8))
        {
            Assert.Contains($"\"{key}\"", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AuditCatalogs_HaveExactKeyParityAndReviewedHindiValues()
    {
        var neutral = ReadCatalog("AuditStrings.resx");
        var hindi = ReadCatalog("AuditStrings.hi-IN.resx");

        Assert.Equal(AuditKeys.Order(StringComparer.Ordinal), neutral.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(neutral.Keys.Order(StringComparer.Ordinal), hindi.Keys.Order(StringComparer.Ordinal));

        foreach (var key in AuditKeys)
        {
            Assert.False(string.IsNullOrWhiteSpace(neutral[key]), $"Neutral Audit localization key '{key}' is empty.");
            Assert.False(string.IsNullOrWhiteSpace(hindi[key]), $"Hindi Audit localization key '{key}' is empty.");
            Assert.NotEqual(neutral[key], hindi[key]);
        }

        Assert.Contains("{0}", neutral["AuditSeverityFormat"], StringComparison.Ordinal);
        Assert.Contains("{0}", hindi["AuditSeverityFormat"], StringComparison.Ordinal);
        Assert.Contains("{0}", neutral["AuditFindingsSummaryFormat"], StringComparison.Ordinal);
        Assert.Contains("{0}", hindi["AuditFindingsSummaryFormat"], StringComparison.Ordinal);
        Assert.Contains("डुप्लिकेट", hindi["AuditKindDuplicateEntry"], StringComparison.Ordinal);
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
