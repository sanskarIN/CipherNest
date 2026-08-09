using CipherNest.App.Services;
using CipherNest.Shared;

namespace CipherNest.App.Views;

public partial class AboutPage : ContentPage
{
    private int _versionTaps;
    private DateTimeOffset _firstTap;

    public AboutPage()
    {
        InitializeComponent();
        VersionButton.Text = $"Version {AppInfo.Current.VersionString} · build {AppInfo.Current.BuildString} · crypto format {AppConstants.CryptoFormatVersion} · database schema {AppConstants.DatabaseSchemaVersion}";
        SupportDevelopmentFrame.IsVisible = BuildFeatureFlags.IsFundingLinkEnabled;
        SupportDevelopmentMetadataLabel.IsVisible = BuildFeatureFlags.IsFundingLinkEnabled;
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//vault");
    private async void OnSecurityInfoClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//security-info");
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
            await DisplayAlertAsync("Link unavailable", $"The configured {description} link is invalid.", "Close");
            return;
        }

        try
        {
            if (!await Launcher.Default.OpenAsync(uri))
                await DisplayAlertAsync("Could not open link", $"The system could not open the {description}.", "Close");
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or InvalidOperationException)
        {
            await DisplayAlertAsync("Could not open link", $"The {description} is not available through the current system launcher.", "Close");
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
            await Shell.Current.GoToAsync("//developer");
        }
    }
}
