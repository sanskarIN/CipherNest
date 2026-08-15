using CipherNest.Shared;

namespace CipherNest.Infrastructure.Services;

public static class BackupArchivePolicy
{
    public const int MaximumEntryCount = VaultStorageLimits.MaximumAttachmentCountTotal + 1;
    public const long MaximumArchiveBytes = 1024L * 1024 * 1024;

    public static void ValidateEntryCount(int count)
    {
        if (count < 0 || count > MaximumEntryCount)
            throw new InvalidDataException("Backup contains too many files.");
    }

    public static long AddEntryLength(long currentTotal, long entryLength)
    {
        if (currentTotal < 0 || currentTotal > MaximumArchiveBytes)
            throw new InvalidDataException("Backup content exceeds the supported size limit.");
        if (entryLength < 0 || entryLength > MaximumArchiveBytes - currentTotal)
            throw new InvalidDataException("Backup content exceeds the supported size limit.");
        return currentTotal + entryLength;
    }

    public static async Task<long> CopyEntryExactlyAsync(
        Stream source,
        Stream destination,
        long declaredLength,
        long currentTotal,
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (!source.CanRead) throw new ArgumentException("Backup entry source stream must be readable.", nameof(source));
        if (!destination.CanWrite) throw new ArgumentException("Backup entry destination stream must be writable.", nameof(destination));
        if (buffer.IsEmpty) throw new ArgumentException("Backup entry copy buffer must not be empty.", nameof(buffer));

        var nextTotal = AddEntryLength(currentTotal, declaredLength);
        long copied = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            if (read > declaredLength - copied)
                throw new InvalidDataException("Backup entry expands beyond its declared uncompressed size.");

            await destination.WriteAsync(buffer[..read], cancellationToken).ConfigureAwait(false);
            copied += read;
        }

        if (copied != declaredLength)
            throw new InvalidDataException("Backup entry uncompressed size does not match its declared size.");

        return nextTotal;
    }
}
