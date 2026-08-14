namespace CipherNest.UiTests;

public sealed class UnlockCapabilityFailureContainmentSourceTests
{
    [Fact]
    public void UnlockPage_KeepsMasterPathAvailableWhenCapabilityProbeFails()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "UnlockPage.xaml.cs"));

        Assert.Contains("catch (Exception ex)", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Unlock.BiometricCapabilityProbe\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("ViewModel.BiometricUnlockAvailable = false;", source, StringComparison.Ordinal);
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
