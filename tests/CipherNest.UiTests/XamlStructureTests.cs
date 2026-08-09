using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class XamlStructureTests
{
    private static readonly string RepoRoot = FindRepositoryRoot();

    [Theory]
    [InlineData("StartupPage.xaml")]
    [InlineData("OnboardingPage.xaml")]
    [InlineData("UnlockPage.xaml")]
    [InlineData("VaultPage.xaml")]
    [InlineData("ItemEditorPage.xaml")]
    [InlineData("GeneratorPage.xaml")]
    [InlineData("AuditPage.xaml")]
    [InlineData("SettingsPage.xaml")]
    [InlineData("AboutPage.xaml")]
    public void RequiredPage_IsWellFormedXaml(string fileName)
    {
        var path = Path.Combine(RepoRoot, "src", "CipherNest.App", "Views", fileName);
        Assert.True(File.Exists(path), $"Missing required page: {path}");
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        Assert.NotNull(document.Root);
        Assert.Equal("ContentPage", document.Root!.Name.LocalName);
    }

    [Fact]
    public void SecurityCriticalScreens_HaveExpectedCopy()
    {
        var onboarding = File.ReadAllText(Path.Combine(RepoRoot, "src", "CipherNest.App", "Views", "OnboardingPage.xaml"));
        var unlock = File.ReadAllText(Path.Combine(RepoRoot, "src", "CipherNest.App", "Views", "UnlockPage.xaml"));
        Assert.Contains("recovery", onboarding, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("master passphrase", unlock, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("100% secure", onboarding, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unhackable", onboarding, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CipherNest.slnx"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate CipherNest repository root from test output directory.");
    }
}
