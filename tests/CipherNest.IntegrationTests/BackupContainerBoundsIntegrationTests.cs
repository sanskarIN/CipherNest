using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class BackupContainerBoundsIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestBackupContainerBounds", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task RestoreEncryptedAsync_RejectsOversizedContainerBeforeParsing()
    {
        Directory.CreateDirectory(_directory);
        var sourcePath = Path.Combine(_directory, "oversized.cnbk");
        await using (var stream = new FileStream(sourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            stream.SetLength(BackupFormatPolicy.MaximumEncryptedContainerBytes + 1);

        var store = new SqliteVaultStore(Path.Combine(_directory, "vault.db"));
        var service = new EncryptedBackupService(store, new CryptoService());

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            service.RestoreEncryptedAsync(sourcePath, "Backup Passphrase 2026!"));

        Assert.Contains("exceeds the supported size limit", exception.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(store.DatabasePath));
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
