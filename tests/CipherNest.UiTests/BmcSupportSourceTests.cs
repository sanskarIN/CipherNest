namespace CipherNest.UiTests;

public sealed class BmcSupportSourceTests
{
    [Fact]
    public void BmcBadge_IsCommittedAndReferencedByMauiAndReadme()
    {
        var badgePath = PathAt("src", "CipherNest.App", "Resources", "Images", "bmc_support.svg");
        Assert.True(File.Exists(badgePath));

        var badge = File.ReadAllText(badgePath);
        var project = File.ReadAllText(PathAt("src", "CipherNest.App", "CipherNest.App.csproj"));
        var readme = File.ReadAllText(PathAt("README.md"));

        Assert.Contains("BMC Support CipherNest", badge, StringComparison.Ordinal);
        Assert.Contains("<MauiImage Include=\"Resources/Images/*\" />", project, StringComparison.Ordinal);
        Assert.Contains("src/CipherNest.App/Resources/Images/bmc_support.svg", readme, StringComparison.Ordinal);
        Assert.Contains("https://buymeacoffee.com/sanskarIN", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void AboutPage_HighlightsBmcWithoutBypassingFundingFlag()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AboutPage.xaml"));
        var code = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AboutPage.xaml.cs"));

        Assert.Contains("x:Name=\"SupportDevelopmentFrame\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Source=\"bmc_support.svg\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Tapped=\"OnBuyMeACoffeeClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"☕ Open Buy Me a Coffee\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SupportDevelopmentFrame.IsVisible = BuildFeatureFlags.IsFundingLinkEnabled", code, StringComparison.Ordinal);
        Assert.Contains("if (!BuildFeatureFlags.IsFundingLinkEnabled) return;", code, StringComparison.Ordinal);
        Assert.Contains("AppConstants.BuyMeACoffeeUrl", code, StringComparison.Ordinal);
    }

    [Fact]
    public void PrimarySurfaces_HighlightBmcAndRespectFundingFlag()
    {
        var settings = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "SettingsPage.xaml"));
        var vault = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "VaultPage.xaml"));

        Assert.Contains("Source=\"bmc_support.svg\"", settings, StringComparison.Ordinal);
        Assert.Contains("Text=\"☕ Support CipherNest development\"", settings, StringComparison.Ordinal);
        Assert.Contains("Text=\"☕ View Buy Me a Coffee support\"", settings, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{x:Static services:BuildFeatureFlags.IsFundingLinkEnabled}\"", settings, StringComparison.Ordinal);

        Assert.Contains("Text=\"☕ Support\"", vault, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{x:Static services:BuildFeatureFlags.IsFundingLinkEnabled}\"", vault, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding AboutCommand}\"", vault, StringComparison.Ordinal);
    }

    [Fact]
    public void SupportDocs_KeepFundingVoluntaryAndLinked()
    {
        var support = File.ReadAllText(PathAt("SUPPORT.md"));
        var branding = File.ReadAllText(PathAt("docs", "branding", "ASSETS.md"));

        Assert.Contains("bmc_support.svg", support, StringComparison.Ordinal);
        Assert.Contains("https://buymeacoffee.com/sanskarIN", support, StringComparison.Ordinal);
        Assert.Contains("Financial support is voluntary", support, StringComparison.Ordinal);
        Assert.Contains("not a claim that it is the official Buy Me a Coffee brand logo", branding, StringComparison.Ordinal);
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
