using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class BackupStagingPolicyTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"CipherNest-BackupStaging-{Guid.NewGuid():N}");

    [Fact]
    public async Task SmallBackup_CopiesWithoutClosingCallerSource()
    {
        Directory.CreateDirectory(_directory);
        await using var source = new MemoryStream([1, 2, 3, 4, 5]);
        var destination = Path.Combine(_directory, "backup.cnbk");

        await BackupStagingPolicy.CopyToNewFileAsync(source, destination);

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, await File.ReadAllBytesAsync(destination));
        Assert.True(source.CanRead);
    }

    [Fact]
    public async Task OversizedSeekableSource_IsRejectedBeforeDestinationCreation()
    {
        Directory.CreateDirectory(_directory);
        await using var source = new OversizedSeekableStream();
        var destination = Path.Combine(_directory, "oversized.cnbk");

        await Assert.ThrowsAsync<InvalidDataException>(() => BackupStagingPolicy.CopyToNewFileAsync(source, destination));

        Assert.False(File.Exists(destination));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class OversizedSeekableStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => BackupFormatPolicy.MaximumEncryptedContainerBytes + 1;
        public override long Position { get; set; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
