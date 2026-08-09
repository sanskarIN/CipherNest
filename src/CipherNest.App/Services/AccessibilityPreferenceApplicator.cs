namespace CipherNest.App.Services;

public static class AccessibilityPreferenceApplicator
{
    public static void Apply(bool largerInterface, bool reducedMotion)
    {
        if (Microsoft.Maui.Controls.Application.Current is null) return;
        var resources = Microsoft.Maui.Controls.Application.Current.Resources;
        resources["BodyFontSize"] = largerInterface ? 18d : 15d;
        resources["CaptionFontSize"] = largerInterface ? 17d : 14d;
        resources["TitleFontSize"] = largerInterface ? 34d : 30d;
        resources["ControlFontSize"] = largerInterface ? 18d : 15d;
        resources["ReducedMotionEnabled"] = reducedMotion;
    }
}
