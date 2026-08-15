namespace CipherNest.UiTests;

public sealed class TotpSafetySourceTests
{
    [Fact]
    public void TotpParserAndGenerator_RetainSecurityCriticalOrderingAndCleanup()
    {
        var policy = File.ReadAllText(PathAt("src", "CipherNest.Application", "Validation", "TotpPolicy.cs"));
        var service = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "TotpService.cs"));

        Assert.Contains("public const int MinimumSecretCharacters = 16;", policy, StringComparison.Ordinal);
        Assert.Contains("public const int MaximumSecretCharacters = 1024;", policy, StringComparison.Ordinal);
        Assert.Contains("public const int MaximumFormattedInputCharacters = 4096;", policy, StringComparison.Ordinal);
        Assert.Contains("Array.Clear(normalized, 0, normalized.Length);", policy, StringComparison.Ordinal);
        Assert.Contains("remainder is 1 or 3 or 6", policy, StringComparison.Ordinal);

        var settingsIndex = service.IndexOf("TotpPolicy.ValidateSettings", StringComparison.Ordinal);
        var normalizeIndex = service.IndexOf("TotpPolicy.NormalizeSecret", StringComparison.Ordinal);
        var decodeIndex = service.IndexOf("DecodeBase32(normalized)", StringComparison.Ordinal);
        var hmacIndex = service.IndexOf("using HMAC hmac", StringComparison.Ordinal);
        Assert.True(settingsIndex >= 0 && settingsIndex < normalizeIndex, "TOTP settings must be validated before seed normalization.");
        Assert.True(normalizeIndex < decodeIndex, "TOTP seed normalization must precede Base32 decoding.");
        Assert.True(decodeIndex < hmacIndex, "Base32 validation/decoding must complete before HMAC work begins.");

        Assert.Contains("MaximumUnixTimeSeconds", service, StringComparison.Ordinal);
        Assert.Contains("? DateTimeOffset.MaxValue", service, StringComparison.Ordinal);
        Assert.Contains("buffer != 0", service, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory(key);", service, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory(counterBytes);", service, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory(hash);", service, StringComparison.Ordinal);
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
