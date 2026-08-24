using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class ReleaseVersionSourceTests
{
    [Fact]
    public void AppProject_DeclaresVersion248AndMonotonicBuildCode()
    {
        var projectPath = PathAt("src", "CipherNest.App", "CipherNest.App.csproj");
        var document = XDocument.Load(projectPath);
        var properties = document.Root!
            .Elements("PropertyGroup")
            .SelectMany(static group => group.Elements())
            .ToDictionary(static element => element.Name.LocalName, static element => element.Value, StringComparer.Ordinal);

        Assert.Equal("2.4.8", properties["ApplicationDisplayVersion"]);
        Assert.Equal("20408", properties["ApplicationVersion"]);
        Assert.True(int.TryParse(properties["ApplicationVersion"], out var buildCode));
        Assert.True(buildCode > 1);
    }

    [Fact]
    public void ReleaseDocumentation_Identifies248AsPreparationNotVerifiedRelease()
    {
        var status = File.ReadAllText(PathAt("PROJECT_STATUS.md"));
        var changelog = File.ReadAllText(PathAt("CHANGELOG.md"));

        Assert.Contains("2.4.8", status, StringComparison.Ordinal);
        Assert.Contains("release candidate", status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2.4.8", changelog, StringComparison.Ordinal);
        Assert.Contains("unreleased", changelog, StringComparison.OrdinalIgnoreCase);
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
