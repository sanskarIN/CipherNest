namespace CipherNest.UiTests;

public sealed class ItemEditorClipboardFailureSourceTests
{
    [Fact]
    public void UsernameAndCustomSecretCopy_ReportClipboardFailures()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.Clipboard.cs"));

        Assert.Contains("_exceptions.Report(\"ItemEditor.CopyUsername\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"ItemEditor.CopyCustomSecret\", ex);", source, StringComparison.Ordinal);
        Assert.True(CountOccurrences(source, "catch (Exception ex)") >= 2);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
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
