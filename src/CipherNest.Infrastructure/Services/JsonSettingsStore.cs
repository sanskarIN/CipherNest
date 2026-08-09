using System.Text.Json;
using CipherNest.Application.Abstractions;
using CipherNest.Domain.Models;

namespace CipherNest.Infrastructure.Services;

public sealed class JsonSettingsStore(string path) : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(path))
            {
                return new AppPreferences();
            }
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<AppPreferences>(stream, Options, cancellationToken).ConfigureAwait(false) ?? new AppPreferences();
        }
        catch (JsonException)
        {
            return new AppPreferences();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Settings directory is missing."));
            var temp = path + ".tmp";
            await using (var stream = File.Create(temp))
            {
                await JsonSerializer.SerializeAsync(stream, preferences, Options, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }
}
