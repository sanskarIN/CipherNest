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
            if (_failures < 5) return;
            var exponent = Math.Min(_failures - 5, 5);
            var seconds = Math.Min(300, 5 * (1 << exponent));
            _blockedUntil = now.AddSeconds(seconds);
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
