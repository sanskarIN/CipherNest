namespace CipherNest.App.Services;

public sealed class SessionSecurityState
{
    private readonly object _gate = new();
    private DateTimeOffset? _lastMasterAuthenticationUtc;

    public void RecordMasterAuthentication(DateTimeOffset utcNow)
    {
        lock (_gate) _lastMasterAuthenticationUtc = utcNow;
    }

    public void Clear()
    {
        lock (_gate) _lastMasterAuthenticationUtc = null;
    }

    public bool RequiresMasterAuthentication(DateTimeOffset utcNow, TimeSpan maximumAge)
    {
        if (maximumAge <= TimeSpan.Zero) return true;
        lock (_gate)
        {
            if (_lastMasterAuthenticationUtc is null) return true;
            var age = utcNow - _lastMasterAuthenticationUtc.Value;
            return age < TimeSpan.Zero || age >= maximumAge;
        }
    }
}
