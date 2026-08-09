namespace CipherNest.Application.Exceptions;

public sealed class VaultAuthenticationException : Exception
{
    public VaultAuthenticationException() : base("The vault could not be authenticated.")
    {
    }

    public VaultAuthenticationException(Exception innerException)
        : base("The vault could not be authenticated.", innerException)
    {
    }
}
