using CipherNest.Shared;
using Microsoft.Data.Sqlite;

namespace CipherNest.Infrastructure.Persistence;

internal static class DatabaseMigrator
{
    private sealed record Migration(int Version, IReadOnlyList<string> Statements);

    private static readonly Migration[] Migrations =
    [
        new(1,
        [
            "CREATE TABLE IF NOT EXISTS VaultHeader (Id INTEGER PRIMARY KEY CHECK(Id = 1), HeaderJson TEXT NOT NULL);",
            "CREATE TABLE IF NOT EXISTS VaultItems (Id TEXT PRIMARY KEY, Envelope BLOB NOT NULL);",
            "CREATE TABLE IF NOT EXISTS AppSettings (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);"
        ])
    ];

    public static async Task ApplyAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        await ExecuteAsync(connection, "CREATE TABLE IF NOT EXISTS MigrationHistory (Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL);", cancellationToken).ConfigureAwait(false);

        var current = await GetCurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);
        if (current > AppConstants.DatabaseSchemaVersion)
            throw new InvalidDataException($"Vault database schema version {current} is newer than this CipherNest build supports ({AppConstants.DatabaseSchemaVersion}).");

        foreach (var migration in Migrations.Where(migration => migration.Version > current).OrderBy(static migration => migration.Version))
        {
            if (migration.Version > AppConstants.DatabaseSchemaVersion) break;
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var statement in migration.Statements)
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = statement;
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await using var history = connection.CreateCommand();
                history.Transaction = transaction;
                history.CommandText = "INSERT INTO MigrationHistory(Version, AppliedUtc) VALUES ($version, $utc);";
                history.Parameters.AddWithValue("$version", migration.Version);
                history.Parameters.AddWithValue("$utc", DateTimeOffset.UtcNow.ToString("O"));
                await history.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                try { await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false); }
                catch (InvalidOperationException) { }
                catch (SqliteException) { }
                throw;
            }
        }

        var finalVersion = await GetCurrentVersionAsync(connection, cancellationToken).ConfigureAwait(false);
        if (finalVersion != AppConstants.DatabaseSchemaVersion)
            throw new InvalidDataException($"Vault database migration stopped at version {finalVersion}; expected {AppConstants.DatabaseSchemaVersion}.");

        await ValidateCurrentSchemaAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> GetCurrentVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(Version), 0) FROM MigrationHistory;";
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task ValidateCurrentSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await ValidateShapeAsync(connection, "SELECT Id, HeaderJson FROM VaultHeader LIMIT 0;", cancellationToken).ConfigureAwait(false);
            await ValidateShapeAsync(connection, "SELECT Id, Envelope FROM VaultItems LIMIT 0;", cancellationToken).ConfigureAwait(false);
            await ValidateShapeAsync(connection, "SELECT Key, Value FROM AppSettings LIMIT 0;", cancellationToken).ConfigureAwait(false);
            await ValidateShapeAsync(connection, "SELECT Version, AppliedUtc FROM MigrationHistory LIMIT 0;", cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException ex)
        {
            throw new InvalidDataException("Vault database schema does not match the supported CipherNest structure.", ex);
        }
    }

    private static async Task ValidateShapeAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
