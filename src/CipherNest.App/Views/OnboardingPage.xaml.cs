using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<OnboardingViewModel>();
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is OnboardingViewModel vm) vm.ClearSensitiveState();
        base.OnDisappearing();
    }
}
