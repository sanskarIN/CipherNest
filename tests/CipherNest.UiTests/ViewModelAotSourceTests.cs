using System.Text.RegularExpressions;

namespace CipherNest.UiTests;

public sealed class ViewModelAotSourceTests
{
    private static readonly Regex FieldObservablePattern = new(
        @"\[ObservableProperty(?:\([^\]]*\))?\]\s+(?:private|protected|internal)\s+[\w<>,.?\[\]]+\s+\w+",
        RegexOptions.CultureInvariant);

    [Fact]
    public void ViewModels_UsePartialPropertiesForObservablePropertyGeneration()
    {
        var viewModelDirectory = PathAt("src", "CipherNest.App", "ViewModels");
        var files = Directory.GetFiles(viewModelDirectory, "*.cs", SearchOption.TopDirectoryOnly);

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            Assert.False(
                FieldObservablePattern.IsMatch(source),
                $"{Path.GetFileName(file)} still uses field-based [ObservableProperty], which is not WinRT/AOT compatible on the Windows target.");
        }
    }

    [Fact]
    public void MigratedViewModels_ContainPartialObservableProperties()
    {
        var requiredFiles = new[]
        {
            "AuditViewModel.cs",
            "DeveloperViewModel.cs",
            "GeneratorDefaultsViewModel.cs",
            "GeneratorViewModel.cs",
            "ItemEditorViewModel.cs",
            "OnboardingViewModel.cs",
            "SettingsViewModel.cs",
            "SettingsViewModel.Localization.cs",
            "TransferViewModel.cs",
            "TrashViewModel.cs",
            "UnlockViewModel.cs",
            "VaultViewModel.cs"
        };

        foreach (var fileName in requiredFiles)
        {
            var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", fileName));
            Assert.Contains("public partial", source, StringComparison.Ordinal);
        }
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
