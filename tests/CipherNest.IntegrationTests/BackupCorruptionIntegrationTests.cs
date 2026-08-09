using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class BackupCorruptionIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestBackupCorruption", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public BackupCorruptionIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task TamperedBackup_IsRejectedBeforeReplacingCurrentVault()
    {
        const string master = "Very Strong Master Passphrase 2026!";
        const string backupPassphrase = "Separate Strong Backup Passphrase 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        var crypto = new CryptoService();
        using var vault = new VaultService(store, crypto, new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);
        await vault.SaveItemAsync(new VaultItem { Id = Guid.NewGuid(), Title = "Current vault survives", Secret = "unique-test-secret" });

        var backupPath = Path.Combine(_directory, "backup.cnbak");
        var backup = new EncryptedBackupService(store, crypto);
        await backup.ExportEncryptedAsync(backupPath, backupPassphrase);
        await vault.LockAsync();

        var bytes = await File.ReadAllBytesAsync(backupPath);
        Assert.True(bytes.Length > 128);
        bytes[bytes.Length / 2] ^= 0x40;
        await File.WriteAllBytesAsync(backupPath, bytes);

        await Assert.ThrowsAsync<InvalidDataException>(() => backup.RestoreEncryptedAsync(backupPath, backupPassphrase));

        await vault.UnlockAsync(master);
        var items = await vault.GetItemsAsync();
        var item = Assert.Single(items);
        Assert.Equal("Current vault survives", item.Title);
        Assert.Equal("unique-test-secret", item.Secret);
    }

    [Fact]
    public async Task WrongBackupPassphrase_IsRejectedBeforeReplacingCurrentVault()
    {
        const string master = "Another Very Strong Master Passphrase 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        var crypto = new CryptoService();
        using var vault = new VaultService(store, crypto, new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);
        await vault.SaveItemAsync(new VaultItem { Id = Guid.NewGuid(), Title = "Still here", Secret = "second-test-secret" });

        var backupPath = Path.Combine(_directory, "backup-wrong-pass.cnbak");
        var backup = new EncryptedBackupService(store, crypto);
        await backup.ExportEncryptedAsync(backupPath, "Correct Backup Passphrase 2026!");
        await vault.LockAsync();

        await Assert.ThrowsAsync<InvalidDataException>(() => backup.RestoreEncryptedAsync(backupPath, "Incorrect Backup Passphrase 2026!"));

        await vault.UnlockAsync(master);
        Assert.Equal("Still here", Assert.Single(await vault.GetItemsAsync()).Title);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }
}
