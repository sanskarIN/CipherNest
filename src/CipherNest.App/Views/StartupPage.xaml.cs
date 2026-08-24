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
            // Restore retry state before any secondary UI work so an alert/dispatcher failure cannot leave startup permanently stuck.
            _navigated = false;
            ReportSafely("Startup.Initialize", ex);

            try
            {
                await DisplayAlertAsync(
                    "CipherNest could not start",
                    "Local storage initialization could not be completed safely. Close and reopen CipherNest, then review troubleshooting guidance if the problem continues.",
                    "OK");
            }
            catch (Exception alertException)
            {
                // OnAppearing is async void. Contain secondary alert failures so they cannot escape the native lifecycle callback.
                ReportSafely("Startup.Initialize.Alert", alertException);
            }
        }
    }

    private static void ReportSafely(string operation, Exception exception)
    {
        try
        {
            ServiceProviderHelper.GetRequiredService<IPrivacySafeExceptionReporter>().Report(operation, exception);
        }
        catch
        {
            // Diagnostics are best-effort here. Never let reporter/service-resolution failure escape startup recovery.
        }
    }
}
