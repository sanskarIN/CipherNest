namespace CipherNest.App.Services;

public static class BuildFeatureFlags
{
#if CIPHERNEST_DISABLE_FUNDING_LINK
    public static bool IsFundingLinkEnabled => false;
#else
    public static bool IsFundingLinkEnabled => true;
#endif
}
