using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class GeneratorPage : ContentPage
{
    public GeneratorPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<GeneratorViewModel>();
    }
}
