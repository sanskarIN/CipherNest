using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class TransferLocalizationSourceTests
{
    private static readonly string[] TransferKeys =
    [
        "TransferTitle",
        "TransferImportHeading",
        "TransferImportSummary",
        "TransferSelectCsvButton",
        "TransferTitleColumnRequiredLabel",
        "TransferUsernameColumnLabel",
        "TransferSecretColumnLabel",
        "TransferUrlColumnLabel",
        "TransferNotesColumnLabel",
        "TransferTagsColumnLabel",
        "TransferCollectionColumnLabel",
        "TransferTypeColumnLabel",
        "TransferImportMappingsButton",
        "TransferPlaintextExportHeading",
        "TransferPlaintextExportWarning",
        "TransferMasterPlaceholder",
        "TransferConfirmationPlaceholder",
        "TransferExportButton",
        "TransferCleanCacheButton",
        "TransferExportMasterSemanticDescription",
        "TransferExportPhraseSemanticDescription",
        "TransferExportButtonSemanticDescription",
        "TransferCleanCacheSemanticDescription",
        "TransferNoCsvSelected",
        "TransferFilePickerTitle",
        "TransferReviewMappingsStatus",
        "TransferCsvSelectFailureStatus",
        "TransferSelectAndMapTitleStatus",
        "TransferImportConfirmTitle",
        "TransferImportConfirmBody",
        "TransferImportConfirmAccept",
        "TransferImportConfirmFailureStatus",
        "TransferImportResultFormat",
        "TransferImportResultWithWarningsFormat",
        "TransferImportFailureStatus",
        "TransferExportPhraseRequiredFormat",
        "TransferMasterConfirmFailureStatus",
        "TransferMasterFailedStatus",
        "TransferExportConfirmTitle",
        "TransferExportConfirmBody",
        "TransferExportConfirmAccept",
        "TransferExportConfirmFailureStatus",
        "TransferExportTemporaryStatus",
        "TransferShareTitle",
        "TransferExportFailureStatus",
        "TransferCleanupWarningSuffix",
        "TransferCacheCleanedStatus",
        "TransferCacheCleanFailureStatus"
    ];

    [Fact]
    public void LocalizationService_RegistersTransferFeatureCatalog()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Services", "LocalizationService.cs"));

        Assert.Contains("CipherNest.App.Resources.Localization.TransferStrings", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TransferPage_UsesReviewedResourcesForFixedAndSemanticText()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "TransferPage.xaml"));

        Assert.Contains("xmlns:l10n=\"clr-namespace:CipherNest.App.Localization\"", xaml, StringComparison.Ordinal);
        Assert.Contains("{l10n:Translate BackButton}", xaml, StringComparison.Ordinal);
        foreach (var key in TransferKeys.Take(23))
        {
            Assert.Contains($"{{l10n:Translate {key}}}", xaml, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Text=\"Import &amp; plaintext export\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Import generic CSV\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Plaintext CSV export\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Placeholder=\"Confirm master passphrase\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Placeholder=\"Type EXPORT PLAINTEXT\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Export plaintext CSV\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void TransferViewModel_UsesLocalizedRuntimeAndConfirmationText()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TransferViewModel.cs"));

        foreach (var key in TransferKeys.Skip(23))
        {
            Assert.Contains($"\"{key}\"", source, StringComparison.Ordinal);
        }

        Assert.Contains("private const string ExportPhrase = \"EXPORT PLAINTEXT\";", source, StringComparison.Ordinal);
        Assert.Contains("ILocalizationService localization", source, StringComparison.Ordinal);
        Assert.Contains("string.Format(CultureInfo.CurrentUICulture", source, StringComparison.Ordinal);
        Assert.Contains("Text(\"CancelButton\")", source, StringComparison.Ordinal);
        Assert.Contains("result.Warnings.Count == 0", source, StringComparison.Ordinal);

        Assert.DoesNotContain("\"Import plaintext CSV?\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Create plaintext export?\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Master-passphrase confirmation failed. Recovery keys are not accepted for plaintext export confirmation.\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string.Join(\" \", result.Warnings", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TransferCatalogs_HaveExactParityAndPreserveSecurityMeaning()
    {
        var neutral = ReadCatalog("TransferStrings.resx");
        var hindi = ReadCatalog("TransferStrings.hi-IN.resx");

        Assert.Equal(TransferKeys.Order(StringComparer.Ordinal), neutral.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(neutral.Keys.Order(StringComparer.Ordinal), hindi.Keys.Order(StringComparer.Ordinal));

        foreach (var key in TransferKeys)
        {
            Assert.False(string.IsNullOrWhiteSpace(neutral[key]), $"Neutral Transfer localization key '{key}' is empty.");
            Assert.False(string.IsNullOrWhiteSpace(hindi[key]), $"Hindi Transfer localization key '{key}' is empty.");
            Assert.NotEqual(neutral[key], hindi[key]);
        }

        Assert.Contains("Recovery keys are not accepted", neutral["TransferMasterFailedStatus"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("backups", neutral["TransferExportConfirmBody"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("snapshots", neutral["TransferCacheCleanedStatus"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("रिकवरी कुंजियाँ", hindi["TransferMasterFailedStatus"], StringComparison.Ordinal);
        Assert.Contains("एंटीवायरस", hindi["TransferExportConfirmBody"], StringComparison.Ordinal);
        Assert.Contains("स्नैपशॉट", hindi["TransferCacheCleanedStatus"], StringComparison.Ordinal);
    }

    [Fact]
    public void TransferCatalogs_PreserveFormatPlaceholdersAndExactAcknowledgementToken()
    {
        var neutral = ReadCatalog("TransferStrings.resx");
        var hindi = ReadCatalog("TransferStrings.hi-IN.resx");

        foreach (var catalog in new[] { neutral, hindi })
        {
            Assert.Contains("{0}", catalog["TransferImportResultFormat"], StringComparison.Ordinal);
            Assert.Contains("{1}", catalog["TransferImportResultFormat"], StringComparison.Ordinal);
            Assert.Contains("{0}", catalog["TransferImportResultWithWarningsFormat"], StringComparison.Ordinal);
            Assert.Contains("{1}", catalog["TransferImportResultWithWarningsFormat"], StringComparison.Ordinal);
            Assert.Contains("{0}", catalog["TransferExportPhraseRequiredFormat"], StringComparison.Ordinal);
            Assert.Contains("EXPORT PLAINTEXT", catalog["TransferConfirmationPlaceholder"], StringComparison.Ordinal);
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
