using System.Security.Cryptography;

namespace CipherNest.Infrastructure.Services;

internal sealed class VaultKeyLease : IDisposable
{
    private readonly CancellationTokenSource _linkedCancellation;
    private bool _disposed;

    public VaultKeyLease(byte[] keyCopy, CancellationToken sessionToken, CancellationToken callerToken)
    {
        ArgumentNullException.ThrowIfNull(keyCopy);
        if (keyCopy.Length != 32)
        {
            CryptographicOperations.ZeroMemory(keyCopy);
            throw new ArgumentException("Vault key lease requires a 256-bit key copy.", nameof(keyCopy));
        }
        Key = keyCopy;
        _linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(sessionToken, callerToken);
    }

    public byte[] Key { get; }
    public CancellationToken Token => _linkedCancellation.Token;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CryptographicOperations.ZeroMemory(Key);
        _linkedCancellation.Dispose();
    }
}
