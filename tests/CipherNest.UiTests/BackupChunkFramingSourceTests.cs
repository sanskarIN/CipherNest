namespace CipherNest.UiTests;

public sealed class BackupChunkFramingSourceTests
{
    [Fact]
    public void BackupService_BoundsChunkCountAndFillsExportChunks()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "EncryptedBackupService.cs"));

        Assert.Contains("BackupFormatPolicy.ValidateChunkIndex(index)", source, StringComparison.Ordinal);
        Assert.Contains("ReadChunkAsync(input, buffer, cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("private static async Task<int> ReadChunkAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)", source, StringComparison.Ordinal);
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
