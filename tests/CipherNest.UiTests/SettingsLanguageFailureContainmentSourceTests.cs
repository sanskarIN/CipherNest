namespace CipherNest.UiTests;

public sealed class SettingsLanguageFailureContainmentSourceTests
{
    [Fact]
    public void LanguageLoadAndSave_ReportContainedFailures()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.Localization.cs"));

        Assert.Contains("_exceptions.Report(\"Settings.Language.Load\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Settings.Language.Save\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("SelectedLanguage = AppLanguagePreference.System;", source, StringComparison.Ordinal);
        Assert.Contains("SafeSettingsText(\"SettingsLoadFailure\"", source, StringComparison.Ordinal);
        Assert.Contains("SafeSettingsText(\"SettingsSaveFailure\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.StackTrace", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LanguageFailureStatus_HasNonThrowingFallback()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.Localization.cs"));

        Assert.Contains("private static string SafeSettingsText", source, StringComparison.Ordinal);
        Assert.Contains("catch\n        {\n            return fallback;", source, StringComparison.Ordinal);
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
