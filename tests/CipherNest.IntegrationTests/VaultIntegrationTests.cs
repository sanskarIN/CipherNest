using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class VaultIntegrationTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestTests", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public Task InitializeAsync() { Directory.CreateDirectory(_directory); return Task.CompletedTask; }
    public Task DisposeAsync() { try { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); } catch (IOException) { } return Task.CompletedTask; }

    [Fact]
    public async Task Create_Save_Lock_Unlock_Search_RoundTrips()
    {
        var store = new SqliteVaultStore(DatabasePath);
        var crypto = new CryptoService();
        using var vault = new VaultService(store, crypto, new SystemClock());
        var recovery = await vault.CreateAsync("Very Strong Master Passphrase 2026!", true);
        Assert.NotNull(recovery);
        var item = new VaultItem { Title = "Example", Username = "user@example.test", Secret = "unique-secret-value", Tags = ["work"] };
        await vault.SaveItemAsync(item);
        await vault.LockAsync();
        Assert.False(vault.IsUnlocked);
        await vault.UnlockAsync("Very Strong Master Passphrase 2026!");
        var results = await vault.SearchAsync("example.test");
        Assert.Single(results);
        Assert.Equal(item.Secret, results[0].Secret);
        await vault.LockAsync();
        await vault.UnlockAsync(recovery!);
        Assert.True(vault.IsUnlocked);
    }

    [Fact]
    public async Task EncryptedBackup_RestoresValidatedDatabase()
    {
        var store = new SqliteVaultStore(DatabasePath);
        var crypto = new CryptoService();
        using var vault = new VaultService(store, crypto, new SystemClock());
        await vault.CreateAsync("Very Strong Master Passphrase 2026!", false);
        await vault.SaveItemAsync(new VaultItem { Title = "Before backup", Secret = "secret" });
        var backup = Path.Combine(_directory, "backup.cnbak");
        var service = new EncryptedBackupService(store, crypto);
        await service.ExportEncryptedAsync(backup, "Separate Strong Backup Passphrase 2026!");
        await vault.SaveItemAsync(new VaultItem { Title = "After backup", Secret = "other" });
        await vault.LockAsync();
        await service.RestoreEncryptedAsync(backup, "Separate Strong Backup Passphrase 2026!");
        await vault.UnlockAsync("Very Strong Master Passphrase 2026!");
        var items = await vault.GetItemsAsync();
        Assert.Single(items);
        Assert.Equal("Before backup", items[0].Title);
    }
}
