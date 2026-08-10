using CipherNest.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace CipherNest.Infrastructure.Persistence;

public sealed class SqliteVaultStore : IVaultStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public string DatabasePath { get; }

    public SqliteVaultStore(string databasePath) => DatabasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));

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
            await DatabaseMigrator.ApplyAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> HasVaultAsync(CancellationToken cancellationToken = default) => await ReadHeaderAsync(cancellationToken).ConfigureAwait(false) is not null;

    public async Task<string?> ReadHeaderAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(DatabasePath)) return null;
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT HeaderJson FROM VaultHeader WHERE Id = 1;";
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        }
        finally { _gate.Release(); }
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
        finally { _gate.Release(); }
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
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) items.Add(new StoredVaultItem(Guid.Parse(reader.GetString(0)), (byte[])reader[1]));
        }
        finally { _gate.Release(); }
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
        finally { _gate.Release(); }
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
        finally { _gate.Release(); }
    }

    public async Task CreateConsistentSnapshotAsync(string destinationDatabasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDatabasePath);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(DatabasePath)) throw new InvalidOperationException("No vault database exists.");
            if (File.Exists(destinationDatabasePath)) File.Delete(destinationDatabasePath);
            await using var source = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await ExecuteAsync(source, "PRAGMA wal_checkpoint(FULL);", cancellationToken).ConfigureAwait(false);
            await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destinationDatabasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Private }.ToString());
            await destination.OpenAsync(cancellationToken).ConfigureAwait(false);
            await Task.Run(() => source.BackupDatabase(destination), cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task ReplaceDatabaseAsync(string sourceDatabasePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabasePath);
        if (!File.Exists(sourceDatabasePath)) throw new FileNotFoundException("Restore database was not found.", sourceDatabasePath);
        if (PathsEqual(sourceDatabasePath, DatabasePath)) throw new InvalidOperationException("Replacement database must be staged separately from the active vault database.");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ValidateReplacementDatabaseAsync(sourceDatabasePath, cancellationToken).ConfigureAwait(false);
            var recovery = CreateRecoveryFileSet();
            try
            {
                StageCurrentFileSet(recovery);
                File.Copy(sourceDatabasePath, DatabasePath, overwrite: false);
            }
            catch
            {
                TryRestoreRecoveryFileSet(recovery);
                throw;
            }
            TryDeleteRecoveryFileSet(recovery);
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            DeleteIfExists(DatabasePath + "-wal");
            DeleteIfExists(DatabasePath + "-shm");
            DeleteIfExists(DatabasePath);
            DeleteIfExists(DatabasePath + ".previous");
            DeleteRecoveryArtifacts();
        }
        finally { _gate.Release(); }
    }

    private static async Task ValidateReplacementDatabaseAsync(string sourceDatabasePath, CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = sourceDatabasePath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Private
            }.ToString();
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using (var quickCheck = connection.CreateCommand())
            {
                quickCheck.CommandText = "PRAGMA quick_check;";
                var result = await quickCheck.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
                if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Replacement vault database failed SQLite integrity validation.");
            }

            await DatabaseMigrator.ValidateCurrentSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            throw new InvalidDataException("Replacement vault database is not a valid supported CipherNest database.", ex);
        }
    }

    private RecoveryFileSet CreateRecoveryFileSet()
    {
        var basePath = DatabasePath + $".previous.{Guid.NewGuid():N}";
        return new RecoveryFileSet(basePath, basePath + "-wal", basePath + "-shm");
    }

    private void StageCurrentFileSet(RecoveryFileSet recovery)
    {
        MoveIfExists(DatabasePath, recovery.DatabasePath);
        MoveIfExists(DatabasePath + "-wal", recovery.WalPath);
        MoveIfExists(DatabasePath + "-shm", recovery.ShmPath);
    }

    private void TryRestoreRecoveryFileSet(RecoveryFileSet recovery)
    {
        try
        {
            TryDeleteFile(DatabasePath + "-wal");
            TryDeleteFile(DatabasePath + "-shm");
            TryDeleteFile(DatabasePath);
            MoveIfExists(recovery.DatabasePath, DatabasePath, overwrite: true);
            MoveIfExists(recovery.WalPath, DatabasePath + "-wal", overwrite: true);
            MoveIfExists(recovery.ShmPath, DatabasePath + "-shm", overwrite: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryDeleteRecoveryFileSet(RecoveryFileSet recovery)
    {
        TryDeleteFile(recovery.DatabasePath);
        TryDeleteFile(recovery.WalPath);
        TryDeleteFile(recovery.ShmPath);
    }

    private void DeleteRecoveryArtifacts()
    {
        var directory = Path.GetDirectoryName(DatabasePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;
        var pattern = Path.GetFileName(DatabasePath) + ".previous.*";
        string[] files;
        try { files = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly); }
        catch (IOException) { return; }
        catch (UnauthorizedAccessException) { return; }
        foreach (var file in files) TryDeleteFile(file);
    }

    private static void MoveIfExists(string source, string destination, bool overwrite = false)
    {
        if (File.Exists(source)) File.Move(source, destination, overwrite);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static void TryDeleteFile(string path)
    {
        try { DeleteIfExists(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static bool PathsEqual(string left, string right)
    {
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), comparison);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = DatabasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Private }.ToString());
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(); command.CommandText = sql; await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed record RecoveryFileSet(string DatabasePath, string WalPath, string ShmPath);
}
