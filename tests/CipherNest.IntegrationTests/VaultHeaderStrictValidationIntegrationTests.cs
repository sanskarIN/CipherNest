using System.Text.Json.Nodes;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Exceptions;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;
using CipherNest.Shared;
using Microsoft.Data.Sqlite;

namespace CipherNest.IntegrationTests;

public sealed class VaultHeaderStrictValidationIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestStrictVaultHeader", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public VaultHeaderStrictValidationIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task LegacyVersion1Header_RemainsUnlockable()
    {
        const string master = "Legacy header compatibility passphrase 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);
        await vault.LockAsync();

        var header = JsonNode.Parse(await store.ReadHeaderAsync() ?? throw new InvalidDataException())?.AsObject()
            ?? throw new InvalidDataException("Current vault header could not be parsed.");
        header["version"] = 1;
        header.Remove("secondary");
        await store.WriteHeaderAsync(header.ToJsonString());

        await vault.UnlockAsync(master);
        Assert.True(vault.IsUnlocked);
    }

    [Fact]
    public async Task MutatingLegacyVersion1Header_UpgradesItToVersion2()
    {
        const string currentMaster = "Legacy rotation current passphrase 2026!";
        const string nextMaster = "Legacy rotation replacement passphrase 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync(currentMaster, createRecoveryKey: false);

        var legacy = JsonNode.Parse(await store.ReadHeaderAsync() ?? throw new InvalidDataException())?.AsObject()
            ?? throw new InvalidDataException("Current vault header could not be parsed.");
        legacy["version"] = 1;
        legacy.Remove("secondary");
        await store.WriteHeaderAsync(legacy.ToJsonString());

        await vault.ChangeMasterPassphraseAsync(currentMaster, nextMaster);

        var upgraded = JsonNode.Parse(await store.ReadHeaderAsync() ?? throw new InvalidDataException())?.AsObject()
            ?? throw new InvalidDataException("Upgraded vault header could not be parsed.");
        Assert.Equal(VaultHeaderJsonPolicy.CurrentVersion, upgraded["version"]?.GetValue<int>());
        Assert.True(upgraded.ContainsKey("secondary"));
        Assert.Null(upgraded["secondary"]);

        await vault.LockAsync();
        await vault.UnlockAsync(nextMaster);
        Assert.True(vault.IsUnlocked);
    }

    [Fact]
    public async Task StructurallyValidVersion2Header_ReachesExactlyOneUnwrap()
    {
        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();
        await store.WriteHeaderAsync(BuildSyntheticVersion2Header());
        var crypto = new ReachabilityCryptoService();
        using var vault = new VaultService(store, crypto, new SystemClock());

        await vault.UnlockAsync("Synthetic vault passphrase 2026!");

        Assert.Equal(1, crypto.UnwrapCalls);
        Assert.True(vault.IsUnlocked);
    }

    [Fact]
    public async Task OversizedPersistedHeader_IsNormalizedBeforeWrappedKeyUnwrap()
    {
        var store = new SqliteVaultStore(DatabasePath);
        await store.InitializeAsync();
        var oversized = new string('x', VaultStorageLimits.MaximumVaultHeaderUtf8Bytes + 1);

        await using (var connection = new SqliteConnection($"Data Source={DatabasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO VaultHeader(Id, HeaderJson) VALUES (1, $header) ON CONFLICT(Id) DO UPDATE SET HeaderJson = excluded.HeaderJson;";
            command.Parameters.AddWithValue("$header", oversized);
            await command.ExecuteNonQueryAsync();
        }

        var crypto = new ReachabilityCryptoService();
        using var vault = new VaultService(store, crypto, new SystemClock());
        await Assert.ThrowsAsync<VaultAuthenticationException>(() => vault.UnlockAsync("Synthetic vault passphrase 2026!"));
        Assert.Equal(0, crypto.UnwrapCalls);
        Assert.False(vault.IsUnlocked);
    }

    private static string BuildSyntheticVersion2Header()
    {
        const string wrapper = "{\"version\":1,\"salt\":\"AAAAAAAAAAAAAAAAAAAAAA==\",\"kdf\":{\"memoryKiB\":65536,\"iterations\":3,\"parallelism\":1},\"nonce\":\"AAAAAAAAAAAAAAAA\",\"ciphertext\":\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=\",\"tag\":\"AAAAAAAAAAAAAAAAAAAAAA==\"}";
        return $"{{\"version\":2,\"master\":{wrapper},\"recovery\":null,\"secondary\":null}}";
    }

    private sealed class ReachabilityCryptoService : ICryptoService
    {
        public int UnwrapCalls { get; private set; }

        public WrappedKeyEnvelope CreateWrappedKey(ReadOnlySpan<char> passphrase) => throw new NotSupportedException();
        public WrappedKeyEnvelope WrapKey(ReadOnlySpan<byte> dataKey, ReadOnlySpan<char> passphrase) => throw new NotSupportedException();

        public byte[] UnwrapKey(ReadOnlySpan<char> passphrase, WrappedKeyEnvelope envelope)
        {
            UnwrapCalls++;
            return new byte[32];
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
