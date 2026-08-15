using System.Buffers.Binary;
using System.IO.Compression;
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
    private static readonly byte[] Magic = "CNBK0002"u8.ToArray();
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
        var destination = BackupPathPolicy.ValidateExportDestination(destinationPath, _store.DatabasePath);
        var working = Path.Combine(Path.GetTempPath(), $"ciphernest-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(working);
        var snapshot = Path.Combine(working, "vault.db");
        var archive = Path.Combine(working, "payload.zip");
        var tempOutput = BackupPathPolicy.CreateTemporarySiblingPath(destination);
        byte[]? key = null;
        try
        {
            await _store.CreateConsistentSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
            await CreateArchiveAsync(snapshot, archive, cancellationToken).ConfigureAwait(false);
            var salt = RandomNumberGenerator.GetBytes(16);
            var kdf = CryptoService.DefaultKdf;
            key = _crypto.DeriveKey(backupPassphrase.AsSpan(), salt, kdf);
            var header = new BackupHeader(BackupFormatPolicy.CurrentVersion, salt, kdf, ChunkSize, DateTimeOffset.UtcNow);
            var headerJson = JsonSerializer.SerializeToUtf8Bytes(header);
            BackupHeaderJsonPolicy.Validate(headerJson);
            await using var output = new FileStream(tempOutput, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);
            await output.WriteAsync(Magic, cancellationToken).ConfigureAwait(false);
            await WriteInt32Async(output, headerJson.Length, cancellationToken).ConfigureAwait(false);
            await output.WriteAsync(headerJson, cancellationToken).ConfigureAwait(false);
            await using var input = new FileStream(archive, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
            var buffer = new byte[ChunkSize];
            var index = 0;
            int read;
            while ((read = await ReadChunkAsync(input, buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                try
                {
                    BackupFormatPolicy.ValidateChunkIndex(index);
                    var isFinal = input.Position == input.Length;
                    var envelope = _crypto.Encrypt(buffer.AsSpan(0, read), key, BuildChunkAad(headerJson, index, isFinal));
                    await WriteInt32Async(output, read, cancellationToken).ConfigureAwait(false);
                    await output.WriteAsync(envelope.Nonce, cancellationToken).ConfigureAwait(false);
                    await output.WriteAsync(envelope.Tag, cancellationToken).ConfigureAwait(false);
                    await output.WriteAsync(envelope.Ciphertext, cancellationToken).ConfigureAwait(false);
                    index++;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(buffer.AsSpan(0, read));
                }
            }
            await WriteInt32Async(output, -1, cancellationToken).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(tempOutput, destination, overwrite: true);
        }
        finally
        {
            if (key is not null) CryptographicOperations.ZeroMemory(key);
            TryDeleteFile(tempOutput);
            TryDeleteDirectory(working);
        }
    }

    public async Task RestoreEncryptedAsync(string sourcePath, string backupPassphrase, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPassphrase);
        cancellationToken.ThrowIfCancellationRequested();
        var sourceLength = new FileInfo(sourcePath).Length;
        if (sourceLength > BackupFormatPolicy.MaximumEncryptedContainerBytes)
            throw new InvalidDataException("Backup container exceeds the supported size limit.");
        var working = Path.Combine(Path.GetTempPath(), $"ciphernest-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(working);
        var archive = Path.Combine(working, "payload.zip");
        var staged = Path.Combine(working, "staged");
        var rollbackDb = Path.Combine(working, "rollback.db");
        byte[]? key = null;
        try
        {
            await using (var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true))
            {
                var magic = new byte[Magic.Length];
                await ReadExactlyAsync(input, magic, cancellationToken).ConfigureAwait(false);
                if (!CryptographicOperations.FixedTimeEquals(magic, Magic)) throw new InvalidDataException("Unsupported or invalid CipherNest backup.");
                var headerLength = await ReadInt32Async(input, cancellationToken).ConfigureAwait(false);
                BackupFormatPolicy.ValidateHeaderLength(headerLength);
                var headerJson = new byte[headerLength];
                await ReadExactlyAsync(input, headerJson, cancellationToken).ConfigureAwait(false);
                BackupHeaderJsonPolicy.Validate(headerJson);
                var header = JsonSerializer.Deserialize<BackupHeader>(headerJson) ?? throw new InvalidDataException("Invalid backup header.");
                if (header.Salt is null || header.Kdf is null) throw new InvalidDataException("Invalid backup header.");
                BackupFormatPolicy.ValidateHeader(header.Version, header.Salt.Length, header.Kdf, header.ChunkSize);
                key = _crypto.DeriveKey(backupPassphrase.AsSpan(), header.Salt, header.Kdf);
                await DecryptArchiveAsync(input, archive, header, headerJson, key, cancellationToken).ConfigureAwait(false);
            }

            Directory.CreateDirectory(staged);
            await ExtractAndValidateArchiveAsync(archive, staged, cancellationToken).ConfigureAwait(false);
            var stagedDb = Path.Combine(staged, "vault.db");
            await ValidateSqliteAsync(stagedDb, cancellationToken).ConfigureAwait(false);
            await _store.CreateConsistentSnapshotAsync(rollbackDb, cancellationToken).ConfigureAwait(false);
            await ReplaceDatabaseAndAttachmentsAsync(staged, rollbackDb, cancellationToken).ConfigureAwait(false);
        }
        catch (EndOfStreamException ex)
        {
            throw new InvalidDataException("Backup is truncated or incomplete.", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Backup header is malformed.", ex);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidDataException("Backup authentication failed. The file may be damaged or the backup passphrase is incorrect.", ex);
        }
        finally
        {
            if (key is not null) CryptographicOperations.ZeroMemory(key);
            TryDeleteDirectory(working);
        }
    }

    private async Task CreateArchiveAsync(string snapshot, string archivePath, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 128 * 1024, useAsync: true);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
        long totalBytes = 0;
        var entryCount = 0;
        (totalBytes, entryCount) = await AddBoundedFileAsync(zip, snapshot, "vault.db", totalBytes, entryCount, cancellationToken).ConfigureAwait(false);

        var attachmentDirectory = Path.Combine(Path.GetDirectoryName(_store.DatabasePath)!, AppConstants.AttachmentDirectoryName);
        if (!Directory.Exists(attachmentDirectory)) return;
        string[] files;
        try { files = Directory.GetFiles(attachmentDirectory, "*.cna", SearchOption.TopDirectoryOnly); }
        catch (IOException ex) { throw new IOException("Encrypted attachment directory could not be enumerated for backup.", ex); }
        catch (UnauthorizedAccessException ex) { throw new IOException("Encrypted attachment directory could not be enumerated for backup.", ex); }
        Array.Sort(files, StringComparer.Ordinal);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(file);
            if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(name), "N", out _)) continue;
            (totalBytes, entryCount) = await AddBoundedFileAsync(zip, file, $"attachments/{name}", totalBytes, entryCount, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<(long TotalBytes, int EntryCount)> AddBoundedFileAsync(ZipArchive zip, string sourcePath, string entryName, long totalBytes, int entryCount, CancellationToken cancellationToken)
    {
        var length = new FileInfo(sourcePath).Length;
        var nextTotal = BackupArchivePolicy.AddEntryLength(totalBytes, length);
        var nextCount = checked(entryCount + 1);
        BackupArchivePolicy.ValidateEntryCount(nextCount);
        await AddFileAsync(zip, sourcePath, entryName, cancellationToken).ConfigureAwait(false);
        return (nextTotal, nextCount);
    }

    private static async Task AddFileAsync(ZipArchive zip, string sourcePath, string entryName, CancellationToken cancellationToken)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
        await using var destination = entry.Open();
        await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
        await source.CopyToAsync(destination, 128 * 1024, cancellationToken).ConfigureAwait(false);
    }

    private async Task DecryptArchiveAsync(Stream input, string archivePath, BackupHeader header, byte[] headerJson, byte[] key, CancellationToken cancellationToken)
    {
        await using var output = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);
        long total = 0;
        var index = 0;
        while (true)
        {
            var plainLength = await ReadInt32Async(input, cancellationToken).ConfigureAwait(false);
            if (plainLength == -1) break;
            BackupFormatPolicy.ValidateChunkIndex(index);
            if (plainLength is < 1 || plainLength > header.ChunkSize) throw new InvalidDataException("Invalid backup chunk size.");
            total = BackupArchivePolicy.AddEntryLength(total, plainLength);
            var nonce = new byte[12];
            var tag = new byte[16];
            var cipher = new byte[plainLength];
            await ReadExactlyAsync(input, nonce, cancellationToken).ConfigureAwait(false);
            await ReadExactlyAsync(input, tag, cancellationToken).ConfigureAwait(false);
            await ReadExactlyAsync(input, cipher, cancellationToken).ConfigureAwait(false);
            var isFinal = input.Position >= input.Length - sizeof(int);
            var plaintext = _crypto.Decrypt(new EncryptedEnvelope(AppConstants.CryptoFormatVersion, nonce, cipher, tag), key, BuildChunkAad(headerJson, index, isFinal));
            try { await output.WriteAsync(plaintext, cancellationToken).ConfigureAwait(false); }
            finally { CryptographicOperations.ZeroMemory(plaintext); }
            index++;
        }
        if (input.Position != input.Length) throw new InvalidDataException("Backup contains trailing unauthenticated data.");
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ExtractAndValidateArchiveAsync(string archivePath, string destination, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        BackupArchivePolicy.ValidateEntryCount(archive.Entries.Count);
        var hasDatabase = false;
        var seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        var copyBuffer = new byte[128 * 1024];
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var normalized = entry.FullName.Replace('\\', '/');
            if (!seenEntries.Add(normalized)) throw new InvalidDataException("Backup contains duplicate paths.");
            var allowedDb = normalized == "vault.db";
            var allowedAttachment = normalized.StartsWith("attachments/", StringComparison.Ordinal) &&
                                    normalized.Count(static c => c == '/') == 1 &&
                                    normalized.EndsWith(".cna", StringComparison.OrdinalIgnoreCase) &&
                                    Guid.TryParseExact(Path.GetFileNameWithoutExtension(normalized), "N", out _);
            if (!allowedDb && !allowedAttachment) throw new InvalidDataException("Backup contains an unexpected path.");
            if (allowedAttachment && entry.Length is < EncryptedAttachmentStore.MinimumContainerBytes or > EncryptedAttachmentStore.MaximumContainerBytes)
                throw new InvalidDataException("Backup attachment container size is outside the supported range.");
            if (allowedDb) hasDatabase = true;
            var target = allowedDb
                ? Path.Combine(destination, "vault.db")
                : Path.Combine(destination, "attachments", Path.GetFileName(normalized));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await using var source = entry.Open();
            await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);
            total = await BackupArchivePolicy.CopyEntryExactlyAsync(
                source,
                output,
                entry.Length,
                total,
                copyBuffer,
                cancellationToken).ConfigureAwait(false);
        }
        if (!hasDatabase) throw new InvalidDataException("Backup does not contain a vault database.");
    }

    private async Task ReplaceDatabaseAndAttachmentsAsync(string staged, string rollbackDb, CancellationToken cancellationToken)
    {
        var root = Path.GetDirectoryName(_store.DatabasePath)!;
        var currentAttachments = Path.Combine(root, AppConstants.AttachmentDirectoryName);
        var stagedAttachments = Path.Combine(staged, "attachments");
        var previousAttachments = Path.Combine(root, $"{AppConstants.AttachmentDirectoryName}.previous.{Guid.NewGuid():N}");
        try
        {
            await _store.ReplaceDatabaseAsync(Path.Combine(staged, "vault.db"), cancellationToken).ConfigureAwait(false);
            if (Directory.Exists(currentAttachments)) Directory.Move(currentAttachments, previousAttachments);
            if (Directory.Exists(stagedAttachments)) Directory.Move(stagedAttachments, currentAttachments);
            else Directory.CreateDirectory(currentAttachments);
            TryDeleteDirectory(previousAttachments);
        }
        catch
        {
            try
            {
                await _store.ReplaceDatabaseAsync(rollbackDb, CancellationToken.None).ConfigureAwait(false);
                if (Directory.Exists(currentAttachments)) TryDeleteDirectory(currentAttachments);
                if (Directory.Exists(previousAttachments)) Directory.Move(previousAttachments, currentAttachments);
            }
            catch
            {
                // Preserve the original restore exception. Recovery material remains in the app's previous/temporary files where the OS permits.
            }
            throw;
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
            if (read == 0) throw new EndOfStreamException("Backup ended unexpectedly.");
            total += read;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private sealed record BackupHeader(int Version, byte[] Salt, KdfParameters Kdf, int ChunkSize, DateTimeOffset CreatedUtc);
}
