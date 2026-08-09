using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class UnlockPage : ContentPage
{
    private UnlockViewModel ViewModel => (UnlockViewModel)BindingContext;

    public UnlockPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<UnlockViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { await ViewModel.LoadAsync(); }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            // The normal master-passphrase path remains available if capability probing fails.
            System.Diagnostics.Debug.WriteLine($"Biometric capability check failed: {ex.Message}");
        }
    }
}
