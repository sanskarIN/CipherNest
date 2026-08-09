namespace CipherNest.Application.Services;

public static class UnlockBackoffPolicy
{
    public static TimeSpan DelayAfterFailureCount(int failureCount)
    {
        if (failureCount < 5) return TimeSpan.Zero;
        var exponent = Math.Min(failureCount - 5, 5);
        var seconds = Math.Min(300, 5 * (1 << exponent));
        return TimeSpan.FromSeconds(seconds);
    }
}
