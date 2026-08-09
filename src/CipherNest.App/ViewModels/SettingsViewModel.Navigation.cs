using CipherNest.App.Views;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class SettingsViewModel
{
    [RelayCommand]
    private static async Task GeneratorDefaultsAsync() => await Shell.Current.GoToAsync(nameof(GeneratorDefaultsPage));

    [RelayCommand]
    private static async Task SecurityAuditAsync() => await Shell.Current.GoToAsync("//audit");

    [RelayCommand]
    private static async Task SecurityInfoAsync() => await Shell.Current.GoToAsync("//security-info");

    [RelayCommand]
    private static async Task AboutAsync() => await Shell.Current.GoToAsync("//about");
}
