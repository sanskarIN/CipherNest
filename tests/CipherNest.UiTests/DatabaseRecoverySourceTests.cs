namespace CipherNest.UiTests;

public sealed class DatabaseRecoverySourceTests
{
    [Fact]
    public void Replacement_StagesAndRestoresDatabaseWalAndShmAsComponents()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Persistence", "SqliteVaultStore.cs"));

        Assert.Contains("StageCurrentFileSet(recovery)", source, StringComparison.Ordinal);
        Assert.Contains("RestoreRecoveryComponent(recovery.DatabasePath, DatabasePath)", source, StringComparison.Ordinal);
        Assert.Contains("RestoreRecoveryComponent(recovery.WalPath, DatabasePath + \"-wal\")", source, StringComparison.Ordinal);
        Assert.Contains("RestoreRecoveryComponent(recovery.ShmPath, DatabasePath + \"-shm\")", source, StringComparison.Ordinal);
        Assert.Contains("if (!File.Exists(recoveryPath)) return;", source, StringComparison.Ordinal);
        Assert.Contains("File.Copy(sourceDatabasePath, DatabasePath, overwrite: false)", source, StringComparison.Ordinal);
        Assert.Contains("ValidateStoredVaultResourceBoundsAsync", source, StringComparison.Ordinal);
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
