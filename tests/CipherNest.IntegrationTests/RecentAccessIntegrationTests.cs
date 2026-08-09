using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class RecentAccessIntegrationTests : IAsyncLifetime
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
    public async Task MarkAccessed_ChangesAccessTimeWithoutChangingModifiedTime()
    {
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync("Very Strong Master Passphrase 2026!", false);

        var item = new VaultItem { Title = "Recently used", Secret = "example" };
        await vault.SaveItemAsync(item);
        var before = Assert.Single(await vault.GetItemsAsync());

        await vault.MarkAccessedAsync(before.Id);
        var after = Assert.Single(await vault.GetItemsAsync());

        Assert.NotNull(after.LastAccessedUtc);
        Assert.Equal(before.ModifiedUtc, after.ModifiedUtc);
    }
}
