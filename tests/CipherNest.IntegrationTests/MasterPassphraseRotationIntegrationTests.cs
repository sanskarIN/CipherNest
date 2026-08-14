using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class MasterPassphraseRotationIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestMasterRotationTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ChangeMasterPassphraseAsync_RejectsWhitespaceOnlyReplacementAndPreservesCurrentMaster()
    {
        Directory.CreateDirectory(_directory);
        const string currentMaster = "Current Master Passphrase 2026!";
        var store = new SqliteVaultStore(Path.Combine(_directory, "vault.db"));
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync(currentMaster, createRecoveryKey: false);

        var blankReplacement = new string(' ', CryptoService.MinimumPassphraseCharacters);
        await Assert.ThrowsAsync<ArgumentException>(() => vault.ChangeMasterPassphraseAsync(currentMaster, blankReplacement));

        await vault.LockAsync();
        await vault.UnlockAsync(currentMaster);
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
    }
}
