using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class AttachmentFormatPolicyTests
{
    [Fact]
    public void ChunkIndex_AcceptsSupportedRange()
    {
        AttachmentFormatPolicy.ValidateChunkIndex(0);
        AttachmentFormatPolicy.ValidateChunkIndex(AttachmentFormatPolicy.MaximumChunkCount - 1);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16_384)]
    [InlineData(int.MaxValue)]
    public void ChunkIndex_RejectsUnsupportedRange(int index)
    {
        Assert.Throws<InvalidDataException>(() => AttachmentFormatPolicy.ValidateChunkIndex(index));
    }
}
