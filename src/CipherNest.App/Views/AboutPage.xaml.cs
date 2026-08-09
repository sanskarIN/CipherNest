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
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//vault");
    private async void OnSecurityInfoClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//security-info");

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
