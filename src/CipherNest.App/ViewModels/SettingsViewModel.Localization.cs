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

        var localization = ServiceProviderHelper.GetRequiredService<ILocalizationService>();
        localization.Apply(SelectedLanguage);
        StatusMessage = SelectedLanguage switch
        {
            AppLanguagePreference.English => localization.Get("EnglishPreferenceSaved"),
            AppLanguagePreference.Hindi => localization.Get("HindiPreferenceSaved"),
            _ => localization.Get("SystemPreferenceSaved")
        };
    }
}
