using System.Buffers;
using System.Globalization;
using System.Text;

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

        var trimmed = displayName.Trim();
        var lastSeparator = Math.Max(trimmed.LastIndexOf('/'), trimmed.LastIndexOf('\\'));
        var normalized = lastSeparator >= 0 ? trimmed[(lastSeparator + 1)..] : trimmed;
        if (!IsValidStoredDisplayName(normalized))
            throw new ArgumentException("Attachment name is invalid or contains unsupported metadata characters.", nameof(displayName));
        return normalized;
    }

    public static string NormalizeMediaType(string? mediaType)
    {
        var normalized = string.IsNullOrWhiteSpace(mediaType) ? DefaultMediaType : mediaType.Trim();
        if (!IsValidStoredMediaType(normalized))
            throw new ArgumentException("Attachment media type is invalid or contains unsupported metadata characters.", nameof(mediaType));
        return normalized;
    }

    public static bool IsValidStoredDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > MaximumDisplayNameCharacters)
            return false;
        if (!string.Equals(displayName, displayName.Trim(), StringComparison.Ordinal))
            return false;
        if (displayName is "." or ".." || displayName.Contains('/') || displayName.Contains('\\'))
            return false;
        return !ContainsUnsupportedMetadataRune(displayName);
    }

    public static bool IsValidStoredMediaType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType) || mediaType.Length > MaximumMediaTypeCharacters)
            return false;
        if (!string.Equals(mediaType, mediaType.Trim(), StringComparison.Ordinal))
            return false;
        return !ContainsUnsupportedMetadataRune(mediaType);
    }

    private static bool ContainsUnsupportedMetadataRune(string value)
    {
        var remaining = value.AsSpan();
        while (!remaining.IsEmpty)
        {
            var status = Rune.DecodeFromUtf16(remaining, out var rune, out var consumed);
            if (status != OperationStatus.Done)
                return true;

            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.Control or UnicodeCategory.Format)
                return true;

            remaining = remaining[consumed..];
        }

        return false;
    }
}
