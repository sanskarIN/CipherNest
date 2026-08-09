namespace CipherNest.App.Services;

public sealed class ScreenshotProtectionService : IScreenshotProtectionService
{
#if ANDROID
    public bool IsSupported => true;
#else
    public bool IsSupported => false;
#endif

    public Task ApplyAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity?.Window is not null)
        {
            if (enabled)
            {
                activity.Window.SetFlags(Android.Views.WindowManagerFlags.Secure, Android.Views.WindowManagerFlags.Secure);
            }
            else
            {
                activity.Window.ClearFlags(Android.Views.WindowManagerFlags.Secure);
            }
        }
#endif
        return Task.CompletedTask;
    }
}
