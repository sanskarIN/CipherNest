using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class UnlockPage : ContentPage
{
    public UnlockPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<UnlockViewModel>();
    }
}
