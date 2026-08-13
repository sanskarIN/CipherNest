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
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName(new string('x', AttachmentImportPolicy.MaximumDisplayNameCharacters + 1)));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeDisplayName("bad\nname.txt"));
    }

    [Fact]
    public void MediaType_UsesSafeDefaultAndRejectsOversizedOrControlText()
    {
        Assert.Equal(AttachmentImportPolicy.DefaultMediaType, AttachmentImportPolicy.NormalizeMediaType(null));
        Assert.Equal(AttachmentImportPolicy.DefaultMediaType, AttachmentImportPolicy.NormalizeMediaType("   "));
        Assert.Equal("text/plain", AttachmentImportPolicy.NormalizeMediaType(" text/plain "));

        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeMediaType(new string('x', AttachmentImportPolicy.MaximumMediaTypeCharacters + 1)));
        Assert.Throws<ArgumentException>(() => AttachmentImportPolicy.NormalizeMediaType("text/plain\r\nmalicious"));
    }
}
