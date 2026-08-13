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
}
