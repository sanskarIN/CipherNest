namespace CipherNest.Application.Services;

public static class ClipboardSafetyPolicy
{
    public static TimeSpan NormalizeClearDelay(TimeSpan requested)
    {
        if (requested <= TimeSpan.Zero) return TimeSpan.Zero;
        var minimum = TimeSpan.FromSeconds(1);
        var maximum = TimeSpan.FromMinutes(5);
        return requested < minimum ? minimum : requested > maximum ? maximum : requested;
    }

    public static bool ShouldClear(string expectedValue, string? currentValue)
    {
        ArgumentNullException.ThrowIfNull(expectedValue);
        return string.Equals(expectedValue, currentValue, StringComparison.Ordinal);
    }
}
