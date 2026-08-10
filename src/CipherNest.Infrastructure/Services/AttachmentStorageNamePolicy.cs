namespace CipherNest.Infrastructure.Services;

public static class AttachmentStorageNamePolicy
{
    public static string ValidateOpaqueFileName(string opaqueFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(opaqueFileName);
        if (opaqueFileName.Contains('/') || opaqueFileName.Contains('\\'))
            throw new InvalidDataException("Encrypted attachment storage name contains a path separator.");
        if (!opaqueFileName.EndsWith(".cna", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Encrypted attachment storage name has an unsupported extension.");

        var stem = opaqueFileName[..^4];
        if (stem.Length != 32 || !Guid.TryParseExact(stem, "N", out _))
            throw new InvalidDataException("Encrypted attachment storage name is not a valid opaque identifier.");

        return stem.ToLowerInvariant() + ".cna";
    }
}
