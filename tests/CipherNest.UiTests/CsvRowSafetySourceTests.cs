namespace CipherNest.UiTests;

public sealed class CsvRowSafetySourceTests
{
    [Fact]
    public void Import_BoundsMappedTagMaterializationBeforeVaultItemConstruction()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "CsvTransferService.cs"));
        var validator = File.ReadAllText(PathAt("src", "CipherNest.Application", "Validation", "VaultItemValidator.cs"));

        var splitStart = source.IndexOf("private static bool TrySplitTags", StringComparison.Ordinal);
        var parseTypeStart = source.IndexOf("private static VaultItemType ParseType", splitStart, StringComparison.Ordinal);
        Assert.True(splitStart >= 0 && parseTypeStart > splitStart, "Could not locate bounded CSV tag parser.");
        var splitMethod = source[splitStart..parseTypeStart];

        Assert.Contains("value.AsSpan", splitMethod, StringComparison.Ordinal);
        Assert.Contains("VaultItemValidator.MaximumTags", splitMethod, StringComparison.Ordinal);
        Assert.Contains("VaultItemValidator.MaximumTagCharacters", splitMethod, StringComparison.Ordinal);
        Assert.Contains("parsed.Count >= VaultItemValidator.MaximumTags", splitMethod, StringComparison.Ordinal);
        Assert.DoesNotContain(".Split(", splitMethod, StringComparison.Ordinal);

        var tagParseCall = source.IndexOf("TrySplitTags(Get(row, indexes, mapping.Tags)", StringComparison.Ordinal);
        var itemConstruction = source.IndexOf("var item = new VaultItem", StringComparison.Ordinal);
        Assert.True(tagParseCall >= 0 && tagParseCall < itemConstruction, "Mapped tags must be bounded before VaultItem construction.");

        Assert.Contains("public const int MaximumTags = 100;", validator, StringComparison.Ordinal);
        Assert.Contains("public const int MaximumTagCharacters = 128;", validator, StringComparison.Ordinal);
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
