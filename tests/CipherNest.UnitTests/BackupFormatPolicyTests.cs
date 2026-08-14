using CipherNest.Application.Abstractions;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class BackupFormatPolicyTests
{
    [Fact]
    public void CurrentHeaderWithinBounds_IsAccepted()
    {
        BackupFormatPolicy.ValidateHeader(
            BackupFormatPolicy.CurrentVersion,
            saltLength: 16,
            CryptoService.DefaultKdf,
            chunkSize: 1024 * 1024);
    }

    [Fact]
    public void EncryptedContainerCeiling_CoversMaximumArchiveAndFraming()
    {
        var minimumExpected = BackupArchivePolicy.MaximumArchiveBytes +
                              ((long)BackupFormatPolicy.MaximumChunkCount * BackupFormatPolicy.EncryptedChunkOverheadBytes) +
                              BackupFormatPolicy.MaximumHeaderBytes;

        Assert.True(BackupFormatPolicy.MaximumEncryptedContainerBytes > minimumExpected);
        Assert.Equal(1_075_855_376L, BackupFormatPolicy.MaximumEncryptedContainerBytes);
    }

    [Theory]
    [InlineData(1, 16, 65536, 3, 1, 1048576)]
    [InlineData(3, 16, 65536, 3, 1, 1048576)]
    [InlineData(2, 15, 65536, 3, 1, 1048576)]
    [InlineData(2, 65, 65536, 3, 1, 1048576)]
    [InlineData(2, 16, 16383, 3, 1, 1048576)]
    [InlineData(2, 16, 524289, 3, 1, 1048576)]
    [InlineData(2, 16, 65536, 0, 1, 1048576)]
    [InlineData(2, 16, 65536, 11, 1, 1048576)]
    [InlineData(2, 16, 65536, 3, 0, 1048576)]
    [InlineData(2, 16, 65536, 3, 17, 1048576)]
    [InlineData(2, 16, 65536, 3, 1, 65535)]
    [InlineData(2, 16, 65536, 3, 1, 4194305)]
    public void OutOfBoundsHeader_IsRejectedAsInvalidData(int version, int saltLength, int memoryKiB, int iterations, int parallelism, int chunkSize)
    {
        Assert.Throws<InvalidDataException>(() => BackupFormatPolicy.ValidateHeader(
            version,
            saltLength,
            new KdfParameters(memoryKiB, iterations, parallelism),
            chunkSize));
    }

    [Fact]
    public void MissingKdfMetadata_IsRejectedAsInvalidData()
    {
        Assert.Throws<InvalidDataException>(() => BackupFormatPolicy.ValidateHeader(
            BackupFormatPolicy.CurrentVersion,
            saltLength: 16,
            null!,
            chunkSize: 1024 * 1024));
    }

    [Fact]
    public void ChunkIndexBounds_AreEnforced()
    {
        BackupFormatPolicy.ValidateChunkIndex(0);
        BackupFormatPolicy.ValidateChunkIndex(BackupFormatPolicy.MaximumChunkCount - 1);

        Assert.Throws<InvalidDataException>(() => BackupFormatPolicy.ValidateChunkIndex(-1));
        Assert.Throws<InvalidDataException>(() => BackupFormatPolicy.ValidateChunkIndex(BackupFormatPolicy.MaximumChunkCount));
    }
}
