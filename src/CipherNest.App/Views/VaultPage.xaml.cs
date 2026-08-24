using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class VaultPage : ContentPage
{
    public VaultPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<VaultViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is VaultViewModel vm)
        {
            vm.Activate();
            await vm.LoadAsync();
        }
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is VaultViewModel vm) vm.ClearSensitiveState();
        base.OnDisappearing();
    }
}
