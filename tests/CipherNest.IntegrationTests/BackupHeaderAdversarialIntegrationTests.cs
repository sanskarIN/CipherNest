using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using CipherNest.Application.Abstractions;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class BackupHeaderAdversarialIntegrationTests : IDisposable
{
    private const string ValidHeader = "{\"Version\":2,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}";
    private static readonly byte[] Magic = "CNBK0002"u8.ToArray();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestBackupHeaderCorpus", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public BackupHeaderAdversarialIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task DeterministicAdversarialCorpus_IsRejectedBeforeKeyDerivation()
    {
        var corpus = BuildCorpus();
        Assert.True(corpus.Count >= 80);

        for (var index = 0; index < corpus.Count; index++)
        {
            var source = Path.Combine(_directory, $"header-{index:D3}.cnbak");
            await WriteRawHeaderAsync(source, corpus[index]);
            var crypto = new DerivationGuardCryptoService();
            var service = new EncryptedBackupService(new SqliteVaultStore(DatabasePath), crypto);

            await Assert.ThrowsAsync<InvalidDataException>(() => service.RestoreEncryptedAsync(source, "Synthetic backup passphrase 2026!"));
            Assert.Equal(0, crypto.DeriveKeyCalls);
        }
    }

    private static List<byte[]> BuildCorpus()
    {
        var corpus = new List<byte[]>
        {
            Encoding.UTF8.GetBytes("{}".PadRight(BackupFormatPolicy.MinimumHeaderBytes)),
            Encoding.UTF8.GetBytes("[]".PadRight(BackupFormatPolicy.MinimumHeaderBytes)),
            Encoding.UTF8.GetBytes("null".PadRight(BackupFormatPolicy.MinimumHeaderBytes)),
            Encoding.UTF8.GetBytes("{\"Version\":2}"),
            Encoding.UTF8.GetBytes(ValidHeader.Replace("\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"", "\"CreatedUtc\":null", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(ValidHeader.Replace("\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\"", "\"Salt\":\"!not-base64!\"", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(ValidHeader.Replace("\"MemoryKiB\":65536", "\"MemoryKiB\":999999999", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(ValidHeader.Replace("\"Kdf\":{", "\"Kdf\":{\"MemoryKiB\":65536,", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(ValidHeader[..^1] + ",\"Unexpected\":true}"),
            Encoding.UTF8.GetBytes("{\"Version\":2,\"Version\":999,\"Salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"Kdf\":{\"MemoryKiB\":65536,\"Iterations\":3,\"Parallelism\":1},\"ChunkSize\":1048576,\"CreatedUtc\":\"2026-08-15T00:00:00+00:00\"}")
        };

        var random = new Random(0x434E424B);
        for (var index = 0; index < 48; index++)
        {
            var chars = new char[8 + random.Next(48)];
            for (var charIndex = 0; charIndex < chars.Length; charIndex++)
                chars[charIndex] = (char)random.Next(0x20, 0x7F);
            var noise = JsonSerializer.Serialize(new string(chars));
            corpus.Add(Encoding.UTF8.GetBytes(ValidHeader[..^1] + $",\"Unexpected{index:D2}\":{noise}}}"));
        }

        for (var index = 0; index < 32; index++)
        {
            var bytes = new byte[64 + index];
            random.NextBytes(bytes);
            bytes[0] = 0xFF;
            corpus.Add(bytes);
        }

        return corpus;
    }

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
            throw new InvalidOperationException("Adversarial backup headers must be rejected before key derivation.");
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
