namespace CipherNest.UiTests;

public sealed class DatabaseReplacementSourceTests
{
    [Fact]
    public void Replacement_ValidatesBeforeMutationAndPreservesOriginalCopyFailure()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Persistence", "SqliteVaultStore.cs"));
        var methodStart = source.IndexOf("public async Task ReplaceDatabaseAsync", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = source.IndexOf("public async Task DeleteDatabaseAsync", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var method = source[methodStart..methodEnd];

        var validate = method.IndexOf("ValidateReplacementDatabaseAsync", StringComparison.Ordinal);
        var sidecars = method.IndexOf("DeleteSidecars();", StringComparison.Ordinal);
        var copy = method.IndexOf("File.Copy(sourceDatabasePath", StringComparison.Ordinal);
        var rollback = method.IndexOf("TryRestorePreviousDatabase", StringComparison.Ordinal);
        Assert.True(validate >= 0 && sidecars > validate);
        Assert.True(copy > sidecars && rollback > copy);
        Assert.Contains("catch\n            {\n                TryRestorePreviousDatabase(backupPath);\n                throw;", method, StringComparison.Ordinal);
        Assert.Contains("PRAGMA quick_check;", source, StringComparison.Ordinal);
        Assert.Contains("DatabaseMigrator.ValidateCurrentSchemaAsync", source, StringComparison.Ordinal);
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
