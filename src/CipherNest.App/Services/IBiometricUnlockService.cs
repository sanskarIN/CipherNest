namespace CipherNest.App.Services;

public interface IBiometricUnlockService
{
    bool IsSupported { get; }
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken = default);
    Task StoreSecondarySecretAsync(string secret, CancellationToken cancellationToken = default);
    Task<string?> ReadSecondarySecretAsync(CancellationToken cancellationToken = default);
    Task ClearSecondarySecretAsync(CancellationToken cancellationToken = default);
}
