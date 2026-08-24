namespace CipherNest.UiTests;

public sealed class GeneratorFailureContainmentSourceTests
{
    [Fact]
    public void GeneratorAndDefaults_ReportSettingsClipboardAndGenerationFailures()
    {
        var generator = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "GeneratorViewModel.cs"));
        var defaults = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "GeneratorDefaultsViewModel.cs"));

        Assert.Contains("_exceptions.Report(\"Generator.LoadDefaults\", ex);", generator, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Generator.Generate\", ex);", generator, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Generator.Copy\", ex);", generator, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"GeneratorDefaults.Load\", ex);", defaults, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"GeneratorDefaults.Save\", ex);", defaults, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratorFailures_ClearPreviouslyGeneratedSensitiveState()
    {
        var generator = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "GeneratorViewModel.cs"));

        Assert.Contains("private void ClearGeneratedState()", generator, StringComparison.Ordinal);
        Assert.Contains("GeneratedValue = string.Empty;\n        StrengthLabel = string.Empty;", generator, StringComparison.Ordinal);
        Assert.Contains("catch (ArgumentException)\n        {\n            ClearGeneratedState();", generator, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Generator.Generate\", ex);\n            ClearGeneratedState();", generator, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Generator.LoadDefaults\", ex);\n            ClearGeneratedState();", generator, StringComparison.Ordinal);
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
