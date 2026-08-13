using CipherNest.Application.Abstractions;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class VaultSessionTransitionIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestSessionTransition", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public VaultSessionTransitionIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task LockRequestedDuringUnlock_CompletesAfterUnlockAndLeavesVaultLocked()
    {
        const string master = "Serialized Session Transition Test 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        using var crypto = new BlockingUnwrapCryptoService();
        using var vault = new VaultService(store, crypto, new SystemClock());

        await vault.CreateAsync(master, createRecoveryKey: false);
        await vault.LockAsync();
        crypto.BlockNextUnwrap();

        var unlockTask = Task.Run(() => vault.UnlockAsync(master));
        await crypto.WaitForBlockedUnwrapAsync().WaitAsync(TimeSpan.FromSeconds(10));

        var lockTask = Task.Run(() => vault.LockAsync());
        Assert.False(lockTask.IsCompleted);

        crypto.ReleaseBlockedUnwrap();
        await Task.WhenAll(unlockTask, lockTask).WaitAsync(TimeSpan.FromSeconds(20));

        Assert.False(vault.IsUnlocked);
    }

    private sealed class BlockingUnwrapCryptoService : ICryptoService, IDisposable
    {
        private readonly CryptoService _inner = new();
        private readonly object _sync = new();
        private TaskCompletionSource<bool>? _entered;
        private ManualResetEventSlim? _release;
        private bool _blockNext;

        public void BlockNextUnwrap()
        {
            lock (_sync)
            {
                _entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _release = new ManualResetEventSlim(false);
                _blockNext = true;
            }
        }

        public Task WaitForBlockedUnwrapAsync()
        {
            lock (_sync) return (_entered ?? throw new InvalidOperationException("Blocking unwrap was not configured.")).Task;
        }

        public void ReleaseBlockedUnwrap()
        {
            lock (_sync) (_release ?? throw new InvalidOperationException("Blocking unwrap was not configured.")).Set();
        }

        public WrappedKeyEnvelope CreateWrappedKey(ReadOnlySpan<char> passphrase) => _inner.CreateWrappedKey(passphrase);
        public WrappedKeyEnvelope WrapKey(ReadOnlySpan<byte> dataKey, ReadOnlySpan<char> passphrase) => _inner.WrapKey(dataKey, passphrase);

        public byte[] UnwrapKey(ReadOnlySpan<char> passphrase, WrappedKeyEnvelope envelope)
        {
            TaskCompletionSource<bool>? entered = null;
            ManualResetEventSlim? release = null;
            lock (_sync)
            {
                if (_blockNext)
                {
                    _blockNext = false;
                    entered = _entered;
                    release = _release;
                }
            }

            if (release is not null)
            {
                entered?.TrySetResult(true);
                release.Wait();
            }

            return _inner.UnwrapKey(passphrase, envelope);
        }

        public EncryptedEnvelope Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData) => _inner.Encrypt(plaintext, key, associatedData);
        public byte[] Decrypt(EncryptedEnvelope envelope, ReadOnlySpan<byte> key, ReadOnlySpan<byte> associatedData) => _inner.Decrypt(envelope, key, associatedData);
        public byte[] DeriveKey(ReadOnlySpan<char> passphrase, ReadOnlySpan<byte> salt, KdfParameters parameters) => _inner.DeriveKey(passphrase, salt, parameters);

        public void Dispose()
        {
            lock (_sync)
            {
                _release?.Set();
                _release?.Dispose();
                _release = null;
                _entered = null;
            }
        }
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
}
