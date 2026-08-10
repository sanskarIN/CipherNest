using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class BackupFormatChunkPolicyTests
{
    [Fact]
    public void ChunkIndex_AcceptsSupportedRange()
    {
        BackupFormatPolicy.ValidateChunkIndex(0);
        BackupFormatPolicy.ValidateChunkIndex(BackupFormatPolicy.MaximumChunkCount - 1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65_536)]
    [InlineData(int.MaxValue)]
    public void ChunkIndex_RejectsOutOfRangeValues(int index)
    {
        Assert.Throws<InvalidDataException>(() => BackupFormatPolicy.ValidateChunkIndex(index));
    }
}
