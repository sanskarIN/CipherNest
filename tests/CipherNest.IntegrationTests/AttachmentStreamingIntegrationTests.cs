using System.Security.Cryptography;
using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class AttachmentStreamingIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestAttachmentTests", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public AttachmentStreamingIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task EightMegabyteAttachment_StreamsEncryptsAndDecryptsWithoutWholeFileBuffering()
    {
        const int size = 8 * 1024 * 1024;
        var inputPath = Path.Combine(_directory, "input.bin");
        var outputPath = Path.Combine(_directory, "output.bin");
        await CreatePatternFileAsync(inputPath, size);

        var store = new SqliteVaultStore(DatabasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync("Very Strong Attachment Master Passphrase 2026!", createRecoveryKey: false);
        var item = new VaultItem { Id = Guid.NewGuid(), Title = "Large document", Type = VaultItemType.Document };
        await vault.SaveItemAsync(item);

        AttachmentReference attachment;
        await using (var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
            attachment = await vault.AddAttachmentAsync(item.Id, input, "large-document.bin", "application/octet-stream");

        Assert.Equal(size, attachment.PlaintextLength);

        await using (var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
            await vault.ExportAttachmentAsync(item.Id, attachment.Id, output);

        var expectedHash = await HashFileAsync(inputPath);
        var actualHash = await HashFileAsync(outputPath);
        Assert.Equal(expectedHash, actualHash);
    }

    private static async Task CreatePatternFileAsync(string path, int bytes)
    {
        var buffer = new byte[128 * 1024];
        for (var i = 0; i < buffer.Length; i++) buffer[i] = (byte)((i * 31 + 17) & 0xff);

        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var remaining = bytes;
        while (remaining > 0)
        {
            var count = Math.Min(buffer.Length, remaining);
            await stream.WriteAsync(buffer.AsMemory(0, count));
            remaining -= count;
        }
        await stream.FlushAsync();
    }

    private static async Task<byte[]> HashFileAsync(string path)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await SHA256.HashDataAsync(stream);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }
}
