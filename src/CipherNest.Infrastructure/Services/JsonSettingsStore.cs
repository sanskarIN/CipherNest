using System.Text.Json;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Services;
using CipherNest.Domain.Models;

namespace CipherNest.Infrastructure.Services;

public sealed class JsonSettingsStore : ISettingsStore
{
    public const long MaximumSettingsFileBytes = 64 * 1024;
    public const int MaximumSettingsJsonDepth = 16;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        MaxDepth = MaximumSettingsJsonDepth
    };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path;

    public JsonSettingsStore(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
    }

    public async Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path))
                return new AppPreferences();

            await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (stream.Length is < 0 or > MaximumSettingsFileBytes)
                return new AppPreferences();

            return await DeserializeBoundedAsync(stream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
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
            var directory = Path.GetDirectoryName(_path) ?? throw new InvalidOperationException("Settings directory is missing.");
            Directory.CreateDirectory(directory);
            var temp = Path.Combine(directory, $".{Path.GetFileName(_path)}.{Guid.NewGuid():N}.tmp");
            try
            {
                var normalized = AppPreferencesPolicy.Normalize(preferences);
                await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 16 * 1024, FileOptions.Asynchronous))
                {
                    await JsonSerializer.SerializeAsync(stream, normalized, Options, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    if (stream.Length > MaximumSettingsFileBytes)
                        throw new InvalidDataException("Serialized settings exceed the supported size limit.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temp, _path, overwrite: true);
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

    private static async Task<AppPreferences> DeserializeBoundedAsync(Stream source, CancellationToken cancellationToken)
    {
        var buffer = GC.AllocateUninitializedArray<byte>(checked((int)MaximumSettingsFileBytes + 1));
        var totalRead = 0;

        while (totalRead < buffer.Length)
        {
            var read = await source.ReadAsync(buffer.AsMemory(totalRead), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;

            totalRead += read;
        }

        if (totalRead > MaximumSettingsFileBytes)
            return new AppPreferences();

        await using var bounded = new MemoryStream(buffer, 0, totalRead, writable: false, publiclyVisible: false);
        var loaded = await JsonSerializer.DeserializeAsync<AppPreferences>(bounded, Options, cancellationToken).ConfigureAwait(false);
        return AppPreferencesPolicy.Normalize(loaded ?? new AppPreferences());
    }
}
