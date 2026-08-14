namespace CipherNest.UiTests;

public sealed class ItemEditorSaveFailureContainmentSourceTests
{
    [Fact]
    public void SaveCommand_ReportsUnexpectedFailuresAfterValidationHandling()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.cs"));
        var saveStart = source.IndexOf("private async Task SaveAsync()", StringComparison.Ordinal);
        var addAttachmentStart = source.IndexOf("private async Task AddAttachmentAsync()", saveStart, StringComparison.Ordinal);
        var method = source[saveStart..addAttachmentStart];

        Assert.Contains("catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException)", method, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"ItemEditor.Save\", ex);", method, StringComparison.Ordinal);
        Assert.Contains("The item could not be saved safely.", method, StringComparison.Ordinal);
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
