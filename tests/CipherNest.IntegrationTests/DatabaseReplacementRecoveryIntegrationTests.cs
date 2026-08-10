using CipherNest.Infrastructure.Persistence;

namespace CipherNest.IntegrationTests;

public sealed class DatabaseReplacementRecoveryIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestDatabaseReplacement", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public DatabaseReplacementRecoveryIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task ReplaceDatabase_RejectsActiveDatabaseAsItsOwnSource()
    {
        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ReplaceDatabaseAsync(DatabasePath));
    }

    [Fact]
    public async Task ReplaceDatabase_RejectsCurrentSchemaWithoutVaultHeader()
    {
        var active = new SqliteVaultStore(DatabasePath);
        await active.InitializeAsync();
        await active.WriteHeaderAsync("{\"marker\":\"active\"}");

        var replacementPath = Path.Combine(_directory, "headerless.db");
        var replacement = new SqliteVaultStore(replacementPath);
        await replacement.InitializeAsync();

        await Assert.ThrowsAsync<InvalidDataException>(() => active.ReplaceDatabaseAsync(replacementPath));
        Assert.Equal("{\"marker\":\"active\"}", await active.ReadHeaderAsync());
    }

    [Fact]
    public async Task ReplaceDatabase_InstallsValidatedDatabaseAndCleansRecoveryArtifacts()
    {
        var active = new SqliteVaultStore(DatabasePath);
        await active.InitializeAsync();
        await active.WriteHeaderAsync("{\"marker\":\"active\"}");

        var replacementPath = Path.Combine(_directory, "replacement.db");
        var replacement = new SqliteVaultStore(replacementPath);
        await replacement.InitializeAsync();
        await replacement.WriteHeaderAsync("{\"marker\":\"replacement\"}");

        await active.ReplaceDatabaseAsync(replacementPath);

        Assert.Equal("{\"marker\":\"replacement\"}", await active.ReadHeaderAsync());
        Assert.Empty(Directory.GetFiles(_directory, "vault.db.previous.*", SearchOption.TopDirectoryOnly));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
