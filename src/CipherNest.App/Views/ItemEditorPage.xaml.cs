using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class ItemEditorPage : ContentPage
{
    public ItemEditorPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<ItemEditorViewModel>();
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
