using CipherNest.Application.Abstractions;
using CipherNest.Infrastructure.Crypto;

namespace CipherNest.Infrastructure.Services;

public static class BackupFormatPolicy
{
    public const int CurrentVersion = 2;
    public const int MinimumChunkSize = 64 * 1024;
    public const int MaximumChunkSize = 4 * 1024 * 1024;
    public const int MaximumChunkCount = 65_536;
    public const int MinimumSaltBytes = 16;
    public const int MaximumSaltBytes = 64;

    public static void ValidateHeader(int version, int saltLength, KdfParameters kdf, int chunkSize)
    {
        if (version != CurrentVersion)
            throw new InvalidDataException("Unsupported backup format version.");
        if (saltLength is < MinimumSaltBytes or > MaximumSaltBytes)
            throw new InvalidDataException("Backup salt length is outside supported bounds.");
        if (chunkSize is < MinimumChunkSize or > MaximumChunkSize)
            throw new InvalidDataException("Backup chunk size is outside supported bounds.");
        if (kdf is null ||
            kdf.MemoryKiB is < CryptoService.MinimumKdfMemoryKiB or > CryptoService.MaximumKdfMemoryKiB ||
            kdf.Iterations is < 1 or > CryptoService.MaximumKdfIterations ||
            kdf.Parallelism is < 1 or > CryptoService.MaximumKdfParallelism)
        {
            throw new InvalidDataException("Backup key-derivation parameters are outside supported resource bounds.");
        }
    }

    public static void ValidateChunkIndex(int zeroBasedIndex)
    {
        if (zeroBasedIndex < 0 || zeroBasedIndex >= MaximumChunkCount)
            throw new InvalidDataException("Backup contains too many encrypted chunks.");
    }
}
