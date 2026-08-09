namespace CipherNest.App.ViewModels;

public partial class SettingsViewModel
{
    partial void OnLargerInterfaceChanged(bool value) => ApplyInterfaceScale(value);

    partial void OnReducedMotionChanged(bool value)
    {
        if (Microsoft.Maui.Controls.Application.Current is null) return;
        Microsoft.Maui.Controls.Application.Current.Resources["ReducedMotionEnabled"] = value;
    }

    private static void ApplyInterfaceScale(bool larger)
    {
        if (Microsoft.Maui.Controls.Application.Current is null) return;
        var resources = Microsoft.Maui.Controls.Application.Current.Resources;
        resources["BodyFontSize"] = larger ? 18d : 15d;
        resources["CaptionFontSize"] = larger ? 17d : 14d;
        resources["TitleFontSize"] = larger ? 34d : 30d;
        resources["ControlFontSize"] = larger ? 18d : 15d;
    }
}
