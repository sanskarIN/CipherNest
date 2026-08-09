namespace CipherNest.Application.Exceptions;

public sealed class VaultLockedException : InvalidOperationException
{
    public VaultLockedException() : base("The vault is locked.")
    {
    }
}
