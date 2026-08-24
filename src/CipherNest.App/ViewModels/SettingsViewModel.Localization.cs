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
        try
        {
            var preferences = await _settings.LoadAsync();
            SelectedLanguage = preferences.Language;
            ServiceProviderHelper.GetRequiredService<ILocalizationService>().Apply(SelectedLanguage);
        }
        catch (Exception ex)
        {
            _exceptions.Report("Settings.Language.Load", ex);
            SelectedLanguage = AppLanguagePreference.System;
            StatusMessage = SafeSettingsText("SettingsLoadFailure", "Language preference could not be loaded safely. CipherNest kept the current interface language where possible.");
        }
    }

    [RelayCommand]
    private async Task SaveLanguageAsync()
    {
        try
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
        catch (Exception ex)
        {
            _exceptions.Report("Settings.Language.Save", ex);
            StatusMessage = SafeSettingsText("SettingsSaveFailure", "Language preference could not be saved or applied safely. The previous setting remains authoritative.");
        }
    }

    private static string SettingsText(string key) =>
        ServiceProviderHelper.GetRequiredService<ILocalizationService>().Get(key);

    private static string SafeSettingsText(string key, string fallback)
    {
        try
        {
            return SettingsText(key);
        }
        catch
        {
            return fallback;
        }
    }

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
