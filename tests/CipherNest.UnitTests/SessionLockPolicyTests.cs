using CipherNest.Application.Services;
using CipherNest.Domain.Models;

namespace CipherNest.UnitTests;

public sealed class SessionLockPolicyTests
{
    private readonly SessionLockPolicy _policy = new();

    [Fact]
    public void BackgroundPolicy_LocksOnlyWhenEnabledAndUnlocked()
    {
        var preferences = new AppPreferences { LockOnBackground = true };
        Assert.True(_policy.ShouldLockWhenBackgrounded(preferences, vaultIsUnlocked: true));
        Assert.False(_policy.ShouldLockWhenBackgrounded(preferences, vaultIsUnlocked: false));
        Assert.False(_policy.ShouldLockWhenBackgrounded(preferences with { LockOnBackground = false }, vaultIsUnlocked: true));
    }

    [Fact]
    public void InactivityPolicy_LocksAtConfiguredTimeout()
    {
        var preferences = new AppPreferences { LockTimeoutSeconds = 60, LockOnBackground = false };
        var inactive = new DateTimeOffset(2026, 8, 9, 6, 0, 0, TimeSpan.Zero);

        Assert.False(_policy.ShouldLockAfterInactivity(preferences, true, inactive, inactive.AddSeconds(59)));
        Assert.True(_policy.ShouldLockAfterInactivity(preferences, true, inactive, inactive.AddSeconds(60)));
    }

    [Fact]
    public void InactivityPolicy_FailsClosedOnClockRollback()
    {
        var preferences = new AppPreferences { LockTimeoutSeconds = 60 };
        var inactive = new DateTimeOffset(2026, 8, 9, 6, 0, 0, TimeSpan.Zero);
        Assert.True(_policy.ShouldLockAfterInactivity(preferences, true, inactive, inactive.AddSeconds(-1)));
    }

    [Fact]
    public void InactivityPolicy_DoesNotLockWithoutInactiveTimestampOrUnlockedVault()
    {
        var preferences = new AppPreferences { LockTimeoutSeconds = 5 };
        var now = DateTimeOffset.UtcNow;
        Assert.False(_policy.ShouldLockAfterInactivity(preferences, true, null, now));
        Assert.False(_policy.ShouldLockAfterInactivity(preferences, false, now.AddHours(-1), now));
    }
}
