namespace CipherNest.UiTests;

public sealed class LocalizationRoadmapSourceTests
{
    [Fact]
    public void CurrentRoadmap_RecognizesMigratedTotpWorkflowLocalization()
    {
        var roadmap = File.ReadAllText(PathAt("docs", "NEXT_STEPS.md"));

        Assert.Contains("verification/TOTP_LOCALIZATION_2026_08_19.md", roadmap, StringComparison.Ordinal);
        Assert.Contains("verify the newly migrated TOTP fixed/dynamic/status strings", roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("remaining literals outside the now-resource-backed TOTP workflow", roadmap, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("including the new TOTP URI UI strings", roadmap, StringComparison.OrdinalIgnoreCase);
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
