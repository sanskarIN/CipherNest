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
        Assert.Contains("migration history", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, "2026-08-10T00:00:00Z")]
    [InlineData(-1, "2026-08-10T00:00:00Z")]
    [InlineData(1, "not-a-timestamp")]
    public async Task Initialize_RejectsInvalidMigrationHistory(int version, string appliedUtc)
    {
        await using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
        {
            await connection.OpenAsync();
            await using var create = connection.CreateCommand();
            create.CommandText = "CREATE TABLE MigrationHistory (Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL); INSERT INTO MigrationHistory(Version, AppliedUtc) VALUES ($version, $utc);";
            create.Parameters.AddWithValue("$version", version);
            create.Parameters.AddWithValue("$utc", appliedUtc);
            await create.ExecuteNonQueryAsync();
        }

        var store = new SqliteVaultStore(DatabasePath);
        await Assert.ThrowsAsync<InvalidDataException>(() => store.InitializeAsync());
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

    [Fact]
    public async Task ReplaceDatabase_RejectsInvalidSchemaBeforeTouchingActiveDatabase()
    {
        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();
        await store.WriteHeaderAsync("{\"marker\":\"active\"}");

        var replacementPath = Path.Combine(_directory, "invalid-replacement.db");
        await using (var replacement = new SqliteConnection($"Data Source={replacementPath}"))
        {
            await replacement.OpenAsync();
            await using var create = replacement.CreateCommand();
            create.CommandText = $"CREATE TABLE MigrationHistory (Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL); INSERT INTO MigrationHistory(Version, AppliedUtc) VALUES ({AppConstants.DatabaseSchemaVersion}, '2026-08-10T00:00:00Z');";
            await create.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<InvalidDataException>(() => store.ReplaceDatabaseAsync(replacementPath));

        Assert.Equal("{\"marker\":\"active\"}", await store.ReadHeaderAsync());
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
