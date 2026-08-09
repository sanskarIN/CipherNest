namespace CipherNest.App.Views;

public partial class SecurityInfoPage : ContentPage
{
    public SecurityInfoPage() => InitializeComponent();

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//settings");
}
