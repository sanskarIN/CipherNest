using CipherNest.Application.Abstractions;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Shared;
using Microsoft.Data.Sqlite;

namespace CipherNest.IntegrationTests;

public sealed class VaultStorageBoundsIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestStorageBounds", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public VaultStorageBoundsIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task WriteHeader_RejectsOversizedUtf8Metadata()
    {
        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();
        var oversized = new string('x', VaultStorageLimits.MaximumVaultHeaderUtf8Bytes + 1);

        await Assert.ThrowsAsync<ArgumentException>(() => store.WriteHeaderAsync(oversized));
    }

    [Fact]
    public async Task ReadHeader_RejectsOversizedPersistedMetadata()
    {
        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();
        var oversized = new string('x', VaultStorageLimits.MaximumVaultHeaderUtf8Bytes + 1);

        await using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO VaultHeader(Id, HeaderJson) VALUES (1, $header) ON CONFLICT(Id) DO UPDATE SET HeaderJson = excluded.HeaderJson;";
            command.Parameters.AddWithValue("$header", oversized);
            await command.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadHeaderAsync());
    }

    [Fact]
    public async Task UpsertItem_RejectsInvalidIdentifierAndEmptyEnvelope()
    {
        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => store.UpsertItemAsync(new StoredVaultItem(Guid.Empty, [1])));
        await Assert.ThrowsAsync<ArgumentException>(() => store.UpsertItemAsync(new StoredVaultItem(Guid.NewGuid(), [])));
    }

    [Fact]
    public async Task ReadAllItems_RejectsNonCanonicalStoredIdentifierBeforeReturningRows()
    {
        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();
        var id = Guid.NewGuid();

        await using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO VaultItems(Id, Envelope) VALUES ($id, $envelope);";
            command.Parameters.AddWithValue("$id", id.ToString("D").ToUpperInvariant());
            command.Parameters.AddWithValue("$envelope", new byte[] { 1 });
            await command.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReadAllItemsAsync());
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
