namespace CipherNest.UiTests;

public sealed class ClipboardSecuritySourceTests
{
    [Fact]
    public void ClipboardService_TracksFingerprintsInsteadOfDelayedPlaintextSecrets()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Services", "ClipboardSecurityService.cs"));

        Assert.Contains("_lastCopiedFingerprint", source, StringComparison.Ordinal);
        Assert.Contains("ClipboardSafetyPolicy.CreateFingerprint", source, StringComparison.Ordinal);
        Assert.Contains("ClipboardSafetyPolicy.MatchesFingerprint", source, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory", source, StringComparison.Ordinal);
        Assert.Contains("Clipboard.ScheduledClear", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ClearLaterAsync(string expected", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateLinkedTokenSource(cancellationToken)", source, StringComparison.Ordinal);
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
