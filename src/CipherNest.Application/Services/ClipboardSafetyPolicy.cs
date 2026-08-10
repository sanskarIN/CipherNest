using System.Security.Cryptography;
using System.Text;

namespace CipherNest.Application.Services;

public static class ClipboardSafetyPolicy
{
    public const int FingerprintLength = 32;

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

    public static byte[] CreateFingerprint(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var utf8 = Encoding.UTF8.GetBytes(value);
        try
        {
            return SHA256.HashData(utf8);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(utf8);
        }
    }

    public static bool MatchesFingerprint(ReadOnlySpan<byte> expectedFingerprint, string? currentValue)
    {
        if (expectedFingerprint.Length != FingerprintLength || currentValue is null) return false;
        var actual = CreateFingerprint(currentValue);
        try
        {
            return CryptographicOperations.FixedTimeEquals(expectedFingerprint, actual);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(actual);
        }
    }
}
