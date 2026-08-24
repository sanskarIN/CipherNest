namespace CipherNest.UiTests;

public sealed class VaultSensitiveStateLifecycleSourceTests
{
    [Fact]
    public void VaultPage_ActivatesOnEntryAndClearsOnDisappearing()
    {
        var page = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "VaultPage.xaml.cs"));
        var viewModel = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "VaultViewModel.cs"));

        Assert.Contains("vm.Activate();", page, StringComparison.Ordinal);
        Assert.Contains("protected override void OnDisappearing()", page, StringComparison.Ordinal);
        Assert.Contains("vm.ClearSensitiveState();", page, StringComparison.Ordinal);
        Assert.Contains("public void Activate() => _isPageActive = true;", viewModel, StringComparison.Ordinal);
        Assert.Contains("public void ClearSensitiveState()", viewModel, StringComparison.Ordinal);
        Assert.Contains("_isPageActive = false;", viewModel, StringComparison.Ordinal);
        Assert.Contains("Items.Clear();", viewModel, StringComparison.Ordinal);
        Assert.Contains("_lastResults = Array.Empty<VaultItem>();", viewModel, StringComparison.Ordinal);
        Assert.Contains("_orderedFilteredResults = Array.Empty<VaultItem>();", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultSensitiveCleanup_CancelsPendingSearchWithoutStartingAnotherSearch()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "VaultViewModel.cs"));

        Assert.Contains("private bool _suppressSearch;", source, StringComparison.Ordinal);
        Assert.Contains("if (_suppressSearch) return;", source, StringComparison.Ordinal);
        Assert.Contains("CancelPendingSearch();", source, StringComparison.Ordinal);
        Assert.Contains("_searchCts = null;", source, StringComparison.Ordinal);
        Assert.Contains("_suppressSearch = true;", source, StringComparison.Ordinal);
        Assert.Contains("SearchText = string.Empty;", source, StringComparison.Ordinal);
        Assert.Contains("_suppressSearch = false;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultLoad_RefusesOverlapAndClearsStaleResultsOnFailureOrLock()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "VaultViewModel.cs"));

        Assert.Contains("if (!_isPageActive || IsBusy) return;", source, StringComparison.Ordinal);
        Assert.Contains("if (!_vault.IsUnlocked)\n        {\n            ClearSensitiveState();", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Vault.Load\", ex);\n            ClearResultState();", source, StringComparison.Ordinal);
        Assert.Contains("ClearSensitiveState();\n        await Shell.Current.GoToAsync(\"//unlock\");", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultLoadAndSearch_DoNotPublishResultsWhenPageIsInactive()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "VaultViewModel.cs"));

        Assert.True(Count(source, "if (!_isPageActive || !_vault.IsUnlocked)") >= 3);
        Assert.Contains("if (_isPageActive && _vault.IsUnlocked && !cancellationToken.IsCancellationRequested)\n                    ReplaceItems(results);", source, StringComparison.Ordinal);
        Assert.Contains("if (!cancellationToken.IsCancellationRequested && _isPageActive)", source, StringComparison.Ordinal);
    }

    private static int Count(string source, string value)
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
