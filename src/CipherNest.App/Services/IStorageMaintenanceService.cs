namespace CipherNest.App.Services;

public sealed record StorageUsage(long AppDataBytes, long CacheBytes)
{
    public long TotalBytes => checked(AppDataBytes + CacheBytes);
}

public interface IStorageMaintenanceService
{
    Task<StorageUsage> GetUsageAsync(CancellationToken cancellationToken = default);
    Task<long> ClearCacheAsync(CancellationToken cancellationToken = default);
}
