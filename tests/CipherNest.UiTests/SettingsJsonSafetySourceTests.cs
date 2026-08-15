namespace CipherNest.UiTests;

public sealed class SettingsJsonSafetySourceTests
{
    [Fact]
    public void JsonSettingsStore_EnforcesReadBudgetAndDepthBeforePublication()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "JsonSettingsStore.cs"));

        Assert.Contains("public const long MaximumSettingsFileBytes = 64 * 1024", source, StringComparison.Ordinal);
        Assert.Contains("public const int MaximumSettingsJsonDepth = 16", source, StringComparison.Ordinal);
        Assert.Contains("MaxDepth = MaximumSettingsJsonDepth", source, StringComparison.Ordinal);
        Assert.Contains("GC.AllocateUninitializedArray<byte>(checked((int)MaximumSettingsFileBytes + 1))", source, StringComparison.Ordinal);
        Assert.Contains("while (totalRead < buffer.Length)", source, StringComparison.Ordinal);
        Assert.Contains("if (totalRead > MaximumSettingsFileBytes)", source, StringComparison.Ordinal);
        Assert.Contains("new MemoryStream(buffer, 0, totalRead, writable: false, publiclyVisible: false)", source, StringComparison.Ordinal);
        Assert.Contains("AppPreferencesPolicy.Normalize(loaded ?? new AppPreferences())", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ReadToEnd", source, StringComparison.Ordinal);
    }

    private static string PathAt(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CipherNest.slnx")))
            directory = directory.Parent;
        if (directory is null)
            throw new DirectoryNotFoundException("Could not locate the CipherNest repository root from the test output directory.");

        var path = directory.FullName;
        foreach (var segment in segments)
            path = Path.Combine(path, segment);
        return path;
    }
}
