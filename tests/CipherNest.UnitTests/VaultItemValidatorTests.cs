using CipherNest.Application.Validation;
using CipherNest.Domain.Models;

namespace CipherNest.UnitTests;

public sealed class VaultItemValidatorTests
{
    [Fact]
    public void ValidItem_HasNoErrors()
    {
        var item = new VaultItem
        {
            Id = Guid.NewGuid(),
            Title = "Example",
            Username = "user@example.test",
            Secret = "secret",
            Url = "https://example.test",
            Notes = "note",
            Collection = "Personal",
            Tags = ["one", "two"],
            CustomFields = [new CustomField("PIN hint", "stored securely", true)]
        };

        Assert.Empty(VaultItemValidator.Validate(item));
    }

    [Fact]
    public void RejectsMissingAndOversizedCoreFields()
    {
        var item = new VaultItem
        {
            Id = Guid.NewGuid(),
            Title = new string('T', 257),
            Username = new string('U', 2049),
            Secret = new string('S', 100_001),
            Url = new string('W', 4097),
            Notes = new string('N', SafeNoteLimits.MaximumCharacters + 1),
            Collection = new string('C', 129)
        };

        var errors = VaultItemValidator.Validate(item);

        Assert.Contains("Title cannot exceed 256 characters.", errors);
        Assert.Contains("Username or identifier is invalid or too long.", errors);
        Assert.Contains("Secret is invalid or too large for an item field.", errors);
        Assert.Contains("URL is invalid or too long.", errors);
        Assert.Contains($"Notes are invalid or exceed the {SafeNoteLimits.MaximumCharacters:N0}-character safety limit.", errors);
        Assert.Contains("Collection name is invalid or exceeds 128 characters.", errors);
        Assert.Contains("Title is required.", VaultItemValidator.Validate(item with { Title = " " }));
    }

    [Fact]
    public void RejectsNoteBeyondSharedLineLimit()
    {
        var notes = string.Join('\n', Enumerable.Repeat("line", SafeNoteLimits.MaximumLines + 1));
        var errors = VaultItemValidator.Validate(new VaultItem { Id = Guid.NewGuid(), Title = "Line limit", Notes = notes });
        Assert.Contains($"Notes exceed the {SafeNoteLimits.MaximumLines:N0}-line safety limit.", errors);
    }

    [Fact]
    public void RejectsCollectionCountsAndEntryBounds()
    {
        var item = new VaultItem
        {
            Id = Guid.NewGuid(),
            Title = "Bounds",
            Tags = Enumerable.Repeat("tag", 101).ToArray(),
            CustomFields = Enumerable.Range(0, 101).Select(i => new CustomField($"field-{i}", "value", false)).ToArray(),
            Attachments = Enumerable.Range(0, 26).Select(i => new AttachmentReference(Guid.NewGuid(), $"file-{i}.txt", "text/plain", 1, $"{Guid.NewGuid():N}.cna", DateTimeOffset.UtcNow)).ToArray()
        };

        var errors = VaultItemValidator.Validate(item);

        Assert.Contains("An item can have at most 100 tags and the tag collection must be present.", errors);
        Assert.Contains("An item can have at most 100 custom fields and the custom-field collection must be present.", errors);
        Assert.Contains("An item can have at most 25 attachments and the attachment collection must be present.", errors);
        Assert.Contains("Tags cannot be empty and cannot exceed 128 characters.", VaultItemValidator.Validate(item with { Tags = [new string('x', 129)], CustomFields = [], Attachments = [] }));
        Assert.Contains("Tags cannot be empty and cannot exceed 128 characters.", VaultItemValidator.Validate(item with { Tags = [" "], CustomFields = [], Attachments = [] }));
        Assert.Contains("A custom field name or value is invalid.", VaultItemValidator.Validate(item with { Tags = [], CustomFields = [new CustomField(" ", "value", false)], Attachments = [] }));
        Assert.Contains("A custom field name or value is invalid.", VaultItemValidator.Validate(item with { Tags = [], CustomFields = [new CustomField("name", new string('x', 100_001), false)], Attachments = [] }));
    }

    [Fact]
    public void RejectsInvalidAndDuplicateAttachmentMetadata()
    {
        var id = Guid.NewGuid();
        var storage = $"{Guid.NewGuid():N}.cna";
        var valid = new VaultItem
        {
            Id = Guid.NewGuid(),
            Title = "Attachment metadata",
            Attachments = [new AttachmentReference(id, "file.txt", "text/plain", 100, storage, DateTimeOffset.UtcNow)]
        };
        Assert.Empty(VaultItemValidator.Validate(valid));

        var invalidMetadata = valid with
        {
            Attachments = [new AttachmentReference(Guid.Empty, " ", new string('m', 257), 100L * 1024 * 1024 + 1, " ", DateTimeOffset.UtcNow)]
        };
        Assert.Contains("An attachment contains invalid metadata.", VaultItemValidator.Validate(invalidMetadata));

        var duplicateIds = valid with
        {
            Attachments =
            [
                new AttachmentReference(id, "one.txt", "text/plain", 1, $"{Guid.NewGuid():N}.cna", DateTimeOffset.UtcNow),
                new AttachmentReference(id, "two.txt", "text/plain", 1, $"{Guid.NewGuid():N}.cna", DateTimeOffset.UtcNow)
            ]
        };
        Assert.Contains("Attachment identifiers must be unique within an item.", VaultItemValidator.Validate(duplicateIds));

        var duplicateStorage = valid with
        {
            Attachments =
            [
                new AttachmentReference(Guid.NewGuid(), "one.txt", "text/plain", 1, storage, DateTimeOffset.UtcNow),
                new AttachmentReference(Guid.NewGuid(), "two.txt", "text/plain", 1, storage.ToUpperInvariant(), DateTimeOffset.UtcNow)
            ]
        };
        Assert.Contains("Encrypted attachment storage names must be unique within an item.", VaultItemValidator.Validate(duplicateStorage));
    }

    [Fact]
    public void RejectsRuntimeNullsEmptyIdentifierAndUnknownTypeWithoutThrowing()
    {
        var malformed = new VaultItem
        {
            Id = Guid.Empty,
            Type = (VaultItemType)999,
            Title = null!,
            Username = null!,
            Secret = null!,
            Url = null!,
            Notes = null!,
            Collection = null!,
            Tags = null!,
            CustomFields = null!,
            Attachments = null!
        };

        var errors = VaultItemValidator.Validate(malformed);

        Assert.Contains("Item identifier is invalid.", errors);
        Assert.Contains("Item type is invalid.", errors);
        Assert.Contains("Title is required.", errors);
        Assert.Contains("Username or identifier is invalid or too long.", errors);
        Assert.Contains("Secret is invalid or too large for an item field.", errors);
        Assert.Contains("URL is invalid or too long.", errors);
        Assert.Contains($"Notes are invalid or exceed the {SafeNoteLimits.MaximumCharacters:N0}-character safety limit.", errors);
        Assert.Contains("Collection name is invalid or exceeds 128 characters.", errors);
        Assert.Contains("An item can have at most 100 tags and the tag collection must be present.", errors);
        Assert.Contains("An item can have at most 100 custom fields and the custom-field collection must be present.", errors);
        Assert.Contains("An item can have at most 25 attachments and the attachment collection must be present.", errors);
    }
}
