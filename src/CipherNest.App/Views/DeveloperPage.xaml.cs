using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class DeveloperPage : ContentPage
{
    public DeveloperPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<DeveloperViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is DeveloperViewModel vm) await vm.LoadAsync();
    }
}
