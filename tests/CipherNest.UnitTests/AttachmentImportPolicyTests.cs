using CipherNest.Application.Validation;

namespace CipherNest.UnitTests;

public sealed class AttachmentImportPolicyTests
{
    [Fact]
    public void DisplayName_NormalizesToLeafNameAndRejectsInvalidInput()
    {
        Assert.Equal("report.txt", AttachmentImportPolicy.NormalizeDisplayName(Path.Combine("folder", "report.txt")));
        Assert.Equal("report.txt", AttachmentImportPolicy.NormalizeDisplayName("  report.txt  "));

        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName(null));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName("   "));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName("."));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName(".."));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName(new string('x', AttachmentImportPolicy.MaximumDisplayNameCharacters + 1)));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName("bad\nname.txt"));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName("bad\u202Ename.txt"));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName("bad\U000E0001name.txt"));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName("bad\uD800name.txt"));
    }

    [Fact]
    public void StoredDisplayName_RequiresNormalizedLeafMetadata()
    {
        Assert.True(AttachmentImportPolicy.IsValidStoredDisplayName("report.txt"));
        Assert.True(AttachmentImportPolicy.IsValidStoredDisplayName(new string('x', AttachmentImportPolicy.MaximumDisplayNameCharacters)));

        Assert.False(AttachmentImportPolicy.IsValidStoredDisplayName(" report.txt"));
        Assert.False(AttachmentImportPolicy.IsValidStoredDisplayName("report.txt "));
        Assert.False(AttachmentImportPolicy.IsValidStoredDisplayName("folder/report.txt"));
        Assert.False(AttachmentImportPolicy.IsValidStoredDisplayName("folder\\report.txt"));
        Assert.False(AttachmentImportPolicy.IsValidStoredDisplayName("."));
        Assert.False(AttachmentImportPolicy.IsValidStoredDisplayName(".."));
        Assert.False(AttachmentImportPolicy.IsValidStoredDisplayName("report\u2066.txt"));
        Assert.False(AttachmentImportPolicy.IsValidStoredDisplayName("report\U000E0001.txt"));
        Assert.False(AttachmentImportPolicy.IsValidStoredDisplayName("report\uD800.txt"));
    }

    [Fact]
    public void MediaType_UsesSafeDefaultAndRejectsOversizedOrUnsupportedMetadataText()
    {
        Assert.Equal(AttachmentImportPolicy.DefaultMediaType, AttachmentImportPolicy.NormalizeMediaType(null));
        Assert.Equal(AttachmentImportPolicy.DefaultMediaType, AttachmentImportPolicy.NormalizeMediaType("   "));
        Assert.Equal("text/plain", AttachmentImportPolicy.NormalizeMediaType(" text/plain "));

        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeMediaType(new string('x', AttachmentImportPolicy.MaximumMediaTypeCharacters + 1)));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeMediaType("text/plain\r\nmalicious"));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeMediaType("text/\u202Eplain"));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeMediaType("text/\U000E0001plain"));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeMediaType("text/\uD800plain"));
    }

    [Fact]
    public void StoredMediaType_RequiresTrimmedRuneSafeMetadata()
    {
        Assert.True(AttachmentImportPolicy.IsValidStoredMediaType("text/plain"));
        Assert.True(AttachmentImportPolicy.IsValidStoredMediaType("text/plain; charset=utf-8"));
        Assert.True(AttachmentImportPolicy.IsValidStoredMediaType(new string('x', AttachmentImportPolicy.MaximumMediaTypeCharacters)));

        Assert.False(AttachmentImportPolicy.IsValidStoredMediaType(" text/plain"));
        Assert.False(AttachmentImportPolicy.IsValidStoredMediaType("text/plain "));
        Assert.False(AttachmentImportPolicy.IsValidStoredMediaType("text/plain\n"));
        Assert.False(AttachmentImportPolicy.IsValidStoredMediaType("text/\u2067plain"));
        Assert.False(AttachmentImportPolicy.IsValidStoredMediaType("text/\U000E0001plain"));
        Assert.False(AttachmentImportPolicy.IsValidStoredMediaType("text/\uD800plain"));
    }
}
