using CipherNest.Application.Abstractions;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class BackupRollbackCancellationIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestBackupRollback", Guid.NewGuid().ToString("N"));

    public BackupRollbackCancellationIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task RestoreCancellation_UsesUncancelledTokenForDatabaseRollback()
    {
        const string master = "Strong Master Passphrase For Rollback 2026!";
        const string backupPassphrase = "Independent Backup Passphrase For Rollback 2026!";
        var sourceRoot = Path.Combine(_directory, "source");
        Directory.CreateDirectory(sourceRoot);
        var sourceStore = new SqliteVaultStore(Path.Combine(sourceRoot, "vault.db"));
        var crypto = new CryptoService();
        using (var vault = new VaultService(sourceStore, crypto, new SystemClock()))
        {
            await vault.CreateAsync(master, createRecoveryKey: false);
        }

        var backupPath = Path.Combine(_directory, "rollback-test.cnbak");
        await new EncryptedBackupService(sourceStore, crypto).ExportEncryptedAsync(backupPath, backupPassphrase);

        using var cancellation = new CancellationTokenSource();
        var restoreRoot = Path.Combine(_directory, "restore-target");
        Directory.CreateDirectory(restoreRoot);
        var fakeStore = new CancellingReplacementStore(Path.Combine(restoreRoot, "vault.db"), cancellation);
        var service = new EncryptedBackupService(fakeStore, crypto);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.RestoreEncryptedAsync(backupPath, backupPassphrase, cancellation.Token));

        Assert.Equal(2, fakeStore.ReplaceCalls);
        Assert.True(fakeStore.FirstReplacementTokenCanBeCanceled);
        Assert.False(fakeStore.RollbackTokenCanBeCanceled);
        Assert.False(fakeStore.RollbackTokenWasCancellationRequested);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }

    private sealed class CancellingReplacementStore(string databasePath, CancellationTokenSource cancellation) : IVaultStore
    {
        public string DatabasePath { get; } = databasePath;
        public int ReplaceCalls { get; private set; }
        public bool FirstReplacementTokenCanBeCanceled { get; private set; }
        public bool RollbackTokenCanBeCanceled { get; private set; }
        public bool RollbackTokenWasCancellationRequested { get; private set; }

        public Task CreateConsistentSnapshotAsync(string destinationDatabasePath, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationDatabasePath)!);
            File.WriteAllBytes(destinationDatabasePath, [0x43, 0x4E]);
            return Task.CompletedTask;
        }

        public Task ReplaceDatabaseAsync(string sourceDatabasePath, CancellationToken cancellationToken = default)
        {
            ReplaceCalls++;
            if (ReplaceCalls == 1)
            {
                FirstReplacementTokenCanBeCanceled = cancellationToken.CanBeCanceled;
                cancellation.Cancel();
                throw new OperationCanceledException(cancellationToken);
            }

            RollbackTokenCanBeCanceled = cancellationToken.CanBeCanceled;
            RollbackTokenWasCancellationRequested = cancellationToken.IsCancellationRequested;
            return Task.CompletedTask;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> HasVaultAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<string?> ReadHeaderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task WriteHeaderAsync(string headerJson, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<StoredVaultItem>> ReadAllItemsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<StoredVaultItem>>([]);
        public Task UpsertItemAsync(StoredVaultItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteDatabaseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
