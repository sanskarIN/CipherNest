using CipherNest.Domain.Models;

namespace CipherNest.Application.Abstractions;

public interface ISettingsStore
{
    Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default);
}
