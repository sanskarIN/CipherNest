namespace CipherNest.UiTests;

public sealed class TransferCsvFailureStateSourceTests
{
    [Fact]
    public void CsvSelectionFailure_ResetsStaleMappingsAndImportCatchesAllFaults()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TransferViewModel.cs"));

        Assert.Contains("ResetCsvSelection();", source, StringComparison.Ordinal);
        Assert.Contains("private void ResetCsvSelection()", source, StringComparison.Ordinal);
        Assert.Contains("SelectedFileName = Text(\"TransferNoCsvSelected\");", source, StringComparison.Ordinal);
        Assert.Contains("TitleColumn = null;", source, StringComparison.Ordinal);
        Assert.Contains("TypeColumn = null;", source, StringComparison.Ordinal);

        var importStart = source.IndexOf("private async Task ImportAsync()", StringComparison.Ordinal);
        var exportStart = source.IndexOf("private async Task ExportPlaintextAsync()", importStart, StringComparison.Ordinal);
        var importMethod = source[importStart..exportStart];
        Assert.Contains("catch (Exception ex)", importMethod, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Transfer.ImportCsv\", ex);", importMethod, StringComparison.Ordinal);
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
