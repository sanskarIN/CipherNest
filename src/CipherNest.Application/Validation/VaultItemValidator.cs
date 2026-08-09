using CipherNest.Domain.Models;

namespace CipherNest.Application.Validation;

public static class VaultItemValidator
{
    public static IReadOnlyList<string> Validate(VaultItem item)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(item.Title))
        {
            errors.Add("Title is required.");
        }

        if (item.Title.Length > 256)
        {
            errors.Add("Title cannot exceed 256 characters.");
        }

        if (item.Notes.Length > 250_000)
        {
            errors.Add("Notes are too large for an item.");
        }

        if (item.Tags.Count > 100)
        {
            errors.Add("An item can have at most 100 tags.");
        }

        if (item.CustomFields.Count > 100)
        {
            errors.Add("An item can have at most 100 custom fields.");
        }

        return errors;
    }
}
