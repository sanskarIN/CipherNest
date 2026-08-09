namespace CipherNest.Domain.Models;

public sealed record AttachmentReference(
    Guid Id,
    string DisplayName,
    string MediaType,
    long PlaintextLength,
    string EncryptedFileName,
    DateTimeOffset CreatedUtc);
