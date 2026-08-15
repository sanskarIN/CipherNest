using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using CipherNest.Application.Abstractions;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class BackupHeaderValidationIntegrationTests : IDisposable
{
    private static readonly byte[] Magic = "CNBK0002"u8.ToArray();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestBackupHeader", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public BackupHeaderValidationIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task HostileKdfHeader_IsRejectedBeforeDeriveKey()
    {
        var source = Path.Combine(_directory, "hostile-kdf.cnbak");
        await WriteHeaderOnlyBackupAsync(source, new
        {
            Version = BackupFormatPolicy.CurrentVersion,
            Salt = new byte[16],
            Kdf = new KdfParameters(999_999_999, 3, 1),
            ChunkSize = 1024 * 1024,
            CreatedUtc = DateTimeOffset.UtcNow
        });

        var crypto = new DerivationGuardCryptoService();
        var service = new EncryptedBackupService(new SqliteVaultStore(DatabasePath), crypto);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreEncryptedAsync(source, "Synthetic backup passphrase 2026!"));
        Assert.Equal(0, crypto.DeriveKeyCalls);
    }

    [Fact]
    public async Task UnsupportedVersion_IsRejectedBeforeDeriveKey()
    {
        var source = Path.Combine(_directory, "future-version.cnbak");
        await WriteHeaderOnlyBackupAsync(source, new
        {
            Version = 999,
            Salt = new byte[16],
            Kdf = new KdfParameters(64 * 1024, 3, 1),
            ChunkSize = 1024 * 1024,
            CreatedUtc = DateTimeOffset.UtcNow
        });

        var crypto = new DerivationGuardCryptoService();
        var service = new EncryptedBackupService(new SqliteVaultStore(DatabasePath), crypto);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreEncryptedAsync(source, "Synthetic backup passphrase 2026!"));
        Assert.Equal(0, crypto.DeriveKeyCalls);
    }

    [Fact]
    public async Task DuplicateHeaderMetadata_IsRejectedBeforeDeriveKey()
    {
        var source = Path.Combine(_directory, "duplicate-header-property.cnbak");
        var json = "{\"Version\":2,\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}";
        await WriteRawHeaderAsync(source, Encoding.UTF8.GetBytes(json));

        var crypto = new DerivationGuardCryptoService();
        var service = new EncryptedBackupService(new SqliteVaultStore(DatabasePath), crypto);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreEncryptedAsync(source, "Synthetic backup passphrase 2026!"));
        Assert.Contains("duplicate", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, crypto.DeriveKeyCalls);
    }

    [Fact]
    public async Task UnexpectedHeaderMetadata_IsRejectedBeforeDeriveKey()
    {
        var source = Path.Combine(_directory, "unexpected-header-property.cnbak");
        var json = "{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\",\"Unexpected\":true}";
        await WriteRawHeaderAsync(source, Encoding.UTF8.GetBytes(json));

        var crypto = new DerivationGuardCryptoService();
        var service = new EncryptedBackupService(new SqliteVaultStore(DatabasePath), crypto);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreEncryptedAsync(source, "Synthetic backup passphrase 2026!"));
        Assert.Contains("unexpected", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, crypto.DeriveKeyCalls);
    }

    [Fact]
    public async Task OverDepthHeader_IsNormalizedAndRejectedBeforeDeriveKey()
    {
        var source = Path.Combine(_directory, "over-depth-header.cnbak");
        var nested = string.Concat(Enumerable.Repeat("{\"x\":", BackupFormatPolicy.MaximumHeaderJsonDepth + 1)) +
                     "0" +
                     new string('}', BackupFormatPolicy.MaximumHeaderJsonDepth + 1);
        var json = "{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\",\"Unexpected\":" + nested + "}";
        await WriteRawHeaderAsync(source, Encoding.UTF8.GetBytes(json));

        var crypto = new DerivationGuardCryptoService();
        var service = new EncryptedBackupService(new SqliteVaultStore(DatabasePath), crypto);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreEncryptedAsync(source, "Synthetic backup passphrase 2026!"));
        Assert.Contains("malformed", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, crypto.DeriveKeyCalls);
    }

    [Fact]
    public async Task MaximumLengthValidHeader_ReachesDeriveKey()
    {
        var source = Path.Combine(_directory, "maximum-header.cnbak");
        var header = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Version = BackupFormatPolicy.CurrentVersion,
            Salt = new byte[16],
            Kdf = new KdfParameters(64 * 1024, 3, 1),
            ChunkSize = 1024 * 1024,
            CreatedUtc = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero)
        });
        var padded = new byte[BackupFormatPolicy.MaximumHeaderBytes];
        Array.Fill(padded, (byte)' ');
        header.CopyTo(padded, 0);
        await WriteRawHeaderAsync(source, padded);

        var crypto = new DerivationGuardCryptoService();
        var service = new EncryptedBackupService(new SqliteVaultStore(DatabasePath), crypto);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreEncryptedAsync(source, "Synthetic backup passphrase 2026!"));
        Assert.Equal(1, crypto.DeriveKeyCalls);
    }

    [Fact]
    public async Task HeaderAboveMaximumLength_IsRejectedBeforeReadOrDeriveKey()
    {
        var source = Path.Combine(_directory, "oversized-header.cnbak");
        await WriteRawHeaderAsync(source, new byte[BackupFormatPolicy.MaximumHeaderBytes + 1]);

        var crypto = new DerivationGuardCryptoService();
        var service = new EncryptedBackupService(new SqliteVaultStore(DatabasePath), crypto);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreEncryptedAsync(source, "Synthetic backup passphrase 2026!"));
        Assert.Contains("header size", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, crypto.DeriveKeyCalls);
    }

    [Fact]
    public async Task TruncatedHeader_IsNormalizedToInvalidData()
    {
        var source = Path.Combine(_directory, "truncated-header.cnbak");
        await using (var stream = new FileStream(source, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await stream.WriteAsync(Magic);
            var length = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32BigEndian(length, 64);
            await stream.WriteAsync(length);
            await stream.WriteAsync("{}"u8.ToArray());
        }

        var crypto = new DerivationGuardCryptoService();
        var service = new EncryptedBackupService(new SqliteVaultStore(DatabasePath), crypto);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreEncryptedAsync(source, "Synthetic backup passphrase 2026!"));
        Assert.Contains("truncated", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, crypto.DeriveKeyCalls);
    }

    [Fact]
    public async Task MalformedHeaderJson_IsNormalizedToInvalidData()
    {
        var source = Path.Combine(_directory, "malformed-header.cnbak");
        var malformed = Encoding.UTF8.GetBytes("{\"Version\":2,\"Salt\":");
        await WriteRawHeaderAsync(source, malformed);

        var crypto = new DerivationGuardCryptoService();
        var service = new EncryptedBackupService(new SqliteVaultStore(DatabasePath), crypto);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreEncryptedAsync(source, "Synthetic backup passphrase 2026!"));
        Assert.Contains("malformed", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, crypto.DeriveKeyCalls);
    }

    private static Task WriteHeaderOnlyBackupAsync(string path, object header) => WriteRawHeaderAsync(path, JsonSerializer.SerializeToUtf8Bytes(header));

    private static async Task WriteRawHeaderAsync(string path, byte[] headerJson)
    {
        var length = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(length, headerJson.Length);

        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await stream.WriteAsync(Magic);
        await stream.WriteAsync(length);
        await stream.WriteAsync(headerJson);
    }

    private sealed class DerivationGuardCryptoService : ICryptoService
    {
        public int DeriveKeyCalls { get; private set; }

        public WrappedKeyEnvelope CreateWrappedKey(ReadOnlySpan<char> passphrase) => throw new NotSupportedException();
        public WrappedKeyEnvelope WrapKey(ReadOnlySpan<byte> dataKey, ReadOnlySpan<char> passphrase) => throw new NotSupportedException();
        public byte[] UnwrapKey(ReadOnlySpan<char> passphrase, WrappedKeyEnvelope envelope) => throw new NotSupportedException();
        public EncryptedEnvelope Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData) => throw new NotSupportedException();
        public byte[] Decrypt(EncryptedEnvelope envelope, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData) => throw new NotSupportedException();

        public byte[] DeriveKey(ReadOnlySpan<char> passphrase, ReadOnlySpan<byte> salt, KdfParameters parameters)
        {
            DeriveKeyCalls++;
            throw new InvalidOperationException("Key derivation must not run for an invalid synthetic backup header.");
        }
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
}
