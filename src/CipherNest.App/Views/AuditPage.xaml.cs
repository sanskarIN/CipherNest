using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class AuditPage : ContentPage
{
    public AuditPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<AuditViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AuditViewModel vm) await vm.RunAsync();
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is AuditViewModel vm) vm.ClearSensitiveState();
        base.OnDisappearing();
    }
}
