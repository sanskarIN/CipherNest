using System.Security.Cryptography;
using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;
using CipherNest.Shared;

namespace CipherNest.IntegrationTests;

public sealed class AttachmentTamperIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestAttachmentTamper", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public AttachmentTamperIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task ModifiedCiphertext_IsRejected()
    {
        var (vault, itemId, attachment, encryptedPath) = await CreateAttachmentAsync();
        using (vault)
        {
            var bytes = await File.ReadAllBytesAsync(encryptedPath);
            Assert.True(bytes.Length > 80);
            bytes[^10] ^= 0x20;
            await File.WriteAllBytesAsync(encryptedPath, bytes);

            await using var output = new MemoryStream();
            await Assert.ThrowsAnyAsync<CryptographicException>(() => vault.ExportAttachmentAsync(itemId, attachment.Id, output));
        }
    }

    [Fact]
    public async Task TruncatedContainer_IsRejected()
    {
        var (vault, itemId, attachment, encryptedPath) = await CreateAttachmentAsync();
        using (vault)
        {
            var bytes = await File.ReadAllBytesAsync(encryptedPath);
            Assert.True(bytes.Length > 32);
            await File.WriteAllBytesAsync(encryptedPath, bytes[..^7]);

            await using var output = new MemoryStream();
            await Assert.ThrowsAsync<EndOfStreamException>(() => vault.ExportAttachmentAsync(itemId, attachment.Id, output));
        }
    }

    private async Task<(VaultService Vault, Guid ItemId, AttachmentReference Attachment, string EncryptedPath)> CreateAttachmentAsync()
    {
        var store = new SqliteVaultStore(DatabasePath);
        var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync("Very Strong Attachment Master Passphrase 2026!", createRecoveryKey: false);
        var item = new VaultItem { Id = Guid.NewGuid(), Title = "Attachment test", Type = VaultItemType.Document };
        await vault.SaveItemAsync(item);
        await using var source = new MemoryStream(Enumerable.Range(0, 8192).Select(static index => (byte)(index & 0xff)).ToArray(), writable: false);
        var attachment = await vault.AddAttachmentAsync(item.Id, source, "test.bin", "application/octet-stream");
        var path = Path.Combine(_directory, AppConstants.AttachmentDirectoryName, attachment.EncryptedFileName);
        return (vault, item.Id, attachment, path);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }
}
