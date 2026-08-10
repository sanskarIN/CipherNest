using System.Security.Cryptography;
using CipherNest.Application.Services;

namespace CipherNest.App.Services;

public sealed class ClipboardSecurityService(IPrivacySafeExceptionReporter exceptions) : IClipboardSecurityService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _clearCts;
    private byte[]? _lastCopiedFingerprint;
    private bool _disposed;

    public async Task CopySecretAsync(string value, TimeSpan clearAfter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(value);
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedDelay = ClipboardSafetyPolicy.NormalizeClearDelay(clearAfter);
        byte[]? fingerprint = ClipboardSafetyPolicy.CreateFingerprint(value);

        try
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                await Clipboard.Default.SetTextAsync(value);
                CancelScheduledClear();
                ClearTrackedFingerprint();
                _lastCopiedFingerprint = fingerprint;
                fingerprint = null;

                if (normalizedDelay > TimeSpan.Zero)
                {
                    _clearCts = new CancellationTokenSource();
                    _ = ClearLaterAsync(normalizedDelay, _clearCts.Token);
                }
            }
            finally
            {
                _gate.Release();
            }
        }
        finally
        {
            if (fingerprint is not null) CryptographicOperations.ZeroMemory(fingerprint);
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            await ClearTrackedClipboardIfPresentAsync().ConfigureAwait(false);
            CancelScheduledClear();
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelScheduledClear();
        ClearTrackedFingerprint();
        GC.SuppressFinalize(this);
    }

    private async Task ClearLaterAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_disposed) return;
                await ClearTrackedClipboardIfPresentAsync().ConfigureAwait(false);
                CancelScheduledClear();
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            exceptions.Report("Clipboard.ScheduledClear", exception);
        }
    }

    private async Task ClearTrackedClipboardIfPresentAsync()
    {
        if (_lastCopiedFingerprint is null) return;

        var current = await Clipboard.Default.GetTextAsync();
        if (ClipboardSafetyPolicy.MatchesFingerprint(_lastCopiedFingerprint, current))
        {
            await Clipboard.Default.SetTextAsync(string.Empty);
        }

        ClearTrackedFingerprint();
    }

    private void CancelScheduledClear()
    {
        _clearCts?.Cancel();
        _clearCts?.Dispose();
        _clearCts = null;
    }

    private void ClearTrackedFingerprint()
    {
        if (_lastCopiedFingerprint is null) return;
        CryptographicOperations.ZeroMemory(_lastCopiedFingerprint);
        _lastCopiedFingerprint = null;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}
