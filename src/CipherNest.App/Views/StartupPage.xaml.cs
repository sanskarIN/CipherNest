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
            await DisplayAlert("CipherNest could not start", $"Local storage initialization failed: {ex.Message}", "OK");
            _navigated = false;
        }
    }
}
