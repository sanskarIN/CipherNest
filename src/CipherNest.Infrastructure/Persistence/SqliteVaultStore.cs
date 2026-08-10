using System.Text;
using CipherNest.Application.Abstractions;
using CipherNest.Shared;
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
            command.CommandText = "SELECT length(CAST(HeaderJson AS BLOB)), HeaderJson FROM VaultHeader WHERE Id = 1;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;
            var byteLength = reader.GetInt64(0);
            if (byteLength is < 1 or > VaultStorageLimits.MaximumVaultHeaderUtf8Bytes) throw new InvalidDataException("Vault header exceeds the supported size limit.");
            var headerJson = reader.GetString(1);
            if (Encoding.UTF8.GetByteCount(headerJson) != byteLength) throw new InvalidDataException("Vault header length is inconsistent.");
            return headerJson;
        }
        finally { _gate.Release(); }
    }

    public async Task WriteHeaderAsync(string headerJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerJson);
        if (Encoding.UTF8.GetByteCount(headerJson) > VaultStorageLimits.MaximumVaultHeaderUtf8Bytes)
            throw new ArgumentException("Vault header exceeds the supported size limit.", nameof(headerJson));
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
            await ValidateStoredItemSetBoundsAsync(connection, cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Envelope, length(Envelope) FROM VaultItems ORDER BY Id;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var idText = reader.GetString(0);
                if (!TryParseCanonicalItemId(idText, out var id)) throw new InvalidDataException("Stored vault item identifier is invalid or non-canonical.");
                var envelopeLength = reader.GetInt64(2);
                if (envelopeLength is < 1 or > VaultStorageLimits.MaximumStoredEnvelopeBytes) throw new InvalidDataException("Stored vault item envelope exceeds the supported size limit.");
                var envelope = (byte[])reader[1];
                if (envelope.LongLength != envelopeLength) throw new InvalidDataException("Stored vault item envelope length is inconsistent.");
                items.Add(new StoredVaultItem(id, envelope));
            }
        }
        finally { _gate.Release(); }
        return items;
    }

    public async Task UpsertItemAsync(StoredVaultItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Id == Guid.Empty) throw new ArgumentException("Stored vault item identifier is invalid.", nameof(item));
        if (item.Envelope is null || item.Envelope.Length is < 1 or > VaultStorageLimits.MaximumStoredEnvelopeBytes)
            throw new ArgumentException("Stored vault item envelope exceeds the supported size limit.", nameof(item));

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await ValidateUpsertBoundsAsync(connection, item.Id, item.Envelope.LongLength, cancellationToken).ConfigureAwait(false);
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
        var destinationPath = Path.GetFullPath(destinationDatabasePath);
        ValidateSnapshotDestination(destinationPath);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(DatabasePath)) throw new InvalidOperationException("No vault database exists.");
            if (File.Exists(destinationPath)) throw new IOException("Snapshot destination already exists.");
            try
            {
                await using var source = await OpenAsync(cancellationToken).ConfigureAwait(false);
                await ExecuteAsync(source, "PRAGMA wal_checkpoint(FULL);", cancellationToken).ConfigureAwait(false);
                await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destinationPath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Private }.ToString());
                await destination.OpenAsync(cancellationToken).ConfigureAwait(false);
                await Task.Run(() => source.BackupDatabase(destination), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                TryDeleteFile(destinationPath);
                throw;
            }
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
            var failures = new List<Exception>();
            TryDeleteManagedFile(DatabasePath, failures);
            TryDeleteManagedFile(DatabasePath + "-wal", failures);
            TryDeleteManagedFile(DatabasePath + "-shm", failures);
            TryDeleteManagedFile(DatabasePath + ".previous", failures);
            DeleteRecoveryArtifacts(failures);
            if (failures.Count > 0)
                throw new IOException("One or more CipherNest database files could not be deleted.", new AggregateException(failures));
        }
        finally { _gate.Release(); }
    }

    private void ValidateSnapshotDestination(string destinationPath)
    {
        if (PathsEqual(destinationPath, DatabasePath) ||
            PathsEqual(destinationPath, DatabasePath + "-wal") ||
            PathsEqual(destinationPath, DatabasePath + "-shm"))
            throw new InvalidOperationException("Snapshot destination cannot replace the active SQLite file set.");

        var databaseDirectory = Path.GetDirectoryName(Path.GetFullPath(DatabasePath)) ?? throw new InvalidOperationException("Database directory is missing.");
        var destinationDirectory = Path.GetDirectoryName(destinationPath) ?? throw new InvalidOperationException("Snapshot destination directory is missing.");
        var comparison = GetPathComparison();
        if (string.Equals(databaseDirectory, destinationDirectory, comparison) &&
            Path.GetFileName(destinationPath).StartsWith(Path.GetFileName(DatabasePath) + ".previous", comparison))
            throw new InvalidOperationException("Snapshot destination cannot replace CipherNest database recovery files.");
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
            await ValidateStoredVaultResourceBoundsAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            throw new InvalidDataException("Replacement vault database is not a valid supported CipherNest database.", ex);
        }
    }

    private static async Task ValidateStoredVaultResourceBoundsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using (var header = connection.CreateCommand())
        {
            header.CommandText = "SELECT length(CAST(HeaderJson AS BLOB)) FROM VaultHeader WHERE Id = 1;";
            var result = await header.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull) throw new InvalidDataException("Replacement vault database does not contain a vault header.");
            var headerBytes = Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
            if (headerBytes is < 1 or > VaultStorageLimits.MaximumVaultHeaderUtf8Bytes) throw new InvalidDataException("Replacement vault header exceeds the supported size limit.");
        }

        await ValidateStoredItemSetBoundsAsync(connection, cancellationToken).ConfigureAwait(false);
        await using var items = connection.CreateCommand();
        items.CommandText = "SELECT Id, length(Envelope) FROM VaultItems ORDER BY Id;";
        await using var reader = await items.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var idText = reader.GetString(0);
            if (!TryParseCanonicalItemId(idText, out _)) throw new InvalidDataException("Replacement vault contains an invalid or non-canonical item identifier.");
            var envelopeLength = reader.GetInt64(1);
            if (envelopeLength is < 1 or > VaultStorageLimits.MaximumStoredEnvelopeBytes) throw new InvalidDataException("Replacement vault contains an encrypted item outside the supported size limit.");
        }
    }

    private static async Task ValidateStoredItemSetBoundsAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*), COALESCE(SUM(length(Envelope)), 0) FROM VaultItems;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) throw new InvalidDataException("Vault item storage metadata is unavailable.");
        var count = reader.GetInt64(0);
        var totalBytes = reader.GetInt64(1);
        if (count < 0 || count > VaultStorageLimits.MaximumItemCount) throw new InvalidDataException("Vault contains more encrypted records than this build supports safely.");
        if (totalBytes < 0 || totalBytes > VaultStorageLimits.MaximumStoredEnvelopeBytesTotal) throw new InvalidDataException("Vault encrypted record storage exceeds the supported aggregate size limit.");
    }

    private static async Task ValidateUpsertBoundsAsync(SqliteConnection connection, Guid itemId, long envelopeBytes, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*), COALESCE(SUM(length(Envelope)), 0) FROM VaultItems WHERE Id <> $id;";
        command.Parameters.AddWithValue("$id", itemId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) throw new InvalidDataException("Vault item storage metadata is unavailable.");
        var otherCount = reader.GetInt64(0);
        var otherBytes = reader.GetInt64(1);
        if (otherCount < 0 || otherCount >= VaultStorageLimits.MaximumItemCount) throw new InvalidOperationException("Vault has reached the supported encrypted record count limit.");
        if (otherBytes < 0 || otherBytes > VaultStorageLimits.MaximumStoredEnvelopeBytesTotal - envelopeBytes)
            throw new InvalidOperationException("Vault has reached the supported encrypted record storage limit.");
    }

    private static bool TryParseCanonicalItemId(string idText, out Guid id)
    {
        return Guid.TryParseExact(idText, "D", out id) &&
               id != Guid.Empty &&
               string.Equals(idText, id.ToString("D"), StringComparison.Ordinal);
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
            RestoreRecoveryComponent(recovery.DatabasePath, DatabasePath);
            RestoreRecoveryComponent(recovery.WalPath, DatabasePath + "-wal");
            RestoreRecoveryComponent(recovery.ShmPath, DatabasePath + "-shm");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void RestoreRecoveryComponent(string recoveryPath, string activePath)
    {
        if (!File.Exists(recoveryPath)) return;
        TryDeleteFile(activePath);
        File.Move(recoveryPath, activePath, overwrite: true);
    }

    private static void TryDeleteRecoveryFileSet(RecoveryFileSet recovery)
    {
        TryDeleteFile(recovery.DatabasePath);
        TryDeleteFile(recovery.WalPath);
        TryDeleteFile(recovery.ShmPath);
    }

    private void DeleteRecoveryArtifacts(ICollection<Exception> failures)
    {
        var directory = Path.GetDirectoryName(DatabasePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;
        var pattern = Path.GetFileName(DatabasePath) + ".previous.*";
        string[] files;
        try { files = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly); }
        catch (IOException ex) { failures.Add(ex); return; }
        catch (UnauthorizedAccessException ex) { failures.Add(ex); return; }
        foreach (var file in files) TryDeleteManagedFile(file, failures);
    }

    private static void TryDeleteManagedFile(string path, ICollection<Exception> failures)
    {
        try { DeleteIfExists(path); }
        catch (IOException ex) { failures.Add(ex); }
        catch (UnauthorizedAccessException ex) { failures.Add(ex); }
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

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), GetPathComparison());

    private static StringComparison GetPathComparison() =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

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
