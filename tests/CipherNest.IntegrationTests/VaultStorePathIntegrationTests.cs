using CipherNest.Infrastructure.Persistence;

namespace CipherNest.IntegrationTests;

public sealed class VaultStorePathIntegrationTests
{
    [Fact]
    public async Task BareRelativeDatabasePath_IsCanonicalizedAndInitializes()
    {
        var relativePath = $"ciphernest-vault-{Guid.NewGuid():N}.db";
        var fullPath = Path.GetFullPath(relativePath);
        try
        {
            var store = new SqliteVaultStore(relativePath);

            Assert.Equal(fullPath, store.DatabasePath);
            await store.InitializeAsync();
            Assert.True(File.Exists(fullPath));
        }
        finally
        {
            foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
            {
                try
                {
                    var path = fullPath + suffix;
                    if (File.Exists(path)) File.Delete(path);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    [Fact]
    public void Constructor_RejectsWhitespaceDatabasePath()
    {
        Assert.Throws<ArgumentException>(() => new SqliteVaultStore("   "));
    }
}
