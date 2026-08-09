using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class TrashPage : ContentPage
{
    public TrashPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<TrashViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is TrashViewModel vm) await vm.LoadAsync();
    }
}
