namespace CipherNest.UiTests;

public sealed class OnboardingPassphraseBoundsSourceTests
{
    [Fact]
    public void Onboarding_RejectsOversizedMasterPassphraseBeforeGeneratorAndVaultWork()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "OnboardingViewModel.cs"));

        Assert.Contains("MaximumMasterPassphraseCharacters = 4_096", source, StringComparison.Ordinal);
        Assert.Contains("value.Length > MaximumMasterPassphraseCharacters", source, StringComparison.Ordinal);
        Assert.Contains("MasterPassphrase.Length is >= MinimumMasterPassphraseCharacters and <= MaximumMasterPassphraseCharacters", source, StringComparison.Ordinal);
        Assert.Contains("MasterPassphrase = string.Empty;", source, StringComparison.Ordinal);
        Assert.Contains("Confirmation = string.Empty;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ErrorMessage = ex.Message", source, StringComparison.Ordinal);
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
