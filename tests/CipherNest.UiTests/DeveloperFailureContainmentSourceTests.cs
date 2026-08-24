namespace CipherNest.UiTests;

public sealed class DeveloperFailureContainmentSourceTests
{
    [Fact]
    public void DeveloperViewModel_ContainsStorageEnumerationFailures()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "DeveloperViewModel.cs"));

        Assert.Contains("IPrivacySafeExceptionReporter _exceptions", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Developer.Load\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("Directory.EnumerateFiles(attachmentDir, \"*.cna\", SearchOption.TopDirectoryOnly).ToArray()", source, StringComparison.Ordinal);
        Assert.Contains("Encrypted database container: unavailable", source, StringComparison.Ordinal);
        Assert.Contains("Developer storage metadata could not be read safely.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DeveloperDiagnostics_DoesNotPublishFalseShareSuccessAfterFailure()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "DeveloperViewModel.cs"));

        Assert.Contains("var shareCompleted = false;", source, StringComparison.Ordinal);
        Assert.Contains("var operationFailed = false;", source, StringComparison.Ordinal);
        Assert.Contains("shareCompleted = true;", source, StringComparison.Ordinal);
        Assert.Contains("operationFailed = true;", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Developer.ExportDiagnostics\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Developer.ExportDiagnostics.Cleanup\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("else if (shareCompleted)", source, StringComparison.Ordinal);
        Assert.Contains("else if (!operationFailed)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.StackTrace", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DeveloperLockSimulation_SeparatesLockAndNavigationFailures()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "DeveloperViewModel.cs"));

        Assert.Contains("_exceptions.Report(\"Developer.SimulateLock\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Developer.SimulateLock.Navigation\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("The vault was locked, but the unlock screen could not be opened automatically.", source, StringComparison.Ordinal);
        Assert.Contains("Vault lock simulation could not be completed safely.", source, StringComparison.Ordinal);
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
