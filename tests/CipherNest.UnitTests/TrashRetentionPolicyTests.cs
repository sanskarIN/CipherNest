using CipherNest.Application.Services;
using CipherNest.Domain.Models;

namespace CipherNest.UnitTests;

public sealed class TrashRetentionPolicyTests
{
    [Fact]
    public void FindsOnlyDeletedItemsAtOrBeforeRetentionCutoff()
    {
        var now = new DateTimeOffset(2026, 8, 9, 7, 0, 0, TimeSpan.Zero);
        var expired = CreateItem(now.AddDays(-30));
        var recent = CreateItem(now.AddDays(-29));
        var active = CreateItem(null);

        var ids = TrashRetentionPolicy.FindExpiredItemIds([expired, recent, active], now, 30);

        Assert.Equal([expired.Id], ids);
    }

    [Fact]
    public void RetentionDaysAreClampedToSafeBounds()
    {
        var now = new DateTimeOffset(2026, 8, 9, 7, 0, 0, TimeSpan.Zero);
        Assert.Equal(now.AddDays(-1), TrashRetentionPolicy.GetCutoff(now, 0));
        Assert.Equal(now.AddDays(-365), TrashRetentionPolicy.GetCutoff(now, 9999));
    }

    private static VaultItem CreateItem(DateTimeOffset? deletedUtc) => new()
    {
        Id = Guid.NewGuid(),
        Type = VaultItemType.Login,
        Title = "Retention test",
        DeletedUtc = deletedUtc,
        CreatedUtc = DateTimeOffset.UtcNow,
        ModifiedUtc = DateTimeOffset.UtcNow
    };
}
