namespace CipherNest.UiTests;

public sealed class VaultStorageBoundsSourceTests
{
    [Fact]
    public void SqliteStore_BoundsHeaderAndEnvelopeLengthsBeforeMaterialization()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Persistence", "SqliteVaultStore.cs"));

        Assert.Contains("length(CAST(HeaderJson AS BLOB))", source, StringComparison.Ordinal);
        Assert.Contains("VaultStorageLimits.MaximumVaultHeaderUtf8Bytes", source, StringComparison.Ordinal);
        Assert.Contains("SELECT COUNT(*), COALESCE(SUM(length(Envelope)), 0)", source, StringComparison.Ordinal);
        Assert.Contains("SELECT Id, Envelope, length(Envelope)", source, StringComparison.Ordinal);
        Assert.Contains("VaultStorageLimits.MaximumStoredEnvelopeBytes", source, StringComparison.Ordinal);
        Assert.Contains("VaultStorageLimits.MaximumStoredEnvelopeBytesTotal", source, StringComparison.Ordinal);
        Assert.Contains("VaultStorageLimits.MaximumItemCount", source, StringComparison.Ordinal);
        Assert.Contains("Guid.TryParseExact(idText, \"D\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultService_BoundsSerializedAndDecryptedRecordSizes()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));
        var policy = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultHeaderJsonPolicy.cs"));
        var limits = File.ReadAllText(PathAt("src", "CipherNest.Shared", "VaultStorageLimits.cs"));

        Assert.Contains("VaultStorageLimits.MaximumItemPlaintextJsonBytes", source, StringComparison.Ordinal);
        Assert.Contains("VaultStorageLimits.MaximumStoredEnvelopeBytes", source, StringComparison.Ordinal);
        Assert.Contains("VaultHeaderJsonPolicy.Validate(headerJson);", source, StringComparison.Ordinal);
        Assert.Contains("VaultStorageLimits.MaximumVaultHeaderUtf8Bytes", policy, StringComparison.Ordinal);
        Assert.Contains("public const int MaximumVaultHeaderUtf8Bytes = 64 * 1024;", limits, StringComparison.Ordinal);
        Assert.Contains("public const int MaximumVaultHeaderJsonDepth = 16;", limits, StringComparison.Ordinal);
        Assert.Contains("JsonSerializer.SerializeToUtf8Bytes(item, JsonOptions)", source, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory(plaintext)", source, StringComparison.Ordinal);
    }

    private static string PathAt(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CipherNest.slnx"))) directory = directory.Parent;
        if (directory is null) throw new DirectoryNotFoundException("Could not locate the CipherNest repository root from the test output directory.");
        var path = directory.FullName;
        foreach (var segment in segments) path = Path.Combine(path, segment);
        return path;
    }
}
