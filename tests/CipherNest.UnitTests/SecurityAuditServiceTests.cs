using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class SecurityAuditServiceTests
{
    [Fact]
    public void Analyze_FindsExactDuplicatesWeakReuseAndOverdueItems()
    {
        var now = new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero);
        var first = new VaultItem
        {
            Id = Guid.NewGuid(), Type = VaultItemType.Login, Title = "Example", Username = "me@example.test",
            Secret = "password123", Url = "https://example.test", Notes = "same", Collection = "Work", ReviewAfterUtc = now.AddDays(-1)
        };
        var second = first with { Id = Guid.NewGuid() };
        var third = new VaultItem
        {
            Id = Guid.NewGuid(), Type = VaultItemType.Login, Title = "Different", Username = "other@example.test",
            Secret = "password123", Url = "https://other.example.test"
        };

        var findings = new SecurityAuditService(new PasswordGenerator()).Analyze([first, second, third], now);

        Assert.Equal(2, findings.Count(f => f.Kind == SecurityFindingKind.DuplicateEntry));
        Assert.Equal(3, findings.Count(f => f.Kind == SecurityFindingKind.ReusedSecret));
        Assert.Equal(2, findings.Count(f => f.Kind == SecurityFindingKind.ExpiredReview));
        Assert.True(findings.Count(f => f.Kind == SecurityFindingKind.WeakSecret) >= 3);
    }

    [Fact]
    public void Analyze_DoesNotTreatDifferentEntriesAsDuplicates()
    {
        var service = new SecurityAuditService(new PasswordGenerator());
        var items = new[]
        {
            new VaultItem { Id = Guid.NewGuid(), Title = "One", Secret = "unique-value-1" },
            new VaultItem { Id = Guid.NewGuid(), Title = "Two", Secret = "unique-value-2" }
        };

        var findings = service.Analyze(items, DateTimeOffset.UtcNow);

        Assert.DoesNotContain(findings, f => f.Kind == SecurityFindingKind.DuplicateEntry);
    }
}
