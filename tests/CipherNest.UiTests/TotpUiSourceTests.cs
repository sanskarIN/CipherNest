namespace CipherNest.UiTests;

public sealed class TotpUiSourceTests
{
    [Fact]
    public void ItemEditor_ExposesTotpOnlyForTotpItemsAndUsesExplicitCommands()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml"));
        var viewModel = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.Totp.cs"));

        Assert.Contains("IsVisible=\"{Binding IsTotpItem}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("RefreshTotpCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("CopyTotpCodeCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("generated codes are computed locally while unlocked and are not persisted", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value == VaultItemType.OneTimePassword", viewModel, StringComparison.Ordinal);
        Assert.Contains("CopySecretAsync(CurrentTotpCode", viewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("PeriodicTimer", viewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Threading.Timer", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void TotpPresentation_IsClearedWhenSensitiveInputsChange()
    {
        var viewModel = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.Totp.cs"));

        Assert.Contains("OnSecretChanged", viewModel, StringComparison.Ordinal);
        Assert.Contains("OnSelectedTotpAlgorithmChanged", viewModel, StringComparison.Ordinal);
        Assert.Contains("OnTotpDigitsChanged", viewModel, StringComparison.Ordinal);
        Assert.Contains("OnTotpPeriodSecondsChanged", viewModel, StringComparison.Ordinal);
        Assert.Contains("CurrentTotpCode = string.Empty", viewModel, StringComparison.Ordinal);
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
