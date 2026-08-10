namespace CipherNest.Infrastructure.Services;

public static class AttachmentStorageNamePolicy
{
    public static string ValidateOpaqueFileName(string opaqueFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opaqueFileName);
        if (opaqueFileName.Contains('/') || opaqueFileName.Contains('\\'))
            throw new InvalidDataException("Encrypted attachment storage name contains a path separator.");
        if (!opaqueFileName.EndsWith(".cna", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Encrypted attachment storage name has an unsupported extension.");

        var stem = opaqueFileName[..^4];
        if (stem.Length != 32 || !Guid.TryParseExact(stem, "N", out var parsed) || parsed == Guid.Empty)
            throw new InvalidDataException("Encrypted attachment storage name is not a valid opaque identifier.");

        return stem.ToLowerInvariant() + ".cna";
    }

    public static string ValidateForAttachment(Guid attachmentId, string opaqueFileName)
    {
        if (attachmentId == Guid.Empty)
            throw new InvalidDataException("Attachment identifier is invalid.");

        var normalized = ValidateOpaqueFileName(opaqueFileName);
        var expected = $"{attachmentId:N}.cna";
        if (!string.Equals(normalized, expected, StringComparison.Ordinal))
            throw new InvalidDataException("Encrypted attachment storage name does not match the attachment identifier.");

        return normalized;
    }
}
