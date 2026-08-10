namespace CipherNest.UiTests;

public sealed class AttachmentStagingSourceTests
{
    [Fact]
    public void EncryptedAttachmentStore_UsesUniqueNonOverwritingStaging()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "EncryptedAttachmentStore.cs"));

        Assert.Contains("MaximumPlaintextBytes = 100L * 1024 * 1024", source, StringComparison.Ordinal);
        Assert.Contains("MinimumContainerBytes = 12", source, StringComparison.Ordinal);
        Assert.Contains("MaximumContainerBytes", source, StringComparison.Ordinal);
        Assert.Contains("Guid.NewGuid():N}.tmp", source, StringComparison.Ordinal);
        Assert.Contains("FileMode.CreateNew", source, StringComparison.Ordinal);
        Assert.Contains("File.Move(tempPath, finalPath, overwrite: false)", source, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory(buffer)", source, StringComparison.Ordinal);
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
