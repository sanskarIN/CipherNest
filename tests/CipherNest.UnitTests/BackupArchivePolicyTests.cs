using CipherNest.Infrastructure.Services;
using CipherNest.Shared;

namespace CipherNest.UnitTests;

public sealed class BackupArchivePolicyTests
{
    [Fact]
    public void EntryCount_AcceptsSupportedMaximumAndRejectsOverflow()
    {
        Assert.Equal(VaultStorageLimits.MaximumAttachmentCountTotal + 1, BackupArchivePolicy.MaximumEntryCount);
        BackupArchivePolicy.ValidateEntryCount(0);
        BackupArchivePolicy.ValidateEntryCount(BackupArchivePolicy.MaximumEntryCount);

        Assert.Throws<InvalidDataException>(() => BackupArchivePolicy.ValidateEntryCount(-1));
        Assert.Throws<InvalidDataException>(() => BackupArchivePolicy.ValidateEntryCount(BackupArchivePolicy.MaximumEntryCount + 1));
    }

    [Fact]
    public void AggregateLength_AcceptsExactLimitAndRejectsOverflowWithoutArithmeticWrap()
    {
        var almostFull = BackupArchivePolicy.MaximumArchiveBytes - 1;
        Assert.Equal(BackupArchivePolicy.MaximumArchiveBytes, BackupArchivePolicy.AddEntryLength(almostFull, 1));

        Assert.Throws<InvalidDataException>(() => BackupArchivePolicy.AddEntryLength(almostFull, 2));
        Assert.Throws<InvalidDataException>(() => BackupArchivePolicy.AddEntryLength(-1, 1));
        Assert.Throws<InvalidDataException>(() => BackupArchivePolicy.AddEntryLength(0, -1));
        Assert.Throws<InvalidDataException>(() => BackupArchivePolicy.AddEntryLength(long.MaxValue, long.MaxValue));
    }

    [Fact]
    public async Task CopyEntryExactly_AcceptsExactDeclaredLengthAndUpdatesAggregate()
    {
        var bytes = Enumerable.Range(0, 10).Select(static value => (byte)value).ToArray();
        await using var source = new MemoryStream(bytes, writable: false);
        await using var destination = new MemoryStream();
        var buffer = new byte[4];

        var total = await BackupArchivePolicy.CopyEntryExactlyAsync(source, destination, bytes.Length, 7, buffer);

        Assert.Equal(17, total);
        Assert.Equal(bytes, destination.ToArray());
    }

    [Fact]
    public async Task CopyEntryExactly_RejectsExpansionBeforeWritingBeyondDeclaredLength()
    {
        await using var source = new MemoryStream([1, 2, 3, 4], writable: false);
        await using var destination = new MemoryStream();
        var buffer = new byte[4];

        await Assert.ThrowsAsync<InvalidDataException>(() => BackupArchivePolicy.CopyEntryExactlyAsync(source, destination, 3, 0, buffer));

        Assert.Equal(0, destination.Length);
    }

    [Fact]
    public async Task CopyEntryExactly_RejectsTruncatedEntry()
    {
        await using var source = new MemoryStream([1, 2, 3], writable: false);
        await using var destination = new MemoryStream();
        var buffer = new byte[2];

        await Assert.ThrowsAsync<InvalidDataException>(() => BackupArchivePolicy.CopyEntryExactlyAsync(source, destination, 4, 0, buffer));

        Assert.Equal(3, destination.Length);
    }

    [Fact]
    public async Task CopyEntryExactly_RejectsAggregateOverflowBeforeReading()
    {
        await using var source = new MemoryStream([1, 2], writable: false);
        await using var destination = new MemoryStream();
        var buffer = new byte[2];

        await Assert.ThrowsAsync<InvalidDataException>(() => BackupArchivePolicy.CopyEntryExactlyAsync(
            source,
            destination,
            2,
            BackupArchivePolicy.MaximumArchiveBytes - 1,
            buffer));

        Assert.Equal(0, source.Position);
        Assert.Equal(0, destination.Length);
    }
}
