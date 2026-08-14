namespace CipherNest.UiTests;

public sealed class SettingsPublicationCancellationSourceTests
{
    [Fact]
    public void SettingsStore_ChecksCancellationAfterStagingAndBeforeAtomicMove()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "JsonSettingsStore.cs"));
        var cancellationIndex = source.IndexOf("cancellationToken.ThrowIfCancellationRequested();", StringComparison.Ordinal);
        var moveIndex = source.IndexOf("File.Move(temp, _path, overwrite: true);", StringComparison.Ordinal);

        Assert.True(cancellationIndex >= 0, "Settings publication must have a final cancellation check.");
        Assert.True(moveIndex > cancellationIndex, "The final cancellation check must occur before settings are atomically published.");
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
