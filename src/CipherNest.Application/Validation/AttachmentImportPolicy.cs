namespace CipherNest.Application.Validation;

public static class AttachmentImportPolicy
{
    public const int MaximumDisplayNameCharacters = 240;
    public const int MaximumMediaTypeCharacters = 256;
    public const string DefaultMediaType = "application/octet-stream";

    public static string NormalizeDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Attachment name is required.", nameof(displayName));

        var normalized = Path.GetFileName(displayName.Trim());
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > MaximumDisplayNameCharacters)
            throw new ArgumentException("Attachment name is invalid or too long.", nameof(displayName));
        if (normalized.Any(char.IsControl))
            throw new ArgumentException("Attachment name contains unsupported control characters.", nameof(displayName));
        return normalized;
    }

    public static string NormalizeMediaType(string? mediaType)
    {
        var normalized = string.IsNullOrWhiteSpace(mediaType) ? DefaultMediaType : mediaType.Trim();
        if (normalized.Length > MaximumMediaTypeCharacters || normalized.Any(char.IsControl))
            throw new ArgumentException("Attachment media type is invalid or too long.", nameof(mediaType));
        return normalized;
    }
}
