using CipherNest.Shared;

namespace CipherNest.Infrastructure.Services;

public static class BackupPathPolicy
{
    public static string ValidateExportDestination(string destinationPath, string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var destination = Path.GetFullPath(destinationPath);
        var database = Path.GetFullPath(databasePath);
        var comparison = GetPathComparison();

        if (string.Equals(destination, database, comparison) ||
            string.Equals(destination, database + "-wal", comparison) ||
            string.Equals(destination, database + "-shm", comparison))
            throw new InvalidOperationException("Backup destination cannot replace the active vault database or SQLite sidecars.");

        var databaseDirectory = Path.GetDirectoryName(database) ?? throw new InvalidOperationException("Vault database directory is unavailable.");
        var destinationDirectory = Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Backup destination directory is unavailable.");
        var databaseFileName = Path.GetFileName(database);
        var destinationFileName = Path.GetFileName(destination);
        if (string.Equals(databaseDirectory, destinationDirectory, comparison) &&
            destinationFileName.StartsWith(databaseFileName + ".previous", comparison))
            throw new InvalidOperationException("Backup destination cannot replace CipherNest database recovery files.");

        var attachmentDirectory = Path.GetFullPath(Path.Combine(databaseDirectory, AppConstants.AttachmentDirectoryName));
        if (IsWithinDirectory(destination, attachmentDirectory, comparison))
            throw new InvalidOperationException("Backup destination cannot be placed inside the encrypted attachment store.");

        return destination;
    }

    public static string CreateTemporarySiblingPath(string validatedDestinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validatedDestinationPath);
        var destination = Path.GetFullPath(validatedDestinationPath);
        var directory = Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Backup destination directory is unavailable.");
        var fileName = Path.GetFileName(destination);
        return Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");
    }

    private static bool IsWithinDirectory(string candidatePath, string directoryPath, StringComparison comparison)
    {
        var directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directoryPath));
        var candidate = Path.GetFullPath(candidatePath);
        if (string.Equals(candidate, directory, comparison)) return true;
        return candidate.StartsWith(directory + Path.DirectorySeparatorChar, comparison);
    }

    private static StringComparison GetPathComparison() =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
