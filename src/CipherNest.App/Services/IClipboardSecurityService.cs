namespace CipherNest.App.Services;

public interface IClipboardSecurityService
{
    Task CopySecretAsync(string value, TimeSpan clearAfter, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
