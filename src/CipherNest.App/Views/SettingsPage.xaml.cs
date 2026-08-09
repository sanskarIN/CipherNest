using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<SettingsViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is not SettingsViewModel vm) return;
        await vm.LoadAsync();
        await vm.LoadLanguageAsync();
    }
}
