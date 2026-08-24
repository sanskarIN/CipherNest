namespace CipherNest.UiTests;

public sealed class ItemEditorNavigationFailureSourceTests
{
    [Fact]
    public void BackClick_ContainsShellNavigationFailure()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml.cs"));

        Assert.Contains("IPrivacySafeExceptionReporter _exceptions", source, StringComparison.Ordinal);
        Assert.Contains("try\n        {\n            await Shell.Current.GoToAsync(\"..\");", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"ItemEditor.Navigate.Back\", ex);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.StackTrace", source, StringComparison.Ordinal);
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
