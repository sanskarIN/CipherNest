using System.Globalization;
using System.Resources;
using CipherNest.Domain.Models;

namespace CipherNest.App.Services;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly CultureInfo SystemUiCulture = CultureInfo.CurrentUICulture;
    private static readonly ResourceManager Resources = new("CipherNest.App.Resources.Localization.AppStrings", typeof(LocalizationService).Assembly);

    public AppLanguagePreference Current { get; private set; } = AppLanguagePreference.System;

    public void Apply(AppLanguagePreference preference)
    {
        Current = preference;
        var culture = preference switch
        {
            AppLanguagePreference.English => CultureInfo.GetCultureInfo("en-US"),
            AppLanguagePreference.Hindi => CultureInfo.GetCultureInfo("hi-IN"),
            _ => SystemUiCulture
        };
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
