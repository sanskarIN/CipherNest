using CipherNest.App.Services;
using CipherNest.Shared;

namespace CipherNest.App.Views;

public partial class AboutPage : ContentPage
{
    private readonly IPrivacySafeExceptionReporter _exceptions;
    private int _versionTaps;
    private DateTimeOffset _firstTap;

    public AboutPage()
    {
        InitializeComponent();
        _exceptions = ServiceProviderHelper.GetRequiredService<IPrivacySafeExceptionReporter>();
        VersionButton.Text = $"Version {AppInfo.Current.VersionString} · build {AppInfo.Current.BuildString} · crypto format {AppConstants.CryptoFormatVersion} · database schema {AppConstants.DatabaseSchemaVersion}";
        SupportDevelopmentFrame.IsVisible = BuildFeatureFlags.IsFundingLinkEnabled;
        SupportDevelopmentMetadataLabel.IsVisible = BuildFeatureFlags.IsFundingLinkEnabled;
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await NavigateSafelyAsync("//vault", "About.Navigate.Back");
    private async void OnSecurityInfoClicked(object? sender, EventArgs e) => await NavigateSafelyAsync("//security-info", "About.Navigate.SecurityInfo");
    private async void OnRepositoryClicked(object? sender, EventArgs e) => await OpenExternalAsync(AppConstants.RepositoryUrl, "repository");
    private async void OnCreatorClicked(object? sender, EventArgs e) => await OpenExternalAsync(AppConstants.CreatorUrl, "creator profile");
    private async void OnBuyMeACoffeeClicked(object? sender, EventArgs e)
    {
        if (!BuildFeatureFlags.IsFundingLinkEnabled) return;
        await OpenExternalAsync(AppConstants.BuyMeACoffeeUrl, "Buy Me a Coffee page");
    }

    private async Task OpenExternalAsync(string url, string description)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            await ShowAlertSafelyAsync("Link unavailable", $"The configured {description} link is invalid.", "Close", "About.ExternalLink.Invalid.Alert");
            return;
        }

        try
        {
            if (!await Launcher.Default.OpenAsync(uri))
            {
                await ShowAlertSafelyAsync("Could not open link", $"The system could not open the {description}.", "Close", "About.ExternalLink.Unavailable.Alert");
            }
        }
        catch (Exception ex)
        {
            _exceptions.Report("About.ExternalLink", ex);
            await ShowAlertSafelyAsync("Could not open link", $"The {description} is not available through the current system launcher.", "Close", "About.ExternalLink.Failure.Alert");
        }
    }

    private async void OnVersionClicked(object? sender, EventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        if (_versionTaps == 0 || now - _firstTap > TimeSpan.FromSeconds(12))
        {
            _versionTaps = 0;
            _firstTap = now;
        }
        _versionTaps++;
        if (_versionTaps >= 7)
        {
            _versionTaps = 0;
            await NavigateSafelyAsync("//developer", "About.Navigate.Developer");
        }
    }

    private async Task NavigateSafelyAsync(string route, string operation)
    {
        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            _exceptions.Report(operation, ex);
            await ShowAlertSafelyAsync(
                "Navigation unavailable",
                "CipherNest could not open that local page safely. Return to the vault and try again.",
                "Close",
                $"{operation}.Alert");
        }
    }

    private async Task ShowAlertSafelyAsync(string title, string message, string cancel, string operation)
    {
        try
        {
            await DisplayAlertAsync(title, message, cancel);
        }
        catch (Exception ex)
        {
            // Click handlers are async void. Contain secondary alert failures rather than leaking them through the native callback.
            _exceptions.Report(operation, ex);
        }
    }
}
