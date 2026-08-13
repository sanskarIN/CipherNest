using System.Buffers.Binary;
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

    private static async Task WriteHeaderOnlyBackupAsync(string path, object header)
    {
        var headerJson = JsonSerializer.SerializeToUtf8Bytes(header);
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
