namespace CipherNest.Shared;

public static class VaultStorageLimits
{
    public const int MaximumVaultHeaderUtf8Bytes = 64 * 1024;
    public const int MaximumItemPlaintextJsonBytes = 16 * 1024 * 1024;
    public const int MaximumStoredEnvelopeBytes = 24 * 1024 * 1024;
    public const int MaximumItemCount = 100_000;
    public const int MaximumAttachmentCountTotal = 10_000;
    public const long MaximumStoredEnvelopeBytesTotal = 256L * 1024 * 1024;
}
