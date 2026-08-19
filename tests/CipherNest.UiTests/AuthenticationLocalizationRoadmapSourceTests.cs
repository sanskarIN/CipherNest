namespace CipherNest.UiTests;

public sealed class AuthenticationLocalizationRoadmapSourceTests
{
    [Fact]
    public void CurrentRoadmap_RecognizesMigratedAuthenticationLocalization()
    {
        var roadmap = File.ReadAllText(PathAt("docs", "NEXT_STEPS.md"));

        Assert.Contains("verification/AUTHENTICATION_LOCALIZATION_2026_08_19.md", roadmap, StringComparison.Ordinal);
        Assert.Contains("migrated Unlock and onboarding/recovery security surfaces", roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("remaining literals outside the now-resource-backed TOTP/authentication/onboarding workflows", roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("master passphrase, or recovery key in screenshots or store media", roadmap, StringComparison.OrdinalIgnoreCase);
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
