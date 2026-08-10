namespace CipherNest.UiTests;

public sealed class StorageMaintenanceSourceTests
{
    [Fact]
    public void DirectoryEnumeration_IsMaterializedInsideGuardedBlocksAndReparseDirectoriesAreSkipped()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Services", "StorageMaintenanceService.cs"));

        Assert.Contains("Directory.EnumerateFiles(directory).ToArray()", source, StringComparison.Ordinal);
        Assert.Contains("Directory.EnumerateDirectories(directory).ToArray()", source, StringComparison.Ordinal);
        Assert.Contains("Directory.EnumerateFiles(root, \"*\", SearchOption.TopDirectoryOnly).ToArray()", source, StringComparison.Ordinal);
        Assert.Contains("Directory.EnumerateDirectories(root, \"*\", SearchOption.TopDirectoryOnly).ToArray()", source, StringComparison.Ordinal);
        Assert.Contains("FileAttributes.ReparsePoint", source, StringComparison.Ordinal);
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
