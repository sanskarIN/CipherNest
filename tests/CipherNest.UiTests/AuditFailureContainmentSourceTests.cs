namespace CipherNest.UiTests;

public sealed class AuditFailureContainmentSourceTests
{
    [Fact]
    public void AuditCommand_ReportsAndClearsOnFailure()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "AuditViewModel.cs"));

        Assert.Contains("IPrivacySafeExceptionReporter _exceptions", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Audit.Run\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("Findings.Clear();", source, StringComparison.Ordinal);
        Assert.Contains("The local security audit could not be completed safely.", source, StringComparison.Ordinal);
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
