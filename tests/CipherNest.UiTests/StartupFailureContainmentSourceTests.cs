namespace CipherNest.UiTests;

public sealed class StartupFailureContainmentSourceTests
{
    [Fact]
    public void StartupPage_RestoresRetryStateBeforeShowingFailureUi()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "StartupPage.xaml.cs"));

        var resetIndex = source.IndexOf("_navigated = false;", StringComparison.Ordinal);
        var alertIndex = source.IndexOf("await DisplayAlertAsync(", StringComparison.Ordinal);

        Assert.True(resetIndex >= 0, "Startup failure path must restore retry state.");
        Assert.True(alertIndex > resetIndex, "Startup retry state must be restored before the secondary failure alert can throw.");
    }

    [Fact]
    public void StartupPage_ContainsSecondaryAlertAndReporterFailures()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "StartupPage.xaml.cs"));

        Assert.Contains("ReportSafely(\"Startup.Initialize\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("ReportSafely(\"Startup.Initialize.Alert\", alertException);", source, StringComparison.Ordinal);
        Assert.Contains("private static void ReportSafely", source, StringComparison.Ordinal);
        Assert.Contains("catch\n        {", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", source, StringComparison.Ordinal);
        Assert.DoesNotContain("alertException.Message", source, StringComparison.Ordinal);
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
