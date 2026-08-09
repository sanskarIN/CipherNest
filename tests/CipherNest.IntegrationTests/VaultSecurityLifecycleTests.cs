using CipherNest.Application.Exceptions;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class VaultSecurityLifecycleTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestLifecycleTests", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");
    public Task InitializeAsync() { Directory.CreateDirectory(_directory); return Task.CompletedTask; }
    public Task DisposeAsync() { try { Directory.Delete(_directory, true); } catch (IOException) { } return Task.CompletedTask; }

    [Fact]
    public async Task ChangeMasterPassphrase_PreservesRecovery_AndRejectsOldMaster()
    {
        using var vault = new VaultService(new SqliteVaultStore(DatabasePath), new CryptoService(), new SystemClock());
        var recovery = await vault.CreateAsync("Initial Very Strong Master Passphrase 2026!", true);
        await vault.ChangeMasterPassphraseAsync("Initial Very Strong Master Passphrase 2026!", "Replacement Very Strong Master Passphrase 2026!");
        await vault.LockAsync();
        await Assert.ThrowsAsync<VaultAuthenticationException>(() => vault.UnlockAsync("Initial Very Strong Master Passphrase 2026!"));
        await vault.UnlockAsync("Replacement Very Strong Master Passphrase 2026!");
        await vault.LockAsync();
        await vault.UnlockAsync(recovery!);
        Assert.True(vault.IsUnlocked);
    }

    [Fact]
    public async Task DeleteVault_RequiresMaster_AndRemovesDatabase()
    {
        using var vault = new VaultService(new SqliteVaultStore(DatabasePath), new CryptoService(), new SystemClock());
        await vault.CreateAsync("Initial Very Strong Master Passphrase 2026!", false);
        await Assert.ThrowsAsync<VaultAuthenticationException>(() => vault.DeleteVaultAsync("Wrong but sufficiently long passphrase!"));
        Assert.True(File.Exists(DatabasePath));
        await vault.DeleteVaultAsync("Initial Very Strong Master Passphrase 2026!");
        Assert.False(File.Exists(DatabasePath));
        Assert.False(vault.IsUnlocked);
    }
}
