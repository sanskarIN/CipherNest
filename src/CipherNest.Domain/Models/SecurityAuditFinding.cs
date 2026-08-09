namespace CipherNest.Domain.Models;

public enum SecurityFindingKind
{
    WeakSecret,
    ReusedSecret,
    ExpiredReview,
    MissingTitle
}

public sealed record SecurityAuditFinding(
    Guid ItemId,
    SecurityFindingKind Kind,
    string Message,
    int Severity);
