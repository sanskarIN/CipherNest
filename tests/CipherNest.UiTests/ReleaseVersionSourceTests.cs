using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class ReleaseVersionSourceTests
{
    [Fact]
    public void PackagingAndRuntimeMetadata_DeclareVersion248AndMonotonicBuildCode()
    {
        var projectPath = PathAt("src", "CipherNest.App", "CipherNest.App.csproj");
        var document = XDocument.Load(projectPath);
        var displayVersion = document.Descendants("ApplicationDisplayVersion").Single().Value;
        var applicationVersion = document.Descendants("ApplicationVersion").Single().Value;
        var constants = File.ReadAllText(PathAt("src", "CipherNest.Shared", "AppConstants.cs"));
        var windowsManifest = XDocument.Load(PathAt("src", "CipherNest.App", "Platforms", "Windows", "Package.appxmanifest"));
        var identity = windowsManifest.Descendants().Single(static element => element.Name.LocalName == "Identity");

        Assert.Equal("2.4.8", displayVersion);
        Assert.Equal("20408", applicationVersion);
        Assert.Contains("public const string Version = \"2.4.8\";", constants, StringComparison.Ordinal);
        Assert.Equal("2.4.8.0", (string?)identity.Attribute("Version"));
        Assert.True(int.TryParse(applicationVersion, out var buildCode));
        Assert.True(buildCode > 1);
    }

    [Fact]
    public void RedactedDiagnostics_UsesTheSharedReleaseVersion()
    {
        var developer = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "DeveloperViewModel.cs"));

        Assert.Contains("AppVersion: {AppConstants.Version}", developer, StringComparison.Ordinal);
        Assert.DoesNotContain("AppVersion: 0.1.0", developer, StringComparison.Ordinal);
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
