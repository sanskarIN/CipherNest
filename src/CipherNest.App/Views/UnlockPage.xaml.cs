using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class UnlockPage : ContentPage
{
    private readonly IPrivacySafeExceptionReporter _exceptions;
    private UnlockViewModel ViewModel => (UnlockViewModel)BindingContext;

    public UnlockPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<UnlockViewModel>();
        _exceptions = ServiceProviderHelper.GetRequiredService<IPrivacySafeExceptionReporter>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try { await ViewModel.LoadAsync(); }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            _exceptions.Report("Unlock.BiometricCapabilityProbe", ex);
            // The normal master-passphrase path remains available if capability probing fails.
        }
    }

    protected override void OnDisappearing()
    {
        ViewModel.ClearSensitiveState();
        base.OnDisappearing();
    }
}
