using CipherNest.Application.Abstractions;
using CipherNest.App.Services;

namespace CipherNest.App.Views;

public partial class StartupPage : ContentPage
{
    private bool _navigated;

    public StartupPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_navigated) return;
        _navigated = true;
        try
        {
            var vault = ServiceProviderHelper.GetRequiredService<IVaultService>();
            var route = await vault.HasVaultAsync() ? "//unlock" : "//onboarding";
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            try { ServiceProviderHelper.GetRequiredService<IPrivacySafeExceptionReporter>().Report("Startup.Initialize", ex); }
            catch (InvalidOperationException)
            {
                // If dependency resolution itself failed, keep the fallback user-facing and do not emit raw exception details.
            }
            await DisplayAlertAsync("CipherNest could not start", "Local storage initialization could not be completed safely. Close and reopen CipherNest, then review troubleshooting guidance if the problem continues.", "OK");
            _navigated = false;
        }
    }
}
