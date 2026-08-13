using CipherNest.App.Services;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class SettingsViewModel
{
    public IReadOnlyList<AppLanguagePreference> Languages { get; } = Enum.GetValues<AppLanguagePreference>();

    [ObservableProperty]
    public partial AppLanguagePreference SelectedLanguage { get; set; } = AppLanguagePreference.System;

    public async Task LoadLanguageAsync()
    {
        var preferences = await _settings.LoadAsync();
        SelectedLanguage = preferences.Language;
        ServiceProviderHelper.GetRequiredService<ILocalizationService>().Apply(SelectedLanguage);
    }

    [RelayCommand]
    private async Task SaveLanguageAsync()
    {
        var preferences = await _settings.LoadAsync();
        await _settings.SaveAsync(preferences with { Language = SelectedLanguage });
        ServiceProviderHelper.GetRequiredService<ILocalizationService>().Apply(SelectedLanguage);
        StatusMessage = SelectedLanguage == AppLanguagePreference.English
            ? "English language preference saved."
            : "System language preference saved. CipherNest currently ships English resources and falls back to English when a translation is unavailable.";
    }
}
