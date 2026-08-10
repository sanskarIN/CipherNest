using CipherNest.Infrastructure.Persistence;
using CipherNest.Shared;
using Microsoft.Data.Sqlite;

namespace CipherNest.IntegrationTests;

public sealed class DatabaseMigrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestMigrationTests", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public DatabaseMigrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task Initialize_CreatesCurrentSchemaOnce_AndIsIdempotent()
    {
        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();
        await store.InitializeAsync();

        await using var connection = new SqliteConnection($"Data Source={DatabasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Version FROM MigrationHistory ORDER BY Version;";
        await using var reader = await command.ExecuteReaderAsync();
        var versions = new List<int>();
        while (await reader.ReadAsync()) versions.Add(reader.GetInt32(0));

        Assert.Equal([AppConstants.DatabaseSchemaVersion], versions);
    }

    [Fact]
    public async Task Initialize_RejectsDatabaseFromNewerSchema()
    {
        await using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
        {
            await connection.OpenAsync();
            await using var create = connection.CreateCommand();
            create.CommandText = "CREATE TABLE MigrationHistory (Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL); INSERT INTO MigrationHistory(Version, AppliedUtc) VALUES (999, '2026-08-09T00:00:00Z');";
            await create.ExecuteNonQueryAsync();
        }

        var store = new SqliteVaultStore(DatabasePath);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => store.InitializeAsync());
        Assert.Contains("newer", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Initialize_RejectsForgedCurrentHistoryWhenRequiredTablesAreMissing()
    {
        await using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
        {
            await connection.OpenAsync();
            await using var create = connection.CreateCommand();
            create.CommandText = $"CREATE TABLE MigrationHistory (Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL); INSERT INTO MigrationHistory(Version, AppliedUtc) VALUES ({AppConstants.DatabaseSchemaVersion}, '2026-08-10T00:00:00Z');";
            await create.ExecuteNonQueryAsync();
        }

        var store = new SqliteVaultStore(DatabasePath);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => store.InitializeAsync());
        Assert.Contains("schema", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
