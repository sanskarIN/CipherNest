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
            Notes = new string('N', 250_001),
            Collection = new string('C', 129)
        };

        var errors = VaultItemValidator.Validate(item);

        Assert.Contains("Title cannot exceed 256 characters.", errors);
        Assert.Contains("Username or identifier is too long.", errors);
        Assert.Contains("Secret is too large for an item field.", errors);
        Assert.Contains("URL is too long.", errors);
        Assert.Contains("Notes are too large for an item.", errors);
        Assert.Contains("Collection name cannot exceed 128 characters.", errors);

        Assert.Contains("Title is required.", VaultItemValidator.Validate(item with { Title = " " }));
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

        Assert.Contains("An item can have at most 100 tags.", errors);
        Assert.Contains("An item can have at most 100 custom fields.", errors);
        Assert.Contains("An item can have at most 25 attachments.", errors);
        Assert.Contains("Tags cannot exceed 128 characters.", VaultItemValidator.Validate(item with { Tags = [new string('x', 129)], CustomFields = [], Attachments = [] }));
        Assert.Contains("A custom field name or value is invalid.", VaultItemValidator.Validate(item with { Tags = [], CustomFields = [new CustomField(" ", "value", false)], Attachments = [] }));
        Assert.Contains("A custom field name or value is invalid.", VaultItemValidator.Validate(item with { Tags = [], CustomFields = [new CustomField("name", new string('x', 100_001), false)], Attachments = [] }));
    }
}
