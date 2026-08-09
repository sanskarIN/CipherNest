using CipherNest.Domain.Models;

namespace CipherNest.App.Services;

public interface ILocalizationService
{
    AppLanguagePreference Current { get; }
    void Apply(AppLanguagePreference preference);
    string Get(string key);
}
