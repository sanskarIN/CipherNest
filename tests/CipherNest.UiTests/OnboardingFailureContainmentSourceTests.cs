namespace CipherNest.UiTests;

public sealed class OnboardingFailureContainmentSourceTests
{
    [Fact]
    public void VaultCreation_ReportsUnexpectedFailuresWithoutClaimingSuccess()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "OnboardingViewModel.cs"));

        Assert.Contains("IPrivacySafeExceptionReporter _exceptions", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Onboarding.CreateVault\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("No successful setup is being reported", source, StringComparison.Ordinal);
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
