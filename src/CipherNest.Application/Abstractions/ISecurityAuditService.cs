using CipherNest.Domain.Models;

namespace CipherNest.Application.Abstractions;

public interface ISecurityAuditService
{
    IReadOnlyList<SecurityAuditFinding> Analyze(IReadOnlyList<VaultItem> items, DateTimeOffset now);
}
