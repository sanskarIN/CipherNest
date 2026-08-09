using CipherNest.Application.Abstractions;
using CipherNest.Domain.Models;

namespace CipherNest.Infrastructure.Services;

public sealed class SecurityAuditService(IPasswordGenerator passwordGenerator) : ISecurityAuditService
{
    public IReadOnlyList<SecurityAuditFinding> Analyze(IReadOnlyList<VaultItem> items, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(items);
        var active = items.Where(static item => item.DeletedUtc is null).ToArray();
        var findings = new List<SecurityAuditFinding>();

        foreach (var item in active)
        {
            if (string.IsNullOrWhiteSpace(item.Title))
            {
                findings.Add(new(item.Id, SecurityFindingKind.MissingTitle, "Item is missing a title.", 1));
            }
            if (!string.IsNullOrEmpty(item.Secret) && passwordGenerator.Evaluate(item.Secret).Score <= 1)
            {
                findings.Add(new(item.Id, SecurityFindingKind.WeakSecret, "Secret appears weak; replace it with a long unique value where possible.", 3));
            }
            if (item.ReviewAfterUtc is { } review && review <= now)
            {
                findings.Add(new(item.Id, SecurityFindingKind.ExpiredReview, "This item is due for review.", 1));
            }
        }

        foreach (var group in active.Where(static x => !string.IsNullOrEmpty(x.Secret)).GroupBy(static x => x.Secret, StringComparer.Ordinal).Where(static g => g.Count() > 1))
        {
            foreach (var item in group)
            {
                findings.Add(new(item.Id, SecurityFindingKind.ReusedSecret, "This secret is reused by another vault item.", 4));
            }
        }

        foreach (var group in active.GroupBy(CreateDuplicateSignature, StringComparer.Ordinal).Where(static group => group.Count() > 1))
        {
            foreach (var item in group)
            {
                findings.Add(new(item.Id, SecurityFindingKind.DuplicateEntry, "This item is an exact duplicate of another active vault entry. Review both before deleting either copy.", 2));
            }
        }

        return findings.OrderByDescending(static f => f.Severity).ThenBy(static f => f.Kind).ToArray();
    }

    private static string CreateDuplicateSignature(VaultItem item) => string.Join(
        '\u001f',
        item.Type.ToString(),
        item.Title.Trim(),
        item.Username.Trim(),
        item.Secret,
        item.Url.Trim(),
        item.Notes,
        item.Collection.Trim());
}
