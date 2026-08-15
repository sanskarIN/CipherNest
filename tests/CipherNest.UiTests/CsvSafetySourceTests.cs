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

    [Fact]
    public void CsvTransfer_UsesStrictUtf8AndOneReadBoundary()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "CsvTransferService.cs"));

        Assert.Contains("throwOnInvalidBytes: true", source, StringComparison.Ordinal);
        Assert.Contains("detectEncodingFromByteOrderMarks: false", source, StringComparison.Ordinal);
        Assert.Contains("if (value == '\\uFEFF') continue;", source, StringComparison.Ordinal);
        Assert.Contains("private int? _pendingChar", source, StringComparison.Ordinal);
        Assert.Contains("ConsumeOptionalLineFeedAsync", source, StringComparison.Ordinal);
        Assert.Contains("CSV contains invalid UTF-8 text.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_reader.Peek()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_reader.Read()", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvTransfer_BoundsAndSanitizesHeaderNamesBeforeMapping()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "CsvTransferService.cs"));

        Assert.Contains("private const int MaxHeaderNameChars = 256", source, StringComparison.Ordinal);
        Assert.Contains("ReadRowAsync(cancellationToken, MaxHeaderNameChars, OversizedHeaderMessage)", source, StringComparison.Ordinal);
        Assert.Contains("if (field.Length > maxFieldChars)", source, StringComparison.Ordinal);
        Assert.Contains("h.Length > MaxHeaderNameChars", source, StringComparison.Ordinal);
        Assert.Contains("value.EnumerateRunes()", source, StringComparison.Ordinal);
        Assert.Contains("Rune.GetUnicodeCategory(rune)", source, StringComparison.Ordinal);
        Assert.Contains("UnicodeCategory.Control or UnicodeCategory.Format", source, StringComparison.Ordinal);
        Assert.Contains("CSV header contains an unsafe control or formatting character.", source, StringComparison.Ordinal);
        Assert.Contains("CSV header contains an oversized column name.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("char.GetUnicodeCategory(ch)", source, StringComparison.Ordinal);
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
