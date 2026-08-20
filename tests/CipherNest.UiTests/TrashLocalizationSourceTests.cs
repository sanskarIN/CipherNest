using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class TrashLocalizationSourceTests
{
    private static readonly string[] TrashKeys =
    [
        "TrashTitle",
        "TrashStatusSemanticDescription",
        "TrashPermanentDeletionTitle",
        "TrashPermanentDeletionSummary",
        "TrashCurrentMasterPlaceholder",
        "TrashEmptyButton",
        "TrashEmptyView",
        "TrashDeletedLabel",
        "TrashRestoreButton",
        "TrashDeleteButton",
        "TrashEmptyStatus",
        "TrashStatusFormat",
        "TrashAlreadyEmptyStatus",
        "TrashDeleteConfirmTitle",
        "TrashDeleteConfirmBody",
        "TrashDeleteConfirmAccept",
        "TrashEmptyConfirmTitle",
        "TrashEmptyConfirmBodyFormat",
        "TrashEmptyConfirmAccept",
        "TrashEmptiedStatus",
        "TrashMasterRequiredStatus",
        "TrashMasterConfirmationFailedStatus"
    ];

    [Fact]
    public void LocalizationService_RegistersTrashFeatureCatalog()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Services", "LocalizationService.cs"));

        Assert.Contains("FeatureResources", source, StringComparison.Ordinal);
        Assert.Contains("CipherNest.App.Resources.Localization.TrashStrings", source, StringComparison.Ordinal);
        Assert.Contains("foreach (var resources in FeatureResources)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TrashPage_UsesReviewedResourcesForFixedInterfaceText()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "TrashPage.xaml"));

        Assert.Contains("xmlns:l10n=\"clr-namespace:CipherNest.App.Localization\"", xaml, StringComparison.Ordinal);
        Assert.Contains("{l10n:Translate BackButton}", xaml, StringComparison.Ordinal);
        foreach (var key in TrashKeys.Take(10))
        {
            Assert.Contains($"{{l10n:Translate {key}}}", xaml, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Text=\"Trash\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Permanent deletion\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Placeholder=\"Current master passphrase\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptyView=\"Trash is empty.\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Restore\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Delete\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void TrashViewModel_UsesLocalizedRuntimeSafetyMessages()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TrashViewModel.cs"));

        foreach (var key in TrashKeys.Skip(10))
        {
            Assert.Contains($"\"{key}\"", source, StringComparison.Ordinal);
        }
        Assert.Contains("TrashText(\"CancelButton\")", source, StringComparison.Ordinal);
        Assert.Contains("string.Format(CultureInfo.CurrentUICulture", source, StringComparison.Ordinal);
        Assert.Contains("Items.Clear();\n            StatusMessage = TrashText(\"TrashEmptiedStatus\");", source, StringComparison.Ordinal);

        Assert.DoesNotContain("\"Delete permanently?\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Empty trash permanently?\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Trash is already empty.\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Master-passphrase confirmation failed. Nothing was permanently deleted.\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TrashCatalogs_HaveExactKeyParityAndReviewedHindiValues()
    {
        var neutral = ReadCatalog("TrashStrings.resx");
        var hindi = ReadCatalog("TrashStrings.hi-IN.resx");

        Assert.Equal(TrashKeys.Order(StringComparer.Ordinal), neutral.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(neutral.Keys.Order(StringComparer.Ordinal), hindi.Keys.Order(StringComparer.Ordinal));

        foreach (var key in TrashKeys)
        {
            Assert.False(string.IsNullOrWhiteSpace(neutral[key]), $"Neutral Trash localization key '{key}' is empty.");
            Assert.False(string.IsNullOrWhiteSpace(hindi[key]), $"Hindi Trash localization key '{key}' is empty.");
            Assert.NotEqual(neutral[key], hindi[key]);
        }

        Assert.Contains("Recovery keys are not accepted", neutral["TrashPermanentDeletionSummary"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("remnants", neutral["TrashDeleteConfirmBody"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("रिकवरी कुंजियाँ", hindi["TrashPermanentDeletionSummary"], StringComparison.Ordinal);
        Assert.Contains("फॉरेंसिक", hindi["TrashDeleteConfirmBody"], StringComparison.Ordinal);
    }

    [Fact]
    public void TrashCatalogs_PreserveRequiredFormatPlaceholders()
    {
        var neutral = ReadCatalog("TrashStrings.resx");
        var hindi = ReadCatalog("TrashStrings.hi-IN.resx");

        foreach (var catalog in new[] { neutral, hindi })
        {
            Assert.Contains("{0}", catalog["TrashStatusFormat"], StringComparison.Ordinal);
            Assert.Contains("{1}", catalog["TrashStatusFormat"], StringComparison.Ordinal);
            Assert.Contains("{0}", catalog["TrashEmptyConfirmBodyFormat"], StringComparison.Ordinal);
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
