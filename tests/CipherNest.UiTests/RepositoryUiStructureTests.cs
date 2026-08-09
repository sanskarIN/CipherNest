namespace CipherNest.UiTests;

public sealed class RepositoryUiStructureTests
{
    [Fact]
    public void Shell_ContainsCoreSecurityRoutes()
    {
        var shell = File.ReadAllText(PathAt("src", "CipherNest.App", "AppShell.xaml"));
        Assert.Contains("Route=\"unlock\"", shell, StringComparison.Ordinal);
        Assert.Contains("Route=\"vault\"", shell, StringComparison.Ordinal);
        Assert.Contains("Route=\"settings\"", shell, StringComparison.Ordinal);
        Assert.Contains("Route=\"security-info\"", shell, StringComparison.Ordinal);
    }

    [Fact]
    public void SensitiveScreens_ExposeSemanticLiveOrDescriptionMetadata()
    {
        var unlock = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "UnlockPage.xaml"));
        var vault = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "VaultPage.xaml"));
        var item = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml"));

        Assert.Contains("SemanticProperties.Description", unlock, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.LiveSetting", vault, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.LiveSetting", item, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalizationCatalog_AndLanguageSettingExist()
    {
        var resx = File.ReadAllText(PathAt("src", "CipherNest.App", "Resources", "Localization", "AppStrings.resx"));
        var settings = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "SettingsPage.xaml"));

        Assert.Contains("ProductName", resx, StringComparison.Ordinal);
        Assert.Contains("SelectedLanguage", settings, StringComparison.Ordinal);
        Assert.Contains("SaveLanguageCommand", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultActions_WrapInsteadOfForcingHorizontalNavigationBar()
    {
        var vault = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "VaultPage.xaml"));
        Assert.Contains("<FlexLayout", vault, StringComparison.Ordinal);
        Assert.Contains("Wrap=\"Wrap\"", vault, StringComparison.Ordinal);
    }

    [Fact]
    public void ExceptionReporter_DoesNotLogExceptionObjectMessageOrStack()
    {
        var reporter = File.ReadAllText(PathAt("src", "CipherNest.App", "Services", "PrivacySafeExceptionReporter.cs"));
        Assert.DoesNotContain("exception.Message", reporter, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.StackTrace", reporter, StringComparison.Ordinal);
        Assert.DoesNotContain("logger.LogError(NonFatalEvent, exception", reporter, StringComparison.Ordinal);
        Assert.Contains("HResult", reporter, StringComparison.Ordinal);
    }

    [Fact]
    public void AboutSurface_ReferencesLegalPrivacyAndThirdPartyNotices()
    {
        var about = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AboutPage.xaml"));
        Assert.Contains("GPL-3.0-or-later", about, StringComparison.Ordinal);
        Assert.Contains("PRIVACY.md", about, StringComparison.Ordinal);
        Assert.Contains("TERMS.md", about, StringComparison.Ordinal);
        Assert.Contains("THIRD_PARTY_NOTICES.md", about, StringComparison.Ordinal);
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
