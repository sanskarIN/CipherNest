using System.Text.Json;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Services;
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
                return new AppPreferences();

            await using var stream = File.OpenRead(path);
            var loaded = await JsonSerializer.DeserializeAsync<AppPreferences>(stream, Options, cancellationToken).ConfigureAwait(false);
            return AppPreferencesPolicy.Normalize(loaded ?? new AppPreferences());
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
            var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Settings directory is missing.");
            Directory.CreateDirectory(directory);
            var temp = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            try
            {
                var normalized = AppPreferencesPolicy.Normalize(preferences);
                await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 16 * 1024, FileOptions.Asynchronous))
                {
                    await JsonSerializer.SerializeAsync(stream, normalized, Options, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                File.Move(temp, path, overwrite: true);
            }
            finally
            {
                try
                {
                    if (File.Exists(temp)) File.Delete(temp);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
