using System.Globalization;
using System.Resources;
using CipherNest.Domain.Models;

namespace CipherNest.App.Services;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly CultureInfo SystemUiCulture = CultureInfo.CurrentUICulture;
    private static readonly ResourceManager PrimaryResources = new("CipherNest.App.Resources.Localization.AppStrings", typeof(LocalizationService).Assembly);
    private static readonly ResourceManager[] FeatureResources =
    [
        new("CipherNest.App.Resources.Localization.AuditStrings", typeof(LocalizationService).Assembly),
        new("CipherNest.App.Resources.Localization.TrashStrings", typeof(LocalizationService).Assembly),
        new("CipherNest.App.Resources.Localization.TransferStrings", typeof(LocalizationService).Assembly)
    ];

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
        var culture = CultureInfo.CurrentUICulture;
        var value = PrimaryResources.GetString(key, culture);
        if (value is not null) return value;

        foreach (var resources in FeatureResources)
        {
            value = resources.GetString(key, culture);
            if (value is not null) return value;
        }

        return key;
    }
}
