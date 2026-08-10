namespace CipherNest.UiTests;

public sealed class DatabaseDeletionSourceTests
{
    [Fact]
    public void DeleteDatabase_AttemptsAllManagedSqliteAndRecoveryFiles()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Persistence", "SqliteVaultStore.cs"));
        var start = source.IndexOf("public async Task DeleteDatabaseAsync", StringComparison.Ordinal);
        var end = source.IndexOf("\n    private ", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var method = source[start..end];

        Assert.Contains("TryDeleteManagedFile(DatabasePath, failures)", method, StringComparison.Ordinal);
        Assert.Contains("TryDeleteManagedFile(DatabasePath + \"-wal\", failures)", method, StringComparison.Ordinal);
        Assert.Contains("TryDeleteManagedFile(DatabasePath + \"-shm\", failures)", method, StringComparison.Ordinal);
        Assert.Contains("DeleteRecoveryArtifacts(failures)", method, StringComparison.Ordinal);
        Assert.Contains("if (failures.Count > 0)", method, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteIfExists(DatabasePath);", method, StringComparison.Ordinal);
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
