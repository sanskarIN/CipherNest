namespace CipherNest.UiTests;

public sealed class VaultKeyLeaseSourceTests
{
    [Fact]
    public void VaultService_UsesCancellablePrivateKeyCopiesInsteadOfMutableSessionKeyReferences()
    {
        var service = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));
        var lease = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultKeyLease.cs"));

        Assert.Contains("private readonly object _keySync", service, StringComparison.Ordinal);
        Assert.Contains("CancellationTokenSource? _sessionCancellation", service, StringComparison.Ordinal);
        Assert.Contains("new VaultKeyLease(_dataKey.ToArray()", service, StringComparison.Ordinal);
        Assert.Contains("CancelAndDisposeSession(session)", service, StringComparison.Ordinal);
        Assert.Contains("CancelAndDisposeSession(previousSession)", service, StringComparison.Ordinal);
        Assert.Contains("session.Cancel()", service, StringComparison.Ordinal);
        Assert.DoesNotContain("private byte[] RequireKey()", service, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory(Key);", lease, StringComparison.Ordinal);
        Assert.Contains("CreateLinkedTokenSource(sessionToken, callerToken)", lease, StringComparison.Ordinal);
        Assert.Contains("catch", lease, StringComparison.Ordinal);
        Assert.True(lease.Split("CryptographicOperations.ZeroMemory(Key);", StringSplitOptions.None).Length >= 3,
            "The lease must zero its key both when linked-token creation fails and when the lease is disposed.");
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
