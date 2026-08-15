using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class AttachmentStorageNamePolicyTests
{
    [Fact]
    public void GeneratedOpaqueName_IsAcceptedAndNormalized()
    {
        var id = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        Assert.Equal(36, AttachmentStorageNamePolicy.OpaqueFileNameCharacters);
        Assert.Equal("0123456789abcdef0123456789abcdef.cna", AttachmentStorageNamePolicy.ValidateOpaqueFileName("0123456789ABCDEF0123456789ABCDEF.CNA"));
        Assert.Equal($"{id:N}.cna", AttachmentStorageNamePolicy.ValidateOpaqueFileName($"{id:N}.cna"));
        Assert.Equal($"{id:N}.cna", AttachmentStorageNamePolicy.ValidateForAttachment(id, $"{id:N}.cna"));
    }

    [Theory]
    [InlineData("folder/file.cna")]
    [InlineData("folder\\file.cna")]
    [InlineData("not-a-guid.cna")]
    [InlineData("0123456789abcdef0123456789abcdef.tmp")]
    [InlineData("0123456789abcdef0123456789abcde.cna")]
    [InlineData("00000000000000000000000000000000.cna")]
    public void MalformedOpaqueName_IsRejected(string value)
    {
        Assert.Throws<InvalidDataException>(() => AttachmentStorageNamePolicy.ValidateOpaqueFileName(value));
    }

    [Fact]
    public void OpaqueName_LengthBoundary_IsCheckedBeforeStemProcessing()
    {
        var id = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        var canonical = $"{id:N}.cna";
        Assert.Equal(AttachmentStorageNamePolicy.OpaqueFileNameCharacters, canonical.Length);
        Assert.Equal(canonical, AttachmentStorageNamePolicy.ValidateOpaqueFileName(canonical));

        Assert.Throws<InvalidDataException>(() => AttachmentStorageNamePolicy.ValidateOpaqueFileName(canonical + "x"));
        Assert.Throws<InvalidDataException>(() => AttachmentStorageNamePolicy.ValidateOpaqueFileName(canonical[..^1]));
        Assert.Throws<InvalidDataException>(() => AttachmentStorageNamePolicy.ValidateOpaqueFileName(new string('a', 1_000_000)));
    }

    [Fact]
    public void MismatchedOpaqueName_IsRejectedForAttachment()
    {
        var attachmentId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        var otherId = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210");

        Assert.Throws<InvalidDataException>(() => AttachmentStorageNamePolicy.ValidateForAttachment(Guid.Empty, $"{attachmentId:N}.cna"));
        Assert.Throws<InvalidDataException>(() => AttachmentStorageNamePolicy.ValidateForAttachment(attachmentId, $"{otherId:N}.cna"));
    }
}
