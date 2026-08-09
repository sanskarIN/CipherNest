using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class TransferPage : ContentPage
{
    public TransferPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<TransferViewModel>();
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is TransferViewModel vm) vm.ClearSensitiveState();
        base.OnDisappearing();
    }
}
