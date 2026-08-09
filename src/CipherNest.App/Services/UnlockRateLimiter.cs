using CipherNest.Application.Services;

namespace CipherNest.App.Services;

public sealed class UnlockRateLimiter
{
    private readonly object _sync = new();
    private int _failures;
    private DateTimeOffset _blockedUntil;

    public TimeSpan GetRemainingDelay(DateTimeOffset now)
    {
        lock (_sync)
        {
            return _blockedUntil > now ? _blockedUntil - now : TimeSpan.Zero;
        }
    }

    public void RegisterFailure(DateTimeOffset now)
    {
        lock (_sync)
        {
            _failures++;
            var delay = UnlockBackoffPolicy.DelayAfterFailureCount(_failures);
            if (delay > TimeSpan.Zero) _blockedUntil = now.Add(delay);
        }
    }

    public void RegisterSuccess()
    {
        lock (_sync)
        {
            _failures = 0;
            _blockedUntil = default;
        }
    }
}
