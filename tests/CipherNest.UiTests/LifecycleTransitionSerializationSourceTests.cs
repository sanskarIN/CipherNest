namespace CipherNest.UiTests;

public sealed class LifecycleTransitionSerializationSourceTests
{
    [Fact]
    public void App_SerializesInactiveAndActiveSecurityTransitions()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "App.xaml.cs"));

        Assert.Contains("private readonly SemaphoreSlim _lifecycleGate = new(1, 1);", source, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(source, "await _lifecycleGate.WaitAsync();"));
        Assert.Equal(2, CountOccurrences(source, "_lifecycleGate.Release();"));
        Assert.Contains("private async Task HandleInactiveAsync()", source, StringComparison.Ordinal);
        Assert.Contains("private async Task HandleActiveAsync()", source, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
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
