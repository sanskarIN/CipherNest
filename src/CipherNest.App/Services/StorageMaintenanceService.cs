namespace CipherNest.App.Services;

public sealed class StorageMaintenanceService : IStorageMaintenanceService
{
    public async Task<StorageUsage> GetUsageAsync(CancellationToken cancellationToken = default)
    {
        var appData = FileSystem.Current.AppDataDirectory;
        var cache = FileSystem.Current.CacheDirectory;
        var appDataBytes = await Task.Run(() => MeasureDirectory(appData, cancellationToken), cancellationToken).ConfigureAwait(false);
        var cacheBytes = await Task.Run(() => MeasureDirectory(cache, cancellationToken), cancellationToken).ConfigureAwait(false);
        return new StorageUsage(appDataBytes, cacheBytes);
    }

    public async Task<long> ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        var root = FileSystem.Current.CacheDirectory;
        return await Task.Run(() => ClearDirectory(root, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    private static long MeasureDirectory(string root, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(root)) return 0;
        long total = 0;
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            IEnumerable<string> files;
            IEnumerable<string> directories;
            try
            {
                files = Directory.EnumerateFiles(directory);
                directories = Directory.EnumerateDirectories(directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try { total = checked(total + new FileInfo(file).Length); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OverflowException) { }
            }

            foreach (var child in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var attributes = File.GetAttributes(child);
                    if ((attributes & FileAttributes.ReparsePoint) == 0) pending.Push(child);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
        return total;
    }

    private static long ClearDirectory(string root, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(root)) return 0;
        long deletedBytes = 0;
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var info = new FileInfo(file);
                var length = info.Exists ? info.Length : 0;
                File.Delete(file);
                deletedBytes = checked(deletedBytes + length);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OverflowException) { }
        }

        foreach (var child in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var attributes = File.GetAttributes(child);
                if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                deletedBytes = checked(deletedBytes + ClearDirectory(child, cancellationToken));
                if (!Directory.EnumerateFileSystemEntries(child).Any()) Directory.Delete(child, recursive: false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OverflowException) { }
        }
        return deletedBytes;
    }
}
