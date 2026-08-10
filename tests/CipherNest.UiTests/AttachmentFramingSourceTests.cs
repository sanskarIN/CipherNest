namespace CipherNest.UiTests;

public sealed class AttachmentFramingSourceTests
{
    [Fact]
    public void AttachmentStore_UsesBoundedFilledChunksAndIdentityBinding()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "EncryptedAttachmentStore.cs"));

        Assert.Contains("AttachmentFormatPolicy.ValidateChunkIndex(chunkIndex)", source, StringComparison.Ordinal);
        Assert.Contains("ReadChunkAsync(source, buffer, cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("AttachmentStorageNamePolicy.ValidateForAttachment(attachmentId, opaqueFileName)", source, StringComparison.Ordinal);
        Assert.Contains("if (dataKey.Length != 32)", source, StringComparison.Ordinal);
        Assert.Contains("if (!source.CanRead)", source, StringComparison.Ordinal);
        Assert.Contains("if (!destination.CanWrite)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("source.ReadAsync(buffer.AsMemory(0, buffer.Length)", source, StringComparison.Ordinal);
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
