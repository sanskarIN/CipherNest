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

    [Theory]
    [InlineData("")]
    [InlineData("-wal")]
    [InlineData("-shm")]
    [InlineData(".previous.test")]
    public async Task CreateConsistentSnapshot_RejectsActiveAndRecoveryDestinations(string suffix)
    {
        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();
        await store.WriteHeaderAsync("{\"marker\":\"active\"}");

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateConsistentSnapshotAsync(DatabasePath + suffix));
        Assert.Equal("{\"marker\":\"active\"}", await store.ReadHeaderAsync());
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
        var replacementHeader = BuildSupportedVaultHeader();
        await replacement.WriteHeaderAsync(replacementHeader);

        await active.ReplaceDatabaseAsync(replacementPath);

        Assert.Equal(replacementHeader, await active.ReadHeaderAsync());
        Assert.Empty(Directory.GetFiles(_directory, "vault.db.previous.*", SearchOption.TopDirectoryOnly));
    }

    private static string BuildSupportedVaultHeader()
    {
        const string wrapper = "{\"version\":1,\"salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"kdf\":{\"memoryKiB\":65536,\"iterations\":3,\"parallelism\":1},\"nonce\":\"AAAAAAAAAAAAAAAA\",\"ciphertext\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=\",\"tag\":\"AAAAAAAAAAAAAAAAAAAAAA==\"}";
        return $"{{\"version\":2,\"master\":{wrapper},\"recovery\":null,\"secondary\":null}}";
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
