namespace CipherNest.Domain.Models;

public enum VaultItemType
{
    Login = 0,
    SecureNote = 1,
    Identity = 2,
    PaymentCardReference = 3,
    WifiCredential = 4,
    SoftwareLicense = 5,
    ServerSshReference = 6,
    Document = 7,
    Custom = 8,
    OneTimePassword = 9
}
