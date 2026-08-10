namespace CipherNest.UiTests;

public sealed class MasterAuthorizationSessionSourceTests
{
    [Theory]
    [InlineData("public async Task EnableSecondaryUnlockAsync")]
    [InlineData("public async Task DisableSecondaryUnlockAsync")]
    [InlineData("public async Task ChangeMasterPassphraseAsync")]
    [InlineData("public async Task DeleteVaultAsync")]
    public void MasterAuthenticatedMutations_AreBoundToTheAcquiredSession(string signature)
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find method: {signature}");
        var nextPublic = source.IndexOf("\n    public ", start + signature.Length, StringComparison.Ordinal);
        var end = nextPublic >= 0 ? nextPublic : source.Length;
        var method = source[start..end];

        Assert.Contains("using var authorizationLease = AcquireKeyLease(cancellationToken);", method, StringComparison.Ordinal);
        Assert.Contains("authorizationLease.Token", method, StringComparison.Ordinal);
        Assert.Contains("await _gate.WaitAsync(authorizationLease.Token)", method, StringComparison.Ordinal);
        Assert.Contains("ReauthenticateAsync(", method, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteVault_ClearsTheAuthorizedSessionBeforeDeletion()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));
        var start = source.IndexOf("public async Task DeleteVaultAsync", StringComparison.Ordinal);
        var end = source.IndexOf("\n    public ", start + 1, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var method = source[start..end];

        Assert.Contains("authorizationLease.Token.ThrowIfCancellationRequested();", method, StringComparison.Ordinal);
        Assert.Contains("ClearSessionKey();", method, StringComparison.Ordinal);
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
