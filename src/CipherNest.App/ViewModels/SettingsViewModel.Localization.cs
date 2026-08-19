using System.Globalization;
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

    private static string SettingsText(string key) =>
        ServiceProviderHelper.GetRequiredService<ILocalizationService>().Get(key);

    private static string SettingsFormat(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, SettingsText(key), args);

    private static string LocalizedStrengthLabel(int score) => score switch
    {
        <= 0 => SettingsText("PasswordStrengthVeryWeak"),
        1 => SettingsText("PasswordStrengthWeak"),
        2 => SettingsText("PasswordStrengthFair"),
        3 => SettingsText("PasswordStrengthStrong"),
        _ => SettingsText("PasswordStrengthVeryStrong")
    };
}
