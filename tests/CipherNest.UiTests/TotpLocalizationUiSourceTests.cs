namespace CipherNest.UiTests;

public sealed class TotpLocalizationUiSourceTests
{
    [Fact]
    public void ItemEditor_TotpSecuritySurfaceUsesLocalizationResources()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml"));

        Assert.Contains("xmlns:l10n=\"clr-namespace:CipherNest.App.Localization\"", xaml, StringComparison.Ordinal);

        foreach (var key in new[]
        {
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
        })
        {
            Assert.Contains($"{{l10n:Translate {key}}}", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ItemEditor_DoesNotHardCodeSensitiveTotpUriActionsAndWarnings()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml"));

        Assert.DoesNotContain("Text=\"Import a TOTP setup URI\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Import URI\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Copy setup URI\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SemanticProperties.Description=\"Sensitive TOTP setup URI to import locally\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Only import TOTP seeds you are authorized to use.", xaml, StringComparison.Ordinal);
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
