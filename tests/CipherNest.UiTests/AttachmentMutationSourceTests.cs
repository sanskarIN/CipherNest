namespace CipherNest.UiTests;

public sealed class AttachmentMutationSourceTests
{
    [Fact]
    public void AttachmentMutations_ShareCancellableGateAndGlobalBudget()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));

        Assert.Contains("private readonly SemaphoreSlim _attachmentMutationGate = new(1, 1);", source, StringComparison.Ordinal);
        AssertMethodContains(source, "public async Task<AttachmentReference> AddAttachmentAsync", "await _attachmentMutationGate.WaitAsync(lease.Token)", "VaultStorageLimits.MaximumAttachmentCountTotal", "finally { _attachmentMutationGate.Release(); }");
        AssertMethodContains(source, "public async Task RemoveAttachmentAsync", "await _attachmentMutationGate.WaitAsync(lease.Token)", "finally { _attachmentMutationGate.Release(); }");
        AssertMethodContains(source, "public async Task DeletePermanentlyAsync", "await _attachmentMutationGate.WaitAsync(lease.Token)", "finally { _attachmentMutationGate.Release(); }");
        AssertMethodContains(source, "public void Dispose()", "_attachmentMutationGate.Dispose()");
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
