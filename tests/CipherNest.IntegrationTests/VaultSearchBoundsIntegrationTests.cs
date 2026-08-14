using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class VaultSearchBoundsIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestSearchBounds", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public VaultSearchBoundsIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task Search_AcceptsMaximumLengthQueryAndRejectsLongerInput()
    {
        const string master = "Search Bounds Test Master Passphrase 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);
        await vault.SaveItemAsync(new VaultItem { Id = Guid.NewGuid(), Title = "Example account" });

        var accepted = await vault.SearchAsync(new string('x', VaultService.MaximumSearchQueryCharacters));
        Assert.Empty(accepted);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            vault.SearchAsync(new string('x', VaultService.MaximumSearchQueryCharacters + 1)));
    }

    [Fact]
    public async Task Search_OversizedInputIsRejectedBeforeLockedVaultAccess()
    {
        const string master = "Search Ordering Test Master Passphrase 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);
        await vault.LockAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            vault.SearchAsync(new string('x', VaultService.MaximumSearchQueryCharacters + 1)));
    }

    [Fact]
    public async Task Search_WhitespaceOnlyInputRetainsNormalAllItemsBehavior()
    {
        const string master = "Search Whitespace Test Master Passphrase 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);
        await vault.SaveItemAsync(new VaultItem { Id = Guid.NewGuid(), Title = "Example account" });

        var results = await vault.SearchAsync("   ");

        Assert.Single(results);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
