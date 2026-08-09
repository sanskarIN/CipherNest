using CipherNest.Domain.Models;

namespace CipherNest.Application.Services;

public static class TrashRetentionPolicy
{
    public static DateTimeOffset GetCutoff(DateTimeOffset nowUtc, int retentionDays) =>
        nowUtc.AddDays(-Math.Clamp(retentionDays, 1, 365));

    public static IReadOnlyList<Guid> FindExpiredItemIds(IEnumerable<VaultItem> items, DateTimeOffset nowUtc, int retentionDays)
    {
        ArgumentNullException.ThrowIfNull(items);
        var cutoff = GetCutoff(nowUtc, retentionDays);
        return items
            .Where(item => item.DeletedUtc is { } deletedUtc && deletedUtc <= cutoff)
            .Select(static item => item.Id)
            .Distinct()
            .ToArray();
    }
}
