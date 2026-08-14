namespace CipherNest.UiTests;

public sealed class VaultSearchFailureContainmentSourceTests
{
    [Fact]
    public void FireAndForgetSearch_ReportsNonCancellationFailures()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "VaultViewModel.cs"));

        Assert.Contains("_ = SearchDelayedAsync(value, _searchCts.Token);", source, StringComparison.Ordinal);
        Assert.Contains("catch (OperationCanceledException)", source, StringComparison.Ordinal);
        Assert.Contains("catch (Exception ex)", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Vault.Search\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("if (!cancellationToken.IsCancellationRequested)", source, StringComparison.Ordinal);
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
