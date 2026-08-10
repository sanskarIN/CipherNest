using System.Buffers.Binary;
using System.Security.Cryptography;
using CipherNest.Application.Abstractions;
using CipherNest.Shared;

namespace CipherNest.Infrastructure.Services;

public sealed class EncryptedAttachmentStore
{
    private const int ChunkSize = 256 * 1024;
    public const long MaximumPlaintextBytes = 100L * 1024 * 1024;
    public const long MinimumContainerBytes = 12;
    public const long MaximumContainerBytes = MaximumPlaintextBytes + (((MaximumPlaintextBytes + ChunkSize - 1) / ChunkSize) * 32) + 8 + 4;
    private static readonly byte[] Magic = "CNAT0001"u8.ToArray();
    private readonly string _directory;
    private readonly ICryptoService _crypto;

    public EncryptedAttachmentStore(string directory, ICryptoService crypto)
    {
        _directory = directory;
        _crypto = crypto;
    }

    public string GetOpaqueFileName(Guid attachmentId) => $"{attachmentId:N}.cna";
    public string GetPath(string opaqueFileName) => Path.Combine(_directory, AttachmentStorageNamePolicy.ValidateOpaqueFileName(opaqueFileName));

    public async Task<long> EncryptAsync(Guid itemId, Guid attachmentId, Stream source, string opaqueFileName, ReadOnlyMemory<byte> dataKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        Directory.CreateDirectory(_directory);
        var finalPath = GetPath(opaqueFileName);
        var tempPath = Path.Combine(_directory, $".{Path.GetFileName(finalPath)}.{Guid.NewGuid():N}.tmp");
        var buffer = new byte[ChunkSize];
        long total = 0;
        try
        {
            await using var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);
            await output.WriteAsync(Magic, cancellationToken).ConfigureAwait(false);
            var chunkIndex = 0;
            int read;
            while ((read = await ReadChunkAsync(source, buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                AttachmentFormatPolicy.ValidateChunkIndex(chunkIndex);
                total += read;
                if (total > MaximumPlaintextBytes) throw new InvalidDataException("Attachment exceeds the 100 MB safety limit.");
                var aad = BuildAad(itemId, attachmentId, chunkIndex);
                var envelope = _crypto.Encrypt(buffer.AsSpan(0, read), dataKey.Span, aad);
                await WriteInt32Async(output, read, cancellationToken).ConfigureAwait(false);
                await output.WriteAsync(envelope.Nonce, cancellationToken).ConfigureAwait(false);
                await output.WriteAsync(envelope.Tag, cancellationToken).ConfigureAwait(false);
                await output.WriteAsync(envelope.Ciphertext, cancellationToken).ConfigureAwait(false);
                CryptographicOperations.ZeroMemory(buffer.AsSpan(0, read));
                chunkIndex++;
            }
            await WriteInt32Async(output, -1, cancellationToken).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, finalPath, overwrite: false);
            return total;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
            TryDeleteFile(tempPath);
        }
    }

    public async Task DecryptToAsync(Guid itemId, Guid attachmentId, string opaqueFileName, long expectedPlaintextLength, Stream destination, ReadOnlyMemory<byte> dataKey, CancellationToken cancellationToken)
    {
        if (expectedPlaintextLength is < 0 or > MaximumPlaintextBytes) throw new InvalidDataException("Attachment length is outside the supported range.");
        await using var input = new FileStream(GetPath(opaqueFileName), FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
        var magic = new byte[Magic.Length];
        await ReadExactlyAsync(input, magic, cancellationToken).ConfigureAwait(false);
        if (!CryptographicOperations.FixedTimeEquals(magic, Magic)) throw new InvalidDataException("Attachment container is invalid.");
        long total = 0;
        var chunkIndex = 0;
        while (true)
        {
            var plainLength = await ReadInt32Async(input, cancellationToken).ConfigureAwait(false);
            if (plainLength == -1) break;
            AttachmentFormatPolicy.ValidateChunkIndex(chunkIndex);
            if (plainLength is < 1 or > ChunkSize) throw new InvalidDataException("Attachment chunk is invalid.");
            var nonce = new byte[12];
            var tag = new byte[16];
            var cipher = new byte[plainLength];
            await ReadExactlyAsync(input, nonce, cancellationToken).ConfigureAwait(false);
            await ReadExactlyAsync(input, tag, cancellationToken).ConfigureAwait(false);
            await ReadExactlyAsync(input, cipher, cancellationToken).ConfigureAwait(false);
            var plaintext = _crypto.Decrypt(new EncryptedEnvelope(AppConstants.CryptoFormatVersion, nonce, cipher, tag), dataKey.Span, BuildAad(itemId, attachmentId, chunkIndex));
            try
            {
                total += plaintext.Length;
                if (total > expectedPlaintextLength) throw new InvalidDataException("Attachment length authentication failed.");
                await destination.WriteAsync(plaintext, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
            chunkIndex++;
        }
        if (total != expectedPlaintextLength || input.Position != input.Length) throw new InvalidDataException("Attachment is truncated or contains trailing data.");
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Delete(string opaqueFileName)
    {
        var path = GetPath(opaqueFileName);
        if (File.Exists(path)) File.Delete(path);
    }

    private static byte[] BuildAad(Guid itemId, Guid attachmentId, int chunkIndex)
    {
        var aad = new byte[16 + 16 + 4];
        itemId.TryWriteBytes(aad.AsSpan(0, 16));
        attachmentId.TryWriteBytes(aad.AsSpan(16, 16));
        BinaryPrimitives.WriteInt32BigEndian(aad.AsSpan(32, 4), chunkIndex);
        return aad;
    }

    private static async Task WriteInt32Async(Stream stream, int value, CancellationToken cancellationToken)
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(bytes, value);
        await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ReadInt32Async(Stream stream, CancellationToken cancellationToken)
    {
        var bytes = new byte[4];
        await ReadExactlyAsync(stream, bytes, cancellationToken).ConfigureAwait(false);
        return BinaryPrimitives.ReadInt32BigEndian(bytes);
    }

    private static async Task<int> ReadChunkAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[total..], cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            total += read;
        }
        return total;
    }

    private static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[total..], cancellationToken).ConfigureAwait(false);
            if (read == 0) throw new EndOfStreamException("Attachment ended unexpectedly.");
            total += read;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
