using System.Security.Cryptography;
using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class VaultLockCancellationIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestLockCancellation", Guid.NewGuid().ToString("N"));

    public VaultLockCancellationIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task Lock_CancelsInFlightDecryptedAttachmentExport()
    {
        var databasePath = Path.Combine(_directory, "vault.db");
        var store = new SqliteVaultStore(databasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync("Lock Cancellation Master Passphrase 2026!", createRecoveryKey: false);

        var itemId = Guid.NewGuid();
        await vault.SaveItemAsync(new VaultItem { Id = itemId, Title = "Attachment export cancellation" });
        var payload = RandomNumberGenerator.GetBytes(600_000);
        await using var source = new MemoryStream(payload, writable: false);
        var attachment = await vault.AddAttachmentAsync(itemId, source, "sample.bin", "application/octet-stream");
        CryptographicOperations.ZeroMemory(payload);

        await using var destination = new BlockingWriteStream();
        var exportTask = vault.ExportAttachmentAsync(itemId, attachment.Id, destination);
        await destination.WriteStarted.WaitAsync(TimeSpan.FromSeconds(10));

        await vault.LockAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await exportTask);
        Assert.False(vault.IsUnlocked);
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
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class BlockingWriteStream : MemoryStream
    {
        private readonly TaskCompletionSource<bool> _writeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task WriteStarted => _writeStarted.Task;

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _writeStarted.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }
}
