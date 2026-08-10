namespace CipherNest.UiTests;

public sealed class AttachmentStoreSecuritySourceTests
{
    [Fact]
    public void AttachmentEncryption_ZeroesPlaintextBufferAndUsesBestEffortStagingCleanup()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "EncryptedAttachmentStore.cs"));

        Assert.Contains("CryptographicOperations.ZeroMemory(buffer.AsSpan(0, read));", source, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory(buffer);", source, StringComparison.Ordinal);
        Assert.Contains("TryDeleteFile(tempPath);", source, StringComparison.Ordinal);
        Assert.Contains("catch (UnauthorizedAccessException)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("finally\n        {\n            if (File.Exists(tempPath)) File.Delete(tempPath);", source, StringComparison.Ordinal);
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
