namespace CipherNest.UiTests;

public sealed class DatabaseReplacementSourceTests
{
    [Fact]
    public void Replacement_ValidatesBeforeMutationAndPreservesOriginalCopyFailure()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Persistence", "SqliteVaultStore.cs"));
        var method = Method(source, "public async Task ReplaceDatabaseAsync", "public async Task DeleteDatabaseAsync");

        var validate = method.IndexOf("ValidateReplacementDatabaseAsync", StringComparison.Ordinal);
        var createRecovery = method.IndexOf("CreateRecoveryFileSet()", StringComparison.Ordinal);
        var stage = method.IndexOf("StageCurrentFileSet(recovery)", StringComparison.Ordinal);
        var copy = method.IndexOf("File.Copy(sourceDatabasePath", StringComparison.Ordinal);
        var rollback = method.IndexOf("TryRestoreRecoveryFileSet(recovery)", StringComparison.Ordinal);
        var cleanup = method.IndexOf("TryDeleteRecoveryFileSet(recovery)", StringComparison.Ordinal);

        Assert.True(validate >= 0 && createRecovery > validate);
        Assert.True(stage > createRecovery && copy > stage);
        Assert.True(rollback > copy && cleanup > rollback);
        Assert.Contains("catch\n            {\n                TryRestoreRecoveryFileSet(recovery);\n                throw;", method, StringComparison.Ordinal);
        Assert.Contains("PRAGMA quick_check;", source, StringComparison.Ordinal);
        Assert.Contains("DatabaseMigrator.ValidateCurrentSchemaAsync", source, StringComparison.Ordinal);
        Assert.Contains("ValidateStoredVaultResourceBoundsAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteDatabase_AttemptsCompleteManagedFileSetBeforeReportingFailures()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Persistence", "SqliteVaultStore.cs"));
        var method = Method(source, "public async Task DeleteDatabaseAsync", "private void ValidateSnapshotDestination");

        var database = method.IndexOf("TryDeleteManagedFile(DatabasePath, failures);", StringComparison.Ordinal);
        var wal = method.IndexOf("TryDeleteManagedFile(DatabasePath + \"-wal\", failures);", StringComparison.Ordinal);
        var shm = method.IndexOf("TryDeleteManagedFile(DatabasePath + \"-shm\", failures);", StringComparison.Ordinal);
        var legacyRecovery = method.IndexOf("TryDeleteManagedFile(DatabasePath + \".previous\", failures);", StringComparison.Ordinal);
        var generatedRecovery = method.IndexOf("DeleteRecoveryArtifacts(failures);", StringComparison.Ordinal);
        var report = method.IndexOf("if (failures.Count > 0)", StringComparison.Ordinal);

        Assert.True(database >= 0 && wal > database && shm > wal && legacyRecovery > shm);
        Assert.True(generatedRecovery > legacyRecovery && report > generatedRecovery);
        Assert.Contains("new AggregateException(failures)", method, StringComparison.Ordinal);
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
