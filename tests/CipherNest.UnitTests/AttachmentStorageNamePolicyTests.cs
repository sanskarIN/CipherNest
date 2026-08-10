using CipherNest.Infrastructure.Services;

namespace CipherNest.UnitTests;

public sealed class AttachmentStorageNamePolicyTests
{
    [Fact]
    public void GeneratedOpaqueName_IsAcceptedAndNormalized()
    {
        var id = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        Assert.Equal("0123456789abcdef0123456789abcdef.cna", AttachmentStorageNamePolicy.ValidateOpaqueFileName("0123456789ABCDEF0123456789ABCDEF.CNA"));
        Assert.Equal($"{id:N}.cna", AttachmentStorageNamePolicy.ValidateOpaqueFileName($"{id:N}.cna"));
    }

    [Theory]
    [InlineData("folder/file.cna")]
    [InlineData("folder\\file.cna")]
    [InlineData("not-a-guid.cna")]
    [InlineData("0123456789abcdef0123456789abcdef.tmp")]
    [InlineData("0123456789abcdef0123456789abcde.cna")]
    public void MalformedOpaqueName_IsRejected(string value)
    {
        Assert.Throws<InvalidDataException>(() => AttachmentStorageNamePolicy.ValidateOpaqueFileName(value));
    }
}
