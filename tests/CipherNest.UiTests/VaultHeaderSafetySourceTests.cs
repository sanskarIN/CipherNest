namespace CipherNest.UiTests;

public sealed class VaultHeaderSafetySourceTests
{
    [Fact]
    public void VaultHeaderPolicy_KeepsStrictVersionAwareSchemaAndDepthBound()
    {
        var policy = File.ReadAllText(FindRepositoryFile("src", "CipherNest.Infrastructure", "Services", "VaultHeaderJsonPolicy.cs"));
        var limits = File.ReadAllText(FindRepositoryFile("src", "CipherNest.Shared", "VaultStorageLimits.cs"));

        Assert.Contains("public const int MinimumSupportedVersion = 1;", policy, StringComparison.Ordinal);
        Assert.Contains("public const int CurrentVersion = 2;", policy, StringComparison.Ordinal);
        Assert.Contains("MaxDepth = VaultStorageLimits.MaximumVaultHeaderJsonDepth", policy, StringComparison.Ordinal);
        Assert.Contains("StringComparer.Ordinal", policy, StringComparison.Ordinal);
        Assert.Contains("name is \"version\" or \"master\" or \"recovery\" or \"secondary\"", policy, StringComparison.Ordinal);
        Assert.Contains("name is \"version\" or \"salt\" or \"kdf\" or \"nonce\" or \"ciphertext\" or \"tag\"", policy, StringComparison.Ordinal);
        Assert.Contains("name is \"memoryKiB\" or \"iterations\" or \"parallelism\"", policy, StringComparison.Ordinal);
        Assert.Contains("rootProperties.Count != Version1RootPropertyCount || rootProperties.Contains(\"secondary\")", policy, StringComparison.Ordinal);
        Assert.Contains("rootProperties.Count != Version2RootPropertyCount || !rootProperties.Contains(\"secondary\")", policy, StringComparison.Ordinal);
        Assert.Contains("public const int MaximumVaultHeaderJsonDepth = 16;", limits, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultService_ValidatesHeaderBeforeDeserializationAndUnwrap()
    {
        var source = File.ReadAllText(FindRepositoryFile("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));
        var readStart = source.IndexOf("private async Task<VaultHeaderDocument> ReadHeaderUnlockedAsync", StringComparison.Ordinal);
        var policy = source.IndexOf("VaultHeaderJsonPolicy.Validate(headerJson);", readStart, StringComparison.Ordinal);
        var deserialize = source.IndexOf("JsonSerializer.Deserialize<VaultHeaderDocument>", policy, StringComparison.Ordinal);
        var unlockStart = source.IndexOf("public async Task UnlockAsync", StringComparison.Ordinal);
        var readHeader = source.IndexOf("var header = await ReadHeaderAsync", unlockStart, StringComparison.Ordinal);
        var unwrap = source.IndexOf("_crypto.UnwrapKey", readHeader, StringComparison.Ordinal);

        Assert.True(readStart >= 0);
        Assert.True(policy > readStart);
        Assert.True(deserialize > policy);
        Assert.True(unlockStart >= 0);
        Assert.True(readHeader > unlockStart);
        Assert.True(unwrap > readHeader);
        Assert.Contains("catch (Exception ex) when (ex is JsonException or InvalidDataException)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultHeaderWrites_SelfValidateAndUpgradeMutatedLegacyHeaders()
    {
        var source = File.ReadAllText(FindRepositoryFile("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));

        Assert.Contains("private static string SerializeHeader(VaultHeaderDocument header)", source, StringComparison.Ordinal);
        Assert.Contains("VaultHeaderJsonPolicy.Validate(headerJson);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("WriteHeaderAsync(JsonSerializer.Serialize", source, StringComparison.Ordinal);
        Assert.Contains("header with { Version = VaultHeaderJsonPolicy.CurrentVersion, Master = newMaster }", source, StringComparison.Ordinal);
        Assert.Contains("header with { Version = VaultHeaderJsonPolicy.CurrentVersion, Secondary = wrapped }", source, StringComparison.Ordinal);
        Assert.Contains("header with { Version = VaultHeaderJsonPolicy.CurrentVersion, Secondary = null }", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryFile(params string[] pathParts)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine([current.FullName, .. pathParts]);
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        throw new FileNotFoundException($"Could not locate repository file: {Path.Combine(pathParts)}");
    }
}
