namespace CipherNest.UiTests;

public sealed class VaultSessionTransitionSourceTests
{
    [Fact]
    public void LockUnlockSecondaryUnlockAndDeletion_ShareSerializedTransitionGate()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));

        AssertMethodContains(source, "public async Task UnlockAsync", "await _gate.WaitAsync(cancellationToken)", "ReplaceDataKey(key)");
        AssertMethodContains(source, "public async Task UnlockWithSecondarySecretAsync", "await _gate.WaitAsync(cancellationToken)", "ReplaceDataKey(key)");
        AssertMethodContains(source, "public async Task LockAsync", "await _gate.WaitAsync(cancellationToken)", "ClearSessionKey()");
        AssertMethodContains(
            source,
            "public async Task DeleteVaultAsync",
            "using var authorizationLease = AcquireKeyLease(cancellationToken)",
            "await _gate.WaitAsync(authorizationLease.Token)",
            "authorizationLease.Token.ThrowIfCancellationRequested()",
            "var sessionCleared = false",
            "ClearSessionKey()",
            "sessionCleared = true",
            "DeleteDatabaseAsync(CancellationToken.None)",
            "if (sessionCleared) LockStateChanged?.Invoke(this, false)");
        Assert.Contains("private void ClearSessionKey()", source, StringComparison.Ordinal);
        Assert.Contains("CancelAndDisposeSession(session)", source, StringComparison.Ordinal);
        Assert.Contains("CancelAndDisposeSession(previousSession)", source, StringComparison.Ordinal);
        Assert.Contains("catch (AggregateException)", source, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory(_dataKey)", source, StringComparison.Ordinal);
    }

    private static void AssertMethodContains(string source, string signature, params string[] expected)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Could not find method: {signature}");
        var nextPublic = source.IndexOf("\n    public ", start + signature.Length, StringComparison.Ordinal);
        var end = nextPublic >= 0 ? nextPublic : source.Length;
        var method = source[start..end];
        foreach (var value in expected) Assert.Contains(value, method, StringComparison.Ordinal);
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
