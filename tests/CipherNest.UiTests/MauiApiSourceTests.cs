namespace CipherNest.UiTests;

public sealed class MauiApiSourceTests
{
    [Fact]
    public void AppSource_DoesNotUseLegacyDisplayAlertApi()
    {
        var appRoot = PathAt("src", "CipherNest.App");
        foreach (var path in Directory.EnumerateFiles(appRoot, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            Assert.DoesNotContain(".DisplayAlert(", source, StringComparison.Ordinal);
        }
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
