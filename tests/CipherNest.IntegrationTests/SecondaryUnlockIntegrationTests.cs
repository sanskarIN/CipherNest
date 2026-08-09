using CipherNest.Application.Exceptions;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class SecondaryUnlockIntegrationTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestTests", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
        catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SecondarySecret_CanUnlockAfterMasterConfirmedEnable()
    {
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        const string master = "Very Strong Master Passphrase 2026!";
        const string secondary = "secondary-secret-with-more-than-thirty-two-characters-2026";

        await vault.CreateAsync(master, false);
        await vault.EnableSecondaryUnlockAsync(master, secondary);
        Assert.True(await vault.IsSecondaryUnlockConfiguredAsync());

        await vault.LockAsync();
        await vault.UnlockWithSecondarySecretAsync(secondary);
        Assert.True(vault.IsUnlocked);
    }

    [Fact]
    public async Task WrongSecondarySecret_IsRejected()
    {
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        const string master = "Very Strong Master Passphrase 2026!";
        const string secondary = "secondary-secret-with-more-than-thirty-two-characters-2026";

        await vault.CreateAsync(master, false);
        await vault.EnableSecondaryUnlockAsync(master, secondary);
        await vault.LockAsync();

        await Assert.ThrowsAsync<VaultAuthenticationException>(() => vault.UnlockWithSecondarySecretAsync("wrong-secondary-secret-with-more-than-thirty-two-characters"));
        Assert.False(vault.IsUnlocked);
    }

    [Fact]
    public async Task DisableSecondaryUnlock_RequiresMasterAndRemovesWrapper()
    {
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        const string master = "Very Strong Master Passphrase 2026!";
        const string secondary = "secondary-secret-with-more-than-thirty-two-characters-2026";

        await vault.CreateAsync(master, false);
        await vault.EnableSecondaryUnlockAsync(master, secondary);
        await Assert.ThrowsAsync<VaultAuthenticationException>(() => vault.DisableSecondaryUnlockAsync("Wrong master passphrase"));
        Assert.True(await vault.IsSecondaryUnlockConfiguredAsync());

        await vault.DisableSecondaryUnlockAsync(master);
        Assert.False(await vault.IsSecondaryUnlockConfiguredAsync());
        await vault.LockAsync();
        await Assert.ThrowsAsync<VaultAuthenticationException>(() => vault.UnlockWithSecondarySecretAsync(secondary));
    }
}
