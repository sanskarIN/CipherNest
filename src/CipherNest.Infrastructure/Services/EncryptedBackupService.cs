using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CipherNest.Application.Abstractions;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Shared;

namespace CipherNest.Infrastructure.Services;

public sealed class EncryptedBackupService : IBackupService
{
    private const int ChunkSize = 1024 * 1024;
    private static readonly byte[] Magic = "CNBK0001"u8.ToArray();
    private readonly IVaultStore _store;
    private readonly ICryptoService _crypto;

    public EncryptedBackupService(IVaultStore store, ICryptoService crypto)
    {
        _store = store;
        _crypto = crypto;
    }

    public async Task ExportEncryptedAsync(string destinationPath, string backupPassphrase, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPassphrase);
        var snapshot = Path.Combine(Path.GetTempPath(), $"ciphernest-snapshot-{Guid.NewGuid():N}.db");
        var temp = destinationPath + ".tmp";
        var salt = RandomNumberGenerator.GetBytes(16);
        var kdf = CryptoService.DefaultKdf;
        byte[]? key = null;
        try
        {
            await _store.CreateConsistentSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
            key = _crypto.DeriveKey(backupPassphrase.AsSpan(), salt, kdf);
            var header = new BackupHeader(AppConstants.CryptoFormatVersion, salt, kdf, ChunkSize);
            var headerJson = JsonSerializer.SerializeToUtf8Bytes(header);
            await using var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);
            await output.WriteAsync(Magic, cancellationToken).ConfigureAwait(false);
            await WriteInt32Async(output, headerJson.Length, cancellationToken).ConfigureAwait(false);
            await output.WriteAsync(headerJson, cancellationToken).ConfigureAwait(false);
            await using var input = new FileStream(snapshot, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
            var buffer = new byte[ChunkSize];
            var chunkIndex = 0;
            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
            {
                var isFinal = input.Position == input.Length;
                var aad = BuildChunkAad(headerJson, chunkIndex, isFinal);
                var envelope = _crypto.Encrypt(buffer.AsSpan(0, read), key, aad);
                await WriteInt32Async(output, read, cancellationToken).ConfigureAwait(false);
                await output.WriteAsync(envelope.Nonce, cancellationToken).ConfigureAwait(false);
                await output.WriteAsync(envelope.Tag, cancellationToken).ConfigureAwait(false);
                await output.WriteAsync(envelope.Ciphertext, cancellationToken).ConfigureAwait(false);
                chunkIndex++;
            }
            await WriteInt32Async(output, -1, cancellationToken).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            File.Move(temp, destinationPath, overwrite: true);
        }
        finally
        {
            if (key is not null) CryptographicOperations.ZeroMemory(key);
            if (File.Exists(snapshot)) File.Delete(snapshot);
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    public async Task RestoreEncryptedAsync(string sourcePath, string backupPassphrase, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPassphrase);
        var restoreDb = Path.Combine(Path.GetTempPath(), $"ciphernest-restore-{Guid.NewGuid():N}.db");
        byte[]? key = null;
        try
        {
            await using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
            var magic = new byte[Magic.Length];
            await ReadExactlyAsync(input, magic, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(magic, Magic)) throw new InvalidDataException("Not a CipherNest backup.");
            var headerLength = await ReadInt32Async(input, cancellationToken).ConfigureAwait(false);
            if (headerLength is < 16 or > 16_384) throw new InvalidDataException("Invalid backup header size.");
            var headerJson = new byte[headerLength];
            await ReadExactlyAsync(input, headerJson, cancellationToken).ConfigureAwait(false);
            var header = JsonSerializer.Deserialize<BackupHeader>(headerJson) ?? throw new InvalidDataException("Invalid backup header.");
            if (header.Version != AppConstants.CryptoFormatVersion || header.ChunkSize is < 64 * 1024 or > 4 * 1024 * 1024) throw new InvalidDataException("Unsupported backup format.");
            key = _crypto.DeriveKey(backupPassphrase.AsSpan(), header.Salt, header.Kdf);
            await using (var output = new FileStream(restoreDb, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true))
            {
                var index = 0;
                while (true)
                {
                    var plainLength = await ReadInt32Async(input, cancellationToken).ConfigureAwait(false);
                    if (plainLength == -1) break;
                    if (plainLength is < 1 || plainLength > header.ChunkSize) throw new InvalidDataException("Invalid backup chunk size.");
                    var nonce = new byte[12];
                    var tag = new byte[16];
                    var cipher = new byte[plainLength];
                    await ReadExactlyAsync(input, nonce, cancellationToken).ConfigureAwait(false);
                    await ReadExactlyAsync(input, tag, cancellationToken).ConfigureAwait(false);
                    await ReadExactlyAsync(input, cipher, cancellationToken).ConfigureAwait(false);
                    var isFinal = input.Position >= input.Length - sizeof(int);
                    var plaintext = _crypto.Decrypt(new EncryptedEnvelope(header.Version, nonce, cipher, tag), key, BuildChunkAad(headerJson, index, isFinal));
                    try { await output.WriteAsync(plaintext, cancellationToken).ConfigureAwait(false); }
                    finally { CryptographicOperations.ZeroMemory(plaintext); }
                    index++;
                }
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            await ValidateSqliteAsync(restoreDb, cancellationToken).ConfigureAwait(false);
            await _store.ReplaceDatabaseAsync(restoreDb, cancellationToken).ConfigureAwait(false);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidDataException("Backup authentication failed. The file may be damaged or the backup passphrase is incorrect.", ex);
        }
        finally
        {
            if (key is not null) CryptographicOperations.ZeroMemory(key);
            if (File.Exists(restoreDb)) File.Delete(restoreDb);
        }
    }

    private static async Task ValidateSqliteAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64, useAsync: true);
        var header = new byte[16];
        await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false);
        if (!Encoding.ASCII.GetString(header).Equals("SQLite format 3\0", StringComparison.Ordinal)) throw new InvalidDataException("Restored payload is not a valid SQLite database.");
    }

    private static byte[] BuildChunkAad(byte[] header, int index, bool isFinal)
    {
        var hash = SHA256.HashData(header);
        var aad = new byte[hash.Length + 5];
        hash.CopyTo(aad, 0);
        BinaryPrimitives.WriteInt32BigEndian(aad.AsSpan(hash.Length, 4), index);
        aad[^1] = isFinal ? (byte)1 : (byte)0;
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

    private static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[total..], cancellationToken).ConfigureAwait(false);
            if (read == 0) throw new EndOfStreamException("Backup ended unexpectedly.");
            total += read;
        }
    }

    private sealed record BackupHeader(int Version, byte[] Salt, KdfParameters Kdf, int ChunkSize);
}
