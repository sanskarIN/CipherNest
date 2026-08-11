using System.Text.Json.Nodes;
using CipherNest.Application.Exceptions;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class VaultHeaderCompatibilityIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestHeaderCompatibility", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public VaultHeaderCompatibilityIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task FutureVaultHeaderVersion_IsRejectedBeforeUnlock()
    {
        const string master = "Future Header Test Master Passphrase 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);
        await vault.LockAsync();

        var headerJson = await store.ReadHeaderAsync();
        Assert.NotNull(headerJson);
        var header = JsonNode.Parse(headerJson!]?.AsObject() ?? throw new InvalidDataException("Header JSON could not be parsed in the test.");
        header["version"] = 999;
        await store.WriteHeaderAsync(header.ToJsonString());

        await Assert.ThrowsAsync<VaultAuthenticationException>(() => vault.UnlockAsync(master));
        Assert.False(vault.IsUnlocked);
    }

    [Fact]
    public async Task MalformedVaultHeaderJson_IsRejectedAsAuthenticationFailure()
    {
        const string master = "Malformed Header Test Master Passphrase 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);
        await vault.LockAsync();
        await store.WriteHeaderAsync("{\"version\":2,\"master\":");

        await Assert.ThrowsAsync<VaultAuthenticationException>(() => vault.UnlockAsync(master));
        Assert.False(vault.IsUnlocked);
    }

    [Fact]
    public async Task CurrentVaultHeaderVersion_RemainsUnlockable()
    {
        const string master = "Current Header Test Master Passphrase 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);
        await vault.LockAsync();

        await vault.UnlockAsync(master);

        Assert.True(vault.IsUnlocked);
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
