using CipherNest.Domain.Models;

namespace CipherNest.Application.Services;

public sealed class SessionLockPolicy
{
    public bool ShouldLockWhenBackgrounded(AppPreferences preferences, bool vaultIsUnlocked)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return vaultIsUnlocked && preferences.LockOnBackground;
    }

    public bool ShouldLockAfterInactivity(AppPreferences preferences, bool vaultIsUnlocked, DateTimeOffset? inactiveUtc, DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (!vaultIsUnlocked || inactiveUtc is null) return false;
        var timeoutSeconds = Math.Clamp(preferences.LockTimeoutSeconds, 5, 3600);
        if (nowUtc < inactiveUtc.Value) return true;
        return nowUtc - inactiveUtc.Value >= TimeSpan.FromSeconds(timeoutSeconds);
    }
}
