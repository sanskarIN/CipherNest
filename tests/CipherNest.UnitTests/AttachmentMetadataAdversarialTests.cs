using CipherNest.Application.Validation;
using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class AttachmentMetadataAdversarialTests
{
    [Fact]
    public void DeterministicHostileMetadataAndStorageCorpus_IsRejected()
    {
        var displayNames = BuildHostileDisplayNames().ToArray();
        var mediaTypes = BuildHostileMediaTypes().ToArray();
        var storageNames = BuildHostileStorageNames().ToArray();

        Assert.Equal(48, displayNames.Length);
        Assert.Equal(40, mediaTypes.Length);
        Assert.Equal(40, storageNames.Length);
        Assert.Equal(128, displayNames.Length + mediaTypes.Length + storageNames.Length);

        foreach (var value in displayNames)
            Assert.False(AttachmentImportPolicy.IsValidStoredDisplayName(value));

        foreach (var value in mediaTypes)
            Assert.False(AttachmentImportPolicy.IsValidStoredMediaType(value));

        foreach (var value in storageNames)
        {
            var exception = Record.Exception(() => AttachmentStorageNamePolicy.ValidateOpaqueFileName(value));
            Assert.NotNull(exception);
            Assert.True(exception is InvalidDataException or ArgumentException, $"Unexpected exception type for hostile storage name: {exception.GetType().Name}");
        }
    }

    private static IEnumerable<string> BuildHostileDisplayNames()
    {
        for (var codePoint = 0; codePoint < 32; codePoint++)
            yield return $"file-{(char)codePoint}-name.txt";

        string[] formattingCharacters =
        [
            "\u200B",
            "\u200E",
            "\u200F",
            "\u202A",
            "\u202B",
            "\u202C",
            "\u2066",
            "\u2069",
            "\U000E0001"
        ];
        foreach (var formattingCharacter in formattingCharacters)
            yield return $"file-{formattingCharacter}-name.txt";

        yield return " file.txt";
        yield return "file.txt ";
        yield return "folder/file.txt";
        yield return "folder\\file.txt";
        yield return ".";
        yield return "bad\uD800name.txt";
        yield return new string('x', AttachmentImportPolicy.MaximumDisplayNameCharacters + 1);
    }

    private static IEnumerable<string> BuildHostileMediaTypes()
    {
        for (var codePoint = 0; codePoint < 32; codePoint++)
            yield return $"text/{(char)codePoint}plain";

        yield return "text/\u200Bplain";
        yield return "text/\u202Eplain";
        yield return "text/\u2067plain";
        yield return "text/\U000E0001plain";
        yield return " text/plain";
        yield return "text/plain ";
        yield return "text/\uD800plain";
        yield return new string('x', AttachmentImportPolicy.MaximumMediaTypeCharacters + 1);
    }

    private static IEnumerable<string> BuildHostileStorageNames()
    {
        for (var length = 0; length < 10; length++)
            yield return new string('a', length) + ".cna";

        for (var length = AttachmentStorageNamePolicy.OpaqueFileNameCharacters + 1; length <= AttachmentStorageNamePolicy.OpaqueFileNameCharacters + 10; length++)
            yield return new string('a', length);

        for (var index = 0; index < 10; index++)
            yield return new string('a', index) + "g" + new string('a', 31 - index) + ".cna";

        var validStem = "0123456789abcdef0123456789abcdef";
        for (var index = 0; index < 5; index++)
            yield return validStem + $".{index:D3}";

        for (var index = 0; index < 5; index++)
        {
            var characters = validStem.ToCharArray();
            characters[index] = index < 3 ? '/' : '\\';
            yield return new string(characters) + ".cna";
        }
    }
}
