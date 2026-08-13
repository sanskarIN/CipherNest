namespace CipherNest.UiTests;

public sealed class BackupArchiveSourceTests
{
    [Fact]
    public void ExportAndRestore_UseSharedArchiveResourcePolicy()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "EncryptedBackupService.cs"));

        var createArchive = Method(source, "private async Task CreateArchiveAsync", "private static async Task<(long TotalBytes, int EntryCount)> AddBoundedFileAsync");
        var addBounded = Method(source, "private static async Task<(long TotalBytes, int EntryCount)> AddBoundedFileAsync", "private static async Task AddFileAsync");
        var decrypt = Method(source, "private async Task DecryptArchiveAsync", "private static async Task ExtractAndValidateArchiveAsync");
        var extract = Method(source, "private static async Task ExtractAndValidateArchiveAsync", "private async Task ReplaceDatabaseAndAttachmentsAsync");

        Assert.Contains("Directory.GetFiles", createArchive, StringComparison.Ordinal);
        Assert.DoesNotContain("Directory.EnumerateFiles", createArchive, StringComparison.Ordinal);
        Assert.Contains("BackupArchivePolicy.AddEntryLength", addBounded, StringComparison.Ordinal);
        Assert.Contains("BackupArchivePolicy.ValidateEntryCount", addBounded, StringComparison.Ordinal);
        Assert.Contains("BackupArchivePolicy.AddEntryLength", decrypt, StringComparison.Ordinal);
        Assert.Contains("BackupArchivePolicy.ValidateEntryCount", extract, StringComparison.Ordinal);
        Assert.Contains("BackupArchivePolicy.AddEntryLength", extract, StringComparison.Ordinal);
    }

    private static string Method(string source, string signature, string nextSignature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find method: {signature}");
        var end = source.IndexOf(nextSignature, start + signature.Length, StringComparison.Ordinal);
        Assert.True(end > start, $"Could not find method boundary after: {signature}");
        return source[start..end];
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
