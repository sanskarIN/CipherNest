namespace CipherNest.App.Services;

public static class BuildFeatureFlags
{
#if CIPHERNEST_DISABLE_FUNDING_LINK
    public const bool IsFundingLinkEnabled = false;
#else
    public const bool IsFundingLinkEnabled = true;
#endif
}
