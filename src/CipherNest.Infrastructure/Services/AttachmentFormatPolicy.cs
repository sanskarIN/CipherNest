namespace CipherNest.Infrastructure.Services;

public static class AttachmentFormatPolicy
{
    public const int MaximumChunkCount = 16_384;

    public static void ValidateChunkIndex(int zeroBasedIndex)
    {
        if (zeroBasedIndex < 0 || zeroBasedIndex >= MaximumChunkCount)
            throw new InvalidDataException("Attachment contains too many encrypted chunks.");
    }
}
