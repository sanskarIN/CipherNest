namespace CipherNest.UiTests;

public sealed class LifecycleFailClosedSourceTests
{
    [Fact]
    public void LifecycleFallback_SeparatelyReportsLockAndClipboardCleanupFailures()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "App.xaml.cs"));

        Assert.Contains("FailClosedLockAndClearClipboardAsync", source, StringComparison.Ordinal);
        Assert.Contains("$\"{operation}.Lock\"", source, StringComparison.Ordinal);
        Assert.Contains("$\"{operation}.Clipboard\"", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Debug.WriteLine", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.WriteLine", source, StringComparison.Ordinal);
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
