using System.Text.Json;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Exceptions;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class VaultHeaderAdversarialIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestVaultHeaderCorpus", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public VaultHeaderAdversarialIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task DeterministicAdversarialCorpus_IsRejectedBeforeWrappedKeyUnwrap()
    {
        var corpus = BuildCorpus();
        Assert.Equal(120, corpus.Count);

        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();

        for (var index = 0; index < corpus.Count; index++)
        {
            await store.WriteHeaderAsync(corpus[index]);
            var crypto = new UnwrapGuardCryptoService();
            using var vault = new VaultService(store, crypto, new SystemClock());

            await Assert.ThrowsAsync<VaultAuthenticationException>(() => vault.UnlockAsync("Synthetic vault passphrase 2026!"));
            Assert.Equal(0, crypto.UnwrapCalls);
            Assert.False(vault.IsUnlocked);
        }
    }

    private static List<string> BuildCorpus()
    {
        var valid = BuildHeader();
        var corpus = new List<string>
        {
            "{}",
            "[]",
            "null",
            "{\"version\":2}",
            valid.Replace("\"version\":2", "\"version\":2,\"version\":2", StringComparison.Ordinal),
            valid.Replace("\"version\":2", "\"Version\":2", StringComparison.Ordinal),
            valid.Replace(",\"secondary\":null", string.Empty, StringComparison.Ordinal),
            valid[..^1] + ",\"unexpected\":true}",
            valid.Replace("\"master\":{\"version\":1", "\"master\":{\"version\":1,\"version\":1", StringComparison.Ordinal),
            valid.Replace("\"master\":{\"version\":1", "\"master\":{\"unexpected\":true,\"version\":1", StringComparison.Ordinal),
            valid.Replace("\"kdf\":{\"memoryKiB\":65536", "\"kdf\":{\"memoryKiB\":65536,\"memoryKiB\":65536", StringComparison.Ordinal),
            valid.Replace("\"kdf\":{", "\"kdf\":{\"unexpected\":1,", StringComparison.Ordinal),
            valid.Replace("\"iterations\":3", "\"iterations\":\"3\"", StringComparison.Ordinal),
            valid.Replace("\"salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\"", "\"salt\":null", StringComparison.Ordinal),
            valid.Replace("\"nonce\":\"AAAAAAAAAAAAAAAA\"", "\"nonce\":[]", StringComparison.Ordinal),
            valid.Replace("\"ciphertext\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=\"", "\"ciphertext\":{}", StringComparison.Ordinal),
            valid.Replace("\"tag\":\"AAAAAAAAAAAAAAAAAAAAAA==\"", "\"tag\":false", StringComparison.Ordinal),
            valid.Replace("\"salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\"", "\"salt\":\"!not-base64!\"", StringComparison.Ordinal),
            valid[..^1],
            valid[..^1] + ",}",
        };

        var nested = string.Concat(Enumerable.Repeat("{\"x\":", CipherNest.Shared.VaultStorageLimits.MaximumVaultHeaderJsonDepth + 1)) +
                     "0" + new string('}', CipherNest.Shared.VaultStorageLimits.MaximumVaultHeaderJsonDepth + 1);
        corpus.Add(valid[..^1] + ",\"deep\":" + nested + "}");

        var random = new Random(0x434E5648);
        for (var index = 0; index < 45; index++)
        {
            var payload = JsonSerializer.Serialize(RandomPrintable(random, 4 + random.Next(60)));
            corpus.Add(valid[..^1] + $",\"unknownRoot{index:D2}\":{payload}}}");
        }

        for (var index = 0; index < 34; index++)
        {
            var payload = JsonSerializer.Serialize(RandomPrintable(random, 4 + random.Next(48)));
            corpus.Add(valid.Replace(
                "\"master\":{\"version\":1",
                $"\"master\":{{\"unknownWrapper{index:D2}\":{payload},\"version\":1",
                StringComparison.Ordinal));
        }

        for (var index = 0; index < 20; index++)
        {
            var value = random.Next(1, 1000);
            corpus.Add(valid.Replace(
                "\"kdf\":{\"memoryKiB\":65536",
                $"\"kdf\":{{\"unknownKdf{index:D2}\":{value},\"memoryKiB\":65536",
                StringComparison.Ordinal));
        }

        Assert.Equal(120, corpus.Count);
        return corpus;
    }

    private static string RandomPrintable(Random random, int length)
    {
        var chars = new char[length];
        for (var index = 0; index < chars.Length; index++) chars[index] = (char)random.Next(0x20, 0x7F);
        return new string(chars);
    }

    private static string BuildHeader()
    {
        const string wrapper = "{\"version\":1,\"salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"kdf\":{\"memoryKiB\":65536,\"iterations\":3,\"parallelism\":1},\"nonce\":\"AAAAAAAAAAAAAAAA\",\"ciphertext\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=\",\"tag\":\"AAAAAAAAAAAAAAAAAAAAAA==\"}";
        return $"{{\"version\":2,\"master\":{wrapper},\"recovery\":null,\"secondary\":null}}";
    }

    private sealed class UnwrapGuardCryptoService : ICryptoService
    {
        public int UnwrapCalls { get; private set; }

        public WrappedKeyEnvelope CreateWrappedKey(ReadOnlySpan<char> passphrase) => throw new NotSupportedException();
        public WrappedKeyEnvelope WrapKey(ReadOnlySpan<byte> dataKey, ReadOnlySpan<char> passphrase) => throw new NotSupportedException();

        public byte[] UnwrapKey(ReadOnlySpan<char> passphrase, WrappedKeyEnvelope envelope)
        {
            UnwrapCalls++;
            throw new InvalidOperationException("Hostile vault headers must be rejected before wrapped-key unwrap.");
        }

        public EncryptedEnvelope Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData) => throw new NotSupportedException();
        public byte[] Decrypt(EncryptedEnvelope envelope, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData) => throw new NotSupportedException();
        public byte[] DeriveKey(ReadOnlySpan<char> passphrase, ReadOnlySpan<byte> salt, KdfParameters parameters) => throw new NotSupportedException();
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
