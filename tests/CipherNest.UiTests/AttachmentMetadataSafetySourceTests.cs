namespace CipherNest.UiTests;

public sealed class AttachmentMetadataSafetySourceTests
{
    [Fact]
    public void AttachmentImportPolicy_UsesRuneAwareControlAndFormatValidation()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Application", "Validation", "AttachmentImportPolicy.cs"));

        Assert.Contains("Rune.DecodeFromUtf16", source, StringComparison.Ordinal);
        Assert.Contains("OperationStatus.Done", source, StringComparison.Ordinal);
        Assert.Contains("Rune.GetUnicodeCategory(rune)", source, StringComparison.Ordinal);
        Assert.Contains("UnicodeCategory.Control or UnicodeCategory.Format", source, StringComparison.Ordinal);
        Assert.Contains("IsValidStoredDisplayName", source, StringComparison.Ordinal);
        Assert.Contains("IsValidStoredMediaType", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultItemValidator_ReusesCanonicalAttachmentMetadataPolicy()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Application", "Validation", "VaultItemValidator.cs"));

        Assert.Contains("AttachmentImportPolicy.IsValidStoredDisplayName(attachment.DisplayName)", source, StringComparison.Ordinal);
        Assert.Contains("AttachmentImportPolicy.IsValidStoredMediaType(attachment.MediaType)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("attachment.DisplayName.Any(char.IsControl)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("attachment.MediaType.Any(char.IsControl)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AttachmentStorageNamePolicy_BoundsLengthBeforeStemParsing()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "AttachmentStorageNamePolicy.cs"));

        var lengthCheck = source.IndexOf("opaqueFileName.Length != OpaqueFileNameCharacters", StringComparison.Ordinal);
        var spanSlice = source.IndexOf("opaqueFileName.AsSpan(0, 32)", StringComparison.Ordinal);
        Assert.True(lengthCheck >= 0);
        Assert.True(spanSlice > lengthCheck);
        Assert.Contains("public const int OpaqueFileNameCharacters = 36", source, StringComparison.Ordinal);
        Assert.DoesNotContain("opaqueFileName[..^4]", source, StringComparison.Ordinal);
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
