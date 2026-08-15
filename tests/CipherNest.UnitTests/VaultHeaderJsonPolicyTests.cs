using CipherNest.Infrastructure.Services;
using CipherNest.Shared;

namespace CipherNest.UnitTests;

public sealed class VaultHeaderJsonPolicyTests
{
    [Fact]
    public void CurrentVersion2Header_IsAccepted()
    {
        VaultHeaderJsonPolicy.Validate(BuildHeader(version: 2, includeSecondary: true));
    }

    [Fact]
    public void LegacyVersion1Header_IsAccepted()
    {
        VaultHeaderJsonPolicy.Validate(BuildHeader(version: 1, includeSecondary: false));
    }

    [Theory]
    [InlineData("duplicate-root")]
    [InlineData("unexpected-root")]
    [InlineData("case-variant-root")]
    [InlineData("missing-secondary-v2")]
    [InlineData("secondary-on-v1")]
    [InlineData("future-version")]
    [InlineData("duplicate-wrapper")]
    [InlineData("unexpected-wrapper")]
    [InlineData("missing-wrapper-field")]
    [InlineData("wrong-wrapper-kind")]
    [InlineData("duplicate-kdf")]
    [InlineData("unexpected-kdf")]
    [InlineData("missing-kdf-field")]
    [InlineData("fractional-kdf")]
    public void StrictSchemaViolations_AreRejected(string mutation)
    {
        var json = Mutate(BuildHeader(version: 2, includeSecondary: true), mutation);
        Assert.Throws<InvalidDataException>(() => VaultHeaderJsonPolicy.Validate(json));
    }

    [Fact]
    public void ExcessiveDepth_IsRejectedByParserBoundary()
    {
        var nested = string.Concat(Enumerable.Repeat("{\"x\":", VaultStorageLimits.MaximumVaultHeaderJsonDepth + 1)) +
                     "0" +
                     new string('}', VaultStorageLimits.MaximumVaultHeaderJsonDepth + 1);
        var json = BuildHeader(version: 2, includeSecondary: true);
        json = json[..^1] + ",\"unexpectedDepth\":" + nested + "}";

        Assert.ThrowsAny<System.Text.Json.JsonException>(() => VaultHeaderJsonPolicy.Validate(json));
    }

    [Fact]
    public void ExactMaximumUtf8Boundary_IsAccepted()
    {
        var json = BuildHeader(version: 2, includeSecondary: true);
        var padded = json.PadRight(VaultStorageLimits.MaximumVaultHeaderUtf8Bytes, ' ');

        VaultHeaderJsonPolicy.Validate(padded);
    }

    [Fact]
    public void FirstByteAboveMaximumUtf8Boundary_IsRejected()
    {
        var json = BuildHeader(version: 2, includeSecondary: true);
        var padded = json.PadRight(VaultStorageLimits.MaximumVaultHeaderUtf8Bytes + 1, ' ');

        Assert.Throws<InvalidDataException>(() => VaultHeaderJsonPolicy.Validate(padded));
    }

    private static string BuildHeader(int version, bool includeSecondary)
    {
        var wrapper = BuildWrapper();
        return includeSecondary
            ? $"{{\"version\":{version},\"master\":{wrapper},\"recovery\":null,\"secondary\":null}}"
            : $"{{\"version\":{version},\"master\":{wrapper},\"recovery\":null}}";
    }

    private static string BuildWrapper()
    {
        var salt = Convert.ToBase64String(new byte[16]);
        var nonce = Convert.ToBase64String(new byte[12]);
        var ciphertext = Convert.ToBase64String(new byte[32]);
        var tag = Convert.ToBase64String(new byte[16]);
        return $"{{\"version\":1,\"salt\":\"{salt}\",\"kdf\":{{\"memoryKiB\":65536,\"iterations\":3,\"parallelism\":1}},\"nonce\":\"{nonce}\",\"ciphertext\":\"{ciphertext}\",\"tag\":\"{tag}\"}}";
    }

    private static string Mutate(string json, string mutation) => mutation switch
    {
        "duplicate-root" => json.Replace("\"version\":2", "\"version\":2,\"version\":2", StringComparison.Ordinal),
        "unexpected-root" => json[..^1] + ",\"unexpected\":true}",
        "case-variant-root" => json.Replace("\"version\":2", "\"Version\":2", StringComparison.Ordinal),
        "missing-secondary-v2" => json.Replace(",\"secondary\":null", string.Empty, StringComparison.Ordinal),
        "secondary-on-v1" => BuildHeader(version: 1, includeSecondary: true),
        "future-version" => json.Replace("\"version\":2", "\"version\":999", StringComparison.Ordinal),
        "duplicate-wrapper" => json.Replace("\"master\":{\"version\":1", "\"master\":{\"version\":1,\"version\":1", StringComparison.Ordinal),
        "unexpected-wrapper" => json.Replace("\"master\":{\"version\":1", "\"master\":{\"unexpected\":true,\"version\":1", StringComparison.Ordinal),
        "missing-wrapper-field" => json.Replace(",\"tag\":\"AAAAAAAAAAAAAAAAAAAAAA==\"", string.Empty, StringComparison.Ordinal),
        "wrong-wrapper-kind" => json.Replace($"\"master\":{BuildWrapper()}", "\"master\":[]", StringComparison.Ordinal),
        "duplicate-kdf" => json.Replace("\"kdf\":{\"memoryKiB\":65536", "\"kdf\":{\"memoryKiB\":65536,\"memoryKiB\":65536", StringComparison.Ordinal),
        "unexpected-kdf" => json.Replace("\"kdf\":{", "\"kdf\":{\"unexpected\":1,", StringComparison.Ordinal),
        "missing-kdf-field" => json.Replace(",\"parallelism\":1", string.Empty, StringComparison.Ordinal),
        "fractional-kdf" => json.Replace("\"iterations\":3", "\"iterations\":3.5", StringComparison.Ordinal),
        _ => throw new ArgumentOutOfRangeException(nameof(mutation), mutation, null)
    };
}
