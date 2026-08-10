namespace CipherNest.UiTests;

public sealed class CsvSafetySourceTests
{
    [Fact]
    public void CsvTransfer_UsesAggregateAndPreParseRowBounds()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "CsvTransferService.cs"));

        Assert.Contains("private const int MaxRowChars = 2_000_000", source, StringComparison.Ordinal);
        Assert.Contains("if (_rowsRead >= _maxRows)", source, StringComparison.Ordinal);
        Assert.Contains("IncrementRowCharacters(ref rowCharacters)", source, StringComparison.Ordinal);
        Assert.Contains("VaultItemValidator.Validate(item)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", source, StringComparison.Ordinal);
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
