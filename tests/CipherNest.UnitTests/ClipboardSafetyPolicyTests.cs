using System.Security.Cryptography;
using CipherNest.Application.Services;

namespace CipherNest.UnitTests;

public sealed class ClipboardSafetyPolicyTests
{
    [Fact]
    public void Delay_IsBoundedAndZeroCanDisableScheduledClear()
    {
        Assert.Equal(TimeSpan.Zero, ClipboardSafetyPolicy.NormalizeClearDelay(TimeSpan.Zero));
        Assert.Equal(TimeSpan.FromSeconds(1), ClipboardSafetyPolicy.NormalizeClearDelay(TimeSpan.FromMilliseconds(100)));
        Assert.Equal(TimeSpan.FromSeconds(30), ClipboardSafetyPolicy.NormalizeClearDelay(TimeSpan.FromSeconds(30)));
        Assert.Equal(TimeSpan.FromMinutes(5), ClipboardSafetyPolicy.NormalizeClearDelay(TimeSpan.FromHours(1)));
    }

    [Fact]
    public void ClearOnlyWhenClipboardStillContainsCopiedValue()
    {
        Assert.True(ClipboardSafetyPolicy.ShouldClear("secret-a", "secret-a"));
        Assert.False(ClipboardSafetyPolicy.ShouldClear("secret-a", "secret-b"));
        Assert.False(ClipboardSafetyPolicy.ShouldClear("secret-a", null));
        Assert.False(ClipboardSafetyPolicy.ShouldClear("secret-a", "SECRET-A"));
    }

    [Fact]
    public void Fingerprint_IsFixedSizeAndMatchesOnlyExactClipboardValue()
    {
        var fingerprint = ClipboardSafetyPolicy.CreateFingerprint("secret-a");
        try
        {
            Assert.Equal(ClipboardSafetyPolicy.FingerprintLength, fingerprint.Length);
            Assert.True(ClipboardSafetyPolicy.MatchesFingerprint(fingerprint, "secret-a"));
            Assert.False(ClipboardSafetyPolicy.MatchesFingerprint(fingerprint, "secret-b"));
            Assert.False(ClipboardSafetyPolicy.MatchesFingerprint(fingerprint, "SECRET-A"));
            Assert.False(ClipboardSafetyPolicy.MatchesFingerprint(fingerprint, null));
            Assert.False(ClipboardSafetyPolicy.MatchesFingerprint(fingerprint.AsSpan(0, fingerprint.Length - 1), "secret-a"));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(fingerprint);
        }
    }
}
