namespace CipherNest.Domain.Models;

public sealed record VaultItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public VaultItemType Type { get; init; } = VaultItemType.Login;
    public string Title { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public string Collection { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public bool IsFavorite { get; init; }
    public IReadOnlyList<CustomField> CustomFields { get; init; } = Array.Empty<CustomField>();
    public IReadOnlyList<AttachmentReference> Attachments { get; init; } = Array.Empty<AttachmentReference>();
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewAfterUtc { get; init; }
    public DateTimeOffset? DeletedUtc { get; init; }
    public bool RequiresReauthentication { get; init; }

    public VaultItem Normalize(DateTimeOffset now)
    {
        var tags = Tags.Select(static tag => tag.Trim()).Where(static tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase).ToArray();
        return this with
        {
            Title = Title.Trim(),
            Username = Username.Trim(),
            Url = Url.Trim(),
            Collection = Collection.Trim(),
            Tags = tags,
            ModifiedUtc = now
        };
    }
}
