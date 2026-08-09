using CipherNest.Application.Abstractions;
using CipherNest.Shared;
using Microsoft.Data.Sqlite;

namespace CipherNest.Infrastructure.Persistence;

public sealed class SqliteVaultStore : IVaultStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public string DatabasePath { get; }

    public SqliteVaultStore(string databasePath)
    {
        DatabasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath) ?? throw new InvalidOperationException("Database directory is missing."));
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(connection, "PRAGMA journal_mode=WAL;", cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(connection, "PRAGMA synchronous=FULL;", cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(connection, "PRAGMA foreign_keys=ON;", cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(connection, "CREATE TABLE IF NOT EXISTS VaultHeader (Id INTEGER PRIMARY KEY CHECK(Id = 1), HeaderJson TEXT NOT NULL);", cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(connection, "CREATE TABLE IF NOT EXISTS VaultItems (Id TEXT PRIMARY KEY, Envelope BLOB NOT NULL);", cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(connection, "CREATE TABLE IF NOT EXISTS AppSettings (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);", cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(connection, "CREATE TABLE IF NOT EXISTS MigrationHistory (Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL);", cancellationToken).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO MigrationHistory(Version, AppliedUtc) VALUES ($version, $utc);";
            command.Parameters.AddWithValue("$version", AppConstants.DatabaseSchemaVersion);
            command.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> HasVaultAsync(CancellationToken cancellationToken = default) =>
        await ReadHeaderAsync(cancellationToken).ConfigureAwait(false) is not null;

    public async Task<string?> ReadHeaderAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT HeaderJson FROM VaultHeader WHERE Id = 1;";
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result as string;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteHeaderAsync(string headerJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerJson);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO VaultHeader(Id, HeaderJson) VALUES (1, $header) ON CONFLICT(Id) DO UPDATE SET HeaderJson = excluded.HeaderJson;";
            command.Parameters.AddWithValue("$header", headerJson);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<StoredVaultItem>> ReadAllItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<StoredVaultItem>();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Envelope FROM VaultItems ORDER BY Id;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                items.Add(new StoredVaultItem(Guid.Parse(reader.GetString(0)), (byte[])reader[1]));
            }
        }
        finally
        {
            _gate.Release();
        }
        return items;
    }

    public async Task UpsertItemAsync(StoredVaultItem item, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO VaultItems(Id, Envelope) VALUES ($id, $envelope) ON CONFLICT(Id) DO UPDATE SET Envelope = excluded.Envelope;";
            command.Parameters.AddWithValue("$id", item.Id.ToString("D"));
            command.Parameters.Add("$envelope", SqliteType.Blob).Value = item.Envelope;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM VaultItems WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", id.ToString("D"));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task CreateConsistentSnapshotAsync(string destinationDatabasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDatabasePath);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(destinationDatabasePath)) File.Delete(destinationDatabasePath);
            await using var source = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(source, "PRAGMA wal_checkpoint(FULL);", cancellationToken).ConfigureAwait(false);
            await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = destinationDatabasePath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Private
            }.ToString());
            await destination.OpenAsync(cancellationToken).ConfigureAwait(false);
            await Task.Run(() => source.BackupDatabase(destination), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceDatabaseAsync(string sourceDatabasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabasePath);
        if (!File.Exists(sourceDatabasePath)) throw new FileNotFoundException("Restore database was not found.", sourceDatabasePath);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var suffix in new[] { "-wal", "-shm" })
            {
                var sidecar = DatabasePath + suffix;
                if (File.Exists(sidecar)) File.Delete(sidecar);
            }
            var backupPath = DatabasePath + ".previous";
            if (File.Exists(backupPath)) File.Delete(backupPath);
            if (File.Exists(DatabasePath)) File.Move(DatabasePath, backupPath);
            try
            {
                File.Copy(sourceDatabasePath, DatabasePath, overwrite: true);
            }
            catch
            {
                if (File.Exists(backupPath)) File.Move(backupPath, DatabasePath, overwrite: true);
                throw;
            }
            if (File.Exists(backupPath)) File.Delete(backupPath);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private
        }.ToString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
