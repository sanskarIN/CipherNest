namespace CipherNest.App.Services;

public interface IScreenshotProtectionService
{
    bool IsSupported { get; }
    Task ApplyAsync(bool enabled, CancellationToken cancellationToken = default);
}
