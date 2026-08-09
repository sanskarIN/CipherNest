using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class GeneratorDefaultsPage : ContentPage
{
    private GeneratorDefaultsViewModel ViewModel => (GeneratorDefaultsViewModel)BindingContext;

    public GeneratorDefaultsPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<GeneratorDefaultsViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadAsync();
    }
}
