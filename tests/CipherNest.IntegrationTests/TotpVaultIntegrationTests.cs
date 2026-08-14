using System.Text;
using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class TotpVaultIntegrationTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestTotpTests", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try { Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task TotpItem_RoundTripsThroughEncryptedVault_WithoutPlaintextSeedInEnvelope()
    {
        const string masterPassphrase = "Synthetic Strong TOTP Vault Passphrase 2026!";
        const string seed = "JBSWY3DPEHPK3PXP";
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync(masterPassphrase, createRecoveryKey: false);

        var id = Guid.NewGuid();
        await vault.SaveItemAsync(new VaultItem
        {
            Id = id,
            Type = VaultItemType.OneTimePassword,
            Title = "Synthetic OTP",
            Username = "demo@example.test",
            Secret = seed,
            TotpAlgorithm = TotpAlgorithm.Sha256,
            TotpDigits = 8,
            TotpPeriodSeconds = 60
        });

        var stored = Assert.Single((await store.ReadAllItemsAsync()).Where(record => record.Id == id));
        var seedBytes = Encoding.UTF8.GetBytes(seed);
        try
        {
            Assert.Equal(-1, stored.Envelope.AsSpan().IndexOf(seedBytes));
        }
        finally
        {
            Array.Clear(seedBytes);
        }

        await vault.LockAsync();
        await vault.UnlockAsync(masterPassphrase);
        var restored = await vault.GetItemAsync(id);

        Assert.NotNull(restored);
        Assert.Equal(VaultItemType.OneTimePassword, restored.Type);
        Assert.Equal("Synthetic OTP", restored.Title);
        Assert.Equal("demo@example.test", restored.Username);
        Assert.Equal(seed, restored.Secret);
        Assert.Equal(TotpAlgorithm.Sha256, restored.TotpAlgorithm);
        Assert.Equal(8, restored.TotpDigits);
        Assert.Equal(60, restored.TotpPeriodSeconds);
    }
}
