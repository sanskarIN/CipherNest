namespace CipherNest.App.Services;

public sealed class ClipboardSecurityService : IClipboardSecurityService, IDisposable
{
    private CancellationTokenSource? _clearCts;

    public async Task CopySecretAsync(string value, TimeSpan clearAfter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        _clearCts?.Cancel();
        _clearCts?.Dispose();
        _clearCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _clearCts.Token;
        await Clipboard.Default.SetTextAsync(value);
        if (clearAfter <= TimeSpan.Zero) return;
        _ = ClearLaterAsync(value, clearAfter, token);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _clearCts?.Cancel();
        var current = await Clipboard.Default.GetTextAsync();
        if (!string.IsNullOrEmpty(current))
        {
            await Clipboard.Default.SetTextAsync(string.Empty);
        }
    }

    public void Dispose()
    {
        _clearCts?.Cancel();
        _clearCts?.Dispose();
    }

    private static async Task ClearLaterAsync(string expected, TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            var current = await Clipboard.Default.GetTextAsync();
            if (string.Equals(current, expected, StringComparison.Ordinal))
            {
                await Clipboard.Default.SetTextAsync(string.Empty);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
