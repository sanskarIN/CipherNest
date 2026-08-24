namespace CipherNest.UiTests;

public sealed class AuditLifecycleSourceTests
{
    [Fact]
    public void AuditRun_RefusesOverlapBeforeAsyncWork()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "AuditViewModel.cs"));
        var runStart = source.IndexOf("public async Task RunAsync()", StringComparison.Ordinal);
        var busyGuard = source.IndexOf("if (IsBusy) return;", runStart, StringComparison.Ordinal);
        var busySet = source.IndexOf("IsBusy = true;", runStart, StringComparison.Ordinal);
        var firstAwait = source.IndexOf("await Shell.Current.GoToAsync", runStart, StringComparison.Ordinal);

        Assert.True(runStart >= 0);
        Assert.True(busyGuard > runStart);
        Assert.True(busySet > busyGuard);
        Assert.True(firstAwait > busySet, "Audit should become busy before any asynchronous navigation/read can overlap.");
    }

    [Fact]
    public void AuditState_IsClearedWhenLockedOrPageDisappears()
    {
        var viewModel = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "AuditViewModel.cs"));
        var page = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AuditPage.xaml.cs"));

        Assert.Contains("if (!_vault.IsUnlocked)\n            {\n                ClearSensitiveState();", viewModel, StringComparison.Ordinal);
        Assert.Contains("public void ClearSensitiveState()", viewModel, StringComparison.Ordinal);
        Assert.Contains("Findings.Clear();\n        Summary = AuditText(\"AuditInitialSummary\");", viewModel, StringComparison.Ordinal);
        Assert.Contains("protected override void OnDisappearing()", page, StringComparison.Ordinal);
        Assert.Contains("vm.ClearSensitiveState();", page, StringComparison.Ordinal);
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
