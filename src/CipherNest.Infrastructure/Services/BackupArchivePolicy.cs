namespace CipherNest.Infrastructure.Services;

public static class BackupArchivePolicy
{
    public const int MaximumEntryCount = 10_001;
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
}