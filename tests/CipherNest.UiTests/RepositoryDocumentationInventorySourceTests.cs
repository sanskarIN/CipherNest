using System.Diagnostics;

namespace CipherNest.UiTests;

public sealed class RepositoryDocumentationInventorySourceTests
{
    private static readonly string[][] RequiredCurrentDocumentation =
    [
        ["docs", "REPOSITORY_FILE_REFERENCE.md"],
        ["docs", "SOURCE_CODE_REFERENCE.md"],
        ["docs", "TEST_SUITE_REFERENCE.md"],
        ["docs", "verification", "TOTP_URI_INTEROPERABILITY_2026_08_18.md"],
        ["docs", "verification", "TOTP_LOCALIZATION_2026_08_19.md"],
        ["docs", "verification", "AUTHENTICATION_LOCALIZATION_2026_08_19.md"],
        ["docs", "verification", "ABOUT_SECURITY_LOCALIZATION_2026_08_19.md"],
        ["docs", "verification", "SETTINGS_SURFACE_LOCALIZATION_2026_08_19.md"],
        ["docs", "verification", "REPOSITORY_WIDE_DOCUMENTATION_2026_08_19.md"],
        ["docs", "history", "what_changed_through_2026_08_15.md"],
        ["docs", "history", "what_changed_through_2026_08_18.md"]
    ];

    [Fact]
    public void CurrentRepositoryDocumentationArtifacts_ArePresentAndNonEmpty()
    {
        foreach (var segments in RequiredCurrentDocumentation)
        {
            var path = PathAt(segments);
            Assert.True(File.Exists(path), $"Required current documentation file is missing: {string.Join("/", segments)}");
            Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(path)));
        }
    }

    [Fact]
    public void DocumentationHub_LinksRepositoryWideInventoriesAndCurrentVerification()
    {
        var hub = File.ReadAllText(PathAt("docs", "README.md"));

        foreach (var expected in new[]
                 {
                     "REPOSITORY_FILE_REFERENCE.md",
                     "SOURCE_CODE_REFERENCE.md",
                     "TEST_SUITE_REFERENCE.md",
                     "verification/TOTP_URI_INTEROPERABILITY_2026_08_18.md",
                     "verification/TOTP_LOCALIZATION_2026_08_19.md",
                     "verification/AUTHENTICATION_LOCALIZATION_2026_08_19.md",
                     "verification/ABOUT_SECURITY_LOCALIZATION_2026_08_19.md",
                     "verification/SETTINGS_SURFACE_LOCALIZATION_2026_08_19.md",
                     "verification/REPOSITORY_WIDE_DOCUMENTATION_2026_08_19.md"
                 })
        {
            Assert.Contains(expected, hub, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RepositoryFileReference_DelegatesProductionAndTestFilesToExhaustiveReferences()
    {
        var repositoryReference = File.ReadAllText(PathAt("docs", "REPOSITORY_FILE_REFERENCE.md"));

        Assert.Contains("SOURCE_CODE_REFERENCE.md", repositoryReference, StringComparison.Ordinal);
        Assert.Contains("TEST_SUITE_REFERENCE.md", repositoryReference, StringComparison.Ordinal);
        Assert.Contains(".github/workflows/dotnet-desktop.yml", repositoryReference, StringComparison.Ordinal);
        Assert.Contains("scripts/verify-core.ps1", repositoryReference, StringComparison.Ordinal);
        Assert.Contains("docs/verification/SETTINGS_SURFACE_LOCALIZATION_2026_08_19.md", repositoryReference, StringComparison.Ordinal);
    }

    [Fact]
    public void RepositoryInventories_ContainEveryTrackedFile()
    {
        var repositoryReference = File.ReadAllText(PathAt("docs", "REPOSITORY_FILE_REFERENCE.md"));
        var sourceReference = File.ReadAllText(PathAt("docs", "SOURCE_CODE_REFERENCE.md"));
        var testReference = File.ReadAllText(PathAt("docs", "TEST_SUITE_REFERENCE.md"));
        var missing = new List<string>();

        foreach (var trackedPath in TrackedFiles())
        {
            var inventoryName = "docs/REPOSITORY_FILE_REFERENCE.md";
            var inventory = repositoryReference;

            if (trackedPath.StartsWith("src/", StringComparison.Ordinal))
            {
                inventoryName = "docs/SOURCE_CODE_REFERENCE.md";
                inventory = sourceReference;
            }
            else if (trackedPath.StartsWith("tests/", StringComparison.Ordinal))
            {
                inventoryName = "docs/TEST_SUITE_REFERENCE.md";
                inventory = testReference;
            }

            if (!inventory.Contains(trackedPath, StringComparison.Ordinal))
                missing.Add($"{trackedPath} -> {inventoryName}");
        }

        Assert.True(
            missing.Count == 0,
            "Every tracked file must be present in its canonical documentation inventory. Missing entries:\n" +
            string.Join("\n", missing.Select(entry => $"- {entry}")));
    }

    [Fact]
    public void SourceAndTestReferences_ContainRepresentativeFilesFromEveryLayerAndSuite()
    {
        var sourceReference = File.ReadAllText(PathAt("docs", "SOURCE_CODE_REFERENCE.md"));
        var testReference = File.ReadAllText(PathAt("docs", "TEST_SUITE_REFERENCE.md"));

        foreach (var expected in new[]
                 {
                     "src/CipherNest.App/MauiProgram.cs",
                     "src/CipherNest.Application/Abstractions/IVaultService.cs",
                     "src/CipherNest.Domain/Models/VaultItem.cs",
                     "src/CipherNest.Infrastructure/Crypto/CryptoService.cs",
                     "src/CipherNest.Infrastructure/Services/VaultService.cs",
                     "src/CipherNest.Shared/VaultStorageLimits.cs"
                 })
        {
            Assert.Contains(expected, sourceReference, StringComparison.Ordinal);
        }

        foreach (var expected in new[]
                 {
                     "tests/Directory.Build.props",
                     "tests/CipherNest.UnitTests/CryptoServiceTests.cs",
                     "tests/CipherNest.IntegrationTests/VaultIntegrationTests.cs",
                     "tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs"
                 })
        {
            Assert.Contains(expected, testReference, StringComparison.Ordinal);
        }
    }

    private static IReadOnlyList<string> TrackedFiles()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = RepositoryRoot().FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("ls-files");
        startInfo.ArgumentList.Add("-z");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start git to enumerate tracked repository files.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, $"git ls-files failed: {error}");

        return output
            .Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(path => path.Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static DirectoryInfo RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CipherNest.slnx")))
            directory = directory.Parent;

        return directory
            ?? throw new DirectoryNotFoundException("Could not locate the CipherNest repository root from the test output directory.");
    }

    private static string PathAt(params string[] segments)
    {
        var path = RepositoryRoot().FullName;
        foreach (var segment in segments)
            path = Path.Combine(path, segment);
        return path;
    }
}
