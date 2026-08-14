using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class EncryptedAttachmentStoreBoundsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestAttachmentBoundsTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task EncryptAsync_RejectsOversizedSeekableSourceBeforeCreatingAttachmentDirectory()
    {
        var store = new EncryptedAttachmentStore(_directory, new CryptoService());
        var attachmentId = Guid.NewGuid();
        using var source = new OversizedSeekableStream(EncryptedAttachmentStore.MaximumPlaintextBytes + 1);

        await Assert.ThrowsAsync<InvalidDataException>(() => store.EncryptAsync(
            Guid.NewGuid(),
            attachmentId,
            source,
            store.GetOpaqueFileName(attachmentId),
            new byte[32],
            CancellationToken.None));

        Assert.Equal(0, source.ReadCalls);
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public async Task DecryptToAsync_RejectsUndersizedContainerBeforeParsingPayload()
    {
        Directory.CreateDirectory(_directory);
        var store = new EncryptedAttachmentStore(_directory, new CryptoService());
        var attachmentId = Guid.NewGuid();
        var name = store.GetOpaqueFileName(attachmentId);
        await File.WriteAllBytesAsync(store.GetPath(name), new byte[EncryptedAttachmentStore.MinimumContainerBytes - 1]);
        using var destination = new MemoryStream();

        await Assert.ThrowsAsync<InvalidDataException>(() => store.DecryptToAsync(
            Guid.NewGuid(),
            attachmentId,
            name,
            expectedPlaintextLength: 0,
            destination,
            new byte[32],
            CancellationToken.None));

        Assert.Equal(0, destination.Length);
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
    }

    private sealed class OversizedSeekableStream(long length) : Stream
    {
        public int ReadCalls { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position { get; set; }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCalls++;
            throw new InvalidOperationException("The oversized source must be rejected before reading.");
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            return ValueTask.FromException<int>(new InvalidOperationException("The oversized source must be rejected before reading."));
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
