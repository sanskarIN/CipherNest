namespace CipherNest.UiTests;

public sealed class StartupPreferenceFallbackSourceTests
{
    [Fact]
    public void StartupPreferenceTask_ContainsEachFallbackFailure()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "App.xaml.cs"));

        Assert.Contains("_ = ApplyInitialPreferencesAsync();", source, StringComparison.Ordinal);
        Assert.Contains("ApplyFallbackPreferencesSafely();", source, StringComparison.Ordinal);
        Assert.Contains("Startup.Preferences.FallbackTheme", source, StringComparison.Ordinal);
        Assert.Contains("Startup.Preferences.FallbackLanguage", source, StringComparison.Ordinal);
        Assert.Contains("Startup.Preferences.FallbackAccessibility", source, StringComparison.Ordinal);
        Assert.Contains("catch (Exception themeException)", source, StringComparison.Ordinal);
        Assert.Contains("catch (Exception localizationException)", source, StringComparison.Ordinal);
        Assert.Contains("catch (Exception accessibilityException)", source, StringComparison.Ordinal);
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
