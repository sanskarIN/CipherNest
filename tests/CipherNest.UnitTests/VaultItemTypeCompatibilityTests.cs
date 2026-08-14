using CipherNest.Domain.Models;

namespace CipherNest.UnitTests;

public sealed class VaultItemTypeCompatibilityTests
{
    [Fact]
    public void PersistedNumericValues_RemainBackwardCompatible()
    {
        Assert.Equal(0, (int)VaultItemType.Login);
        Assert.Equal(1, (int)VaultItemType.SecureNote);
        Assert.Equal(2, (int)VaultItemType.Identity);
        Assert.Equal(3, (int)VaultItemType.PaymentCardReference);
        Assert.Equal(4, (int)VaultItemType.WifiCredential);
        Assert.Equal(5, (int)VaultItemType.SoftwareLicense);
        Assert.Equal(6, (int)VaultItemType.ServerSshReference);
        Assert.Equal(7, (int)VaultItemType.Document);
        Assert.Equal(8, (int)VaultItemType.Custom);
        Assert.Equal(9, (int)VaultItemType.OneTimePassword);
    }
}
