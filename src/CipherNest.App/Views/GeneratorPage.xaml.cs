using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class GeneratorPage : ContentPage
{
    private GeneratorViewModel ViewModel => (GeneratorViewModel)BindingContext;

    public GeneratorPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<GeneratorViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadDefaultsAsync();
    }
}
