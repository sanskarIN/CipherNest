using CipherNest.Domain.Models;

namespace CipherNest.Application.Validation;

public static class VaultItemValidator
{
    public static IReadOnlyList<string> Validate(VaultItem item)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(item.Title)) errors.Add("Title is required.");
        if (item.Title.Length > 256) errors.Add("Title cannot exceed 256 characters.");
        if (item.Username.Length > 2048) errors.Add("Username or identifier is too long.");
        if (item.Secret.Length > 100_000) errors.Add("Secret is too large for an item field.");
        if (item.Url.Length > 4096) errors.Add("URL is too long.");
        if (item.Notes.Length > 250_000) errors.Add("Notes are too large for an item.");
        if (item.Collection.Length > 128) errors.Add("Collection name cannot exceed 128 characters.");
        if (item.Tags.Count > 100) errors.Add("An item can have at most 100 tags.");
        if (item.CustomFields.Count > 100) errors.Add("An item can have at most 100 custom fields.");
        if (item.Attachments.Count > 25) errors.Add("An item can have at most 25 attachments.");
        if (item.Tags.Any(static tag => tag.Length > 128)) errors.Add("Tags cannot exceed 128 characters.");
        if (item.CustomFields.Any(static field => string.IsNullOrWhiteSpace(field.Name) || field.Name.Length > 128 || field.Value.Length > 100_000)) errors.Add("A custom field name or value is invalid.");
        return errors;
    }
}
