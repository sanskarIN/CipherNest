namespace CipherNest.App.Services;

public static class AttachmentTypePolicy
{
    private static readonly HashSet<string> PreviewableMediaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/markdown",
        "text/csv",
        "application/json"
    };

    private static readonly IReadOnlyDictionary<string, string> PreviewableExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".txt"] = "text/plain",
        [".md"] = "text/markdown",
        [".markdown"] = "text/markdown",
        [".csv"] = "text/csv",
        [".json"] = "application/json",
        [".log"] = "text/plain"
    };

    public static bool CanPreview(string mediaType, string fileName) =>
        PreviewableMediaTypes.Contains(NormalizeMediaType(mediaType)) || PreviewableExtensions.ContainsKey(Path.GetExtension(fileName));

    public static string ResolveMediaType(string? declaredMediaType, string fileName)
    {
        var normalized = NormalizeMediaType(declaredMediaType);
        if (normalized != "application/octet-stream") return normalized;
        return PreviewableExtensions.TryGetValue(Path.GetExtension(fileName), out var inferred) ? inferred : normalized;
    }

    public static string NormalizeMediaType(string? mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType)) return "application/octet-stream";
        var value = mediaType.Trim();
        var separator = value.IndexOf(';');
        if (separator >= 0) value = value[..separator].Trim();
        if (value.Length is < 3 or > 127 || value.Any(static ch => char.IsControl(ch) || char.IsWhiteSpace(ch))) return "application/octet-stream";
        var slash = value.IndexOf('/');
        if (slash <= 0 || slash == value.Length - 1 || value.IndexOf('/', slash + 1) >= 0) return "application/octet-stream";
        return value.ToLowerInvariant();
    }
}
