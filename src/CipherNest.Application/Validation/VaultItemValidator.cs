using CipherNest.Domain.Models;

namespace CipherNest.Application.Validation;

public static class VaultItemValidator
{
    private const long MaximumAttachmentBytes = 100L * 1024 * 1024;

    public static IReadOnlyList<string> Validate(VaultItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var errors = new List<string>();
        var tags = item.Tags ?? Array.Empty<string>();
        var customFields = item.CustomFields ?? Array.Empty<CustomField>();
        var attachments = item.Attachments ?? Array.Empty<AttachmentReference>();

        if (item.Id == Guid.Empty) errors.Add("Item identifier is invalid.");
        if (!Enum.IsDefined(item.Type)) errors.Add("Item type is invalid.");
        if (string.IsNullOrWhiteSpace(item.Title)) errors.Add("Title is required.");
        if (item.Title is { Length: > 256 }) errors.Add("Title cannot exceed 256 characters.");
        if (item.Username is null || item.Username.Length > 2048) errors.Add("Username or identifier is invalid or too long.");
        if (item.Secret is null || item.Secret.Length > 100_000) errors.Add("Secret is invalid or too large for an item field.");
        if (item.Url is null || item.Url.Length > 4096) errors.Add("URL is invalid or too long.");
        if (item.Notes is null || item.Notes.Length > SafeNoteLimits.MaximumCharacters) errors.Add($"Notes are invalid or exceed the {SafeNoteLimits.MaximumCharacters:N0}-character safety limit.");
        if (item.Notes is not null && SafeNoteLimits.ExceedsLineLimit(item.Notes)) errors.Add($"Notes exceed the {SafeNoteLimits.MaximumLines:N0}-line safety limit.");
        if (item.Collection is null || item.Collection.Length > 128) errors.Add("Collection name is invalid or exceeds 128 characters.");
        if (item.Tags is null || tags.Count > 100) errors.Add("An item can have at most 100 tags and the tag collection must be present.");
        if (item.CustomFields is null || customFields.Count > 100) errors.Add("An item can have at most 100 custom fields and the custom-field collection must be present.");
        if (item.Attachments is null || attachments.Count > 25) errors.Add("An item can have at most 25 attachments and the attachment collection must be present.");
        if (tags.Any(static tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 128)) errors.Add("Tags cannot be empty and cannot exceed 128 characters.");
        if (customFields.Any(static field => field is null || string.IsNullOrWhiteSpace(field.Name) || field.Name.Length > 128 || field.Value is null || field.Value.Length > 100_000)) errors.Add("A custom field name or value is invalid.");
        if (attachments.Any(static attachment =>
                attachment is null ||
                attachment.Id == Guid.Empty ||
                string.IsNullOrWhiteSpace(attachment.DisplayName) || attachment.DisplayName.Length > 240 ||
                string.IsNullOrWhiteSpace(attachment.MediaType) || attachment.MediaType.Length > 256 ||
                attachment.PlaintextLength is < 0 or > MaximumAttachmentBytes ||
                string.IsNullOrWhiteSpace(attachment.EncryptedFileName) || attachment.EncryptedFileName.Length > 64))
        {
            errors.Add("An attachment contains invalid metadata.");
        }

        var nonNullAttachments = attachments.Where(static attachment => attachment is not null).ToArray();
        if (nonNullAttachments.Select(static attachment => attachment.Id).Distinct().Count() != nonNullAttachments.Length)
            errors.Add("Attachment identifiers must be unique within an item.");
        if (nonNullAttachments.Select(static attachment => attachment.EncryptedFileName).Where(static name => name is not null).Distinct(StringComparer.OrdinalIgnoreCase).Count() != nonNullAttachments.Count(static attachment => attachment.EncryptedFileName is not null))
            errors.Add("Encrypted attachment storage names must be unique within an item.");
        return errors;
    }
}
