namespace CipherNest.UiTests;

public sealed class TotpLocalizedStatusSourceTests
{
    [Fact]
    public void TotpViewModel_UsesLocalizedDynamicTextAndOperationStatuses()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.Totp.cs"));
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml"));

        Assert.Contains("public string TotpPeriodText", source, StringComparison.Ordinal);
        Assert.Contains("TotpText(\"TotpPeriodFormat\")", source, StringComparison.Ordinal);
        Assert.Contains("public string TotpValidityText", source, StringComparison.Ordinal);
        Assert.Contains("TotpText(\"TotpValidityFormat\")", source, StringComparison.Ordinal);
        Assert.Contains("CultureInfo.CurrentUICulture", source, StringComparison.Ordinal);
        Assert.Contains("OnPropertyChanged(nameof(TotpPeriodText))", source, StringComparison.Ordinal);
        Assert.Contains("OnPropertyChanged(nameof(TotpValidityText))", source, StringComparison.Ordinal);

        foreach (var key in new[]
        {
            "TotpInvalidSeedSettingsError",
            "TotpGenerateError",
            "TotpCopyCodeError",
            "TotpImportMissingUriError",
            "TotpImportSuccess",
            "TotpImportInvalidError",
            "TotpImportFailureError",
            "TotpCopyUriSuccess",
            "TotpCopyUriInvalidError",
            "TotpCopyUriFailureError"
        })
        {
            Assert.Contains($"TotpText(\"{key}\")", source, StringComparison.Ordinal);
        }

        Assert.Contains("Text=\"{Binding TotpPeriodText}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding TotpValidityText}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("StringFormat='Period: {0} seconds'", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("StringFormat='Valid for about {0} more seconds after refresh.'", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void TotpViewModel_DoesNotHardCodeOperationMessages()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.Totp.cs"));

        Assert.DoesNotContain("The TOTP seed or settings are invalid.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TOTP setup URI imported locally.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("The TOTP setup URI is invalid or unsupported.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("TOTP setup URI copied with timed clipboard cleanup", source, StringComparison.Ordinal);
        Assert.DoesNotContain("The TOTP setup URI could not be copied safely.", source, StringComparison.Ordinal);
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
