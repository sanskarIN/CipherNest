using CipherNest.App.Services;
using CipherNest.App.ViewModels;

namespace CipherNest.App.Views;

public partial class ItemEditorPage : ContentPage
{
    private readonly IPrivacySafeExceptionReporter _exceptions;

    public ItemEditorPage()
    {
        InitializeComponent();
        BindingContext = ServiceProviderHelper.GetRequiredService<ItemEditorViewModel>();
        _exceptions = ServiceProviderHelper.GetRequiredService<IPrivacySafeExceptionReporter>();
    }

    protected override void OnDisappearing()
    {
        if (BindingContext is ItemEditorViewModel viewModel) viewModel.ClearSensitiveState();
        base.OnDisappearing();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _exceptions.Report("ItemEditor.Navigate.Back", ex);
        }
    }
}
