namespace CipherNest.UiTests;

public sealed class BackupRestoreHardeningSourceTests
{
    [Fact]
    public void BackupService_ProtectsExportAndRecoveryBoundaries()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "EncryptedBackupService.cs"));

        Assert.Contains("BackupPathPolicy.ValidateExportDestination", source, StringComparison.Ordinal);
        Assert.Contains("BackupPathPolicy.CreateTemporarySiblingPath", source, StringComparison.Ordinal);
        Assert.Contains("FileMode.CreateNew", source, StringComparison.Ordinal);
        Assert.Contains("HashSet<string>(StringComparer.OrdinalIgnoreCase)", source, StringComparison.Ordinal);
        Assert.Contains("!seenEntries.Add(normalized)", source, StringComparison.Ordinal);
        Assert.Contains("EncryptedAttachmentStore.MinimumContainerBytes", source, StringComparison.Ordinal);
        Assert.Contains("EncryptedAttachmentStore.MaximumContainerBytes", source, StringComparison.Ordinal);
        Assert.Contains("CancellationToken.None", source, StringComparison.Ordinal);
        Assert.Contains(".previous.{Guid.NewGuid():N}", source, StringComparison.Ordinal);
    }

    [Fact]
    public void BackupPathPolicy_BlocksLiveVaultAndAttachmentTargets()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "BackupPathPolicy.cs"));

        Assert.Contains("database + \"-wal\"", source, StringComparison.Ordinal);
        Assert.Contains("database + \"-shm\"", source, StringComparison.Ordinal);
        Assert.Contains("databaseFileName + \".previous\"", source, StringComparison.Ordinal);
        Assert.Contains("AppConstants.AttachmentDirectoryName", source, StringComparison.Ordinal);
        Assert.Contains("Guid.NewGuid():N", source, StringComparison.Ordinal);
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
