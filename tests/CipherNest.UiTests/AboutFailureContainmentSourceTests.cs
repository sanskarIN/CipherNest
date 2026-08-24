namespace CipherNest.UiTests;

public sealed class AboutFailureContainmentSourceTests
{
    [Fact]
    public void AboutNavigationHandlers_UseContainedNavigationPath()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AboutPage.xaml.cs"));

        Assert.Contains("NavigateSafelyAsync(\"//vault\", \"About.Navigate.Back\")", source, StringComparison.Ordinal);
        Assert.Contains("NavigateSafelyAsync(\"//security-info\", \"About.Navigate.SecurityInfo\")", source, StringComparison.Ordinal);
        Assert.Contains("NavigateSafelyAsync(\"//developer\", \"About.Navigate.Developer\")", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(operation, ex);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AboutExternalLinkAlerts_AreContained()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AboutPage.xaml.cs"));

        Assert.Contains("ShowAlertSafelyAsync", source, StringComparison.Ordinal);
        Assert.Contains("About.ExternalLink.Invalid.Alert", source, StringComparison.Ordinal);
        Assert.Contains("About.ExternalLink.Unavailable.Alert", source, StringComparison.Ordinal);
        Assert.Contains("About.ExternalLink.Failure.Alert", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"About.ExternalLink\", ex);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.StackTrace", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AboutAlertHelper_ContainsSecondaryAlertExceptions()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AboutPage.xaml.cs"));

        Assert.Contains("private async Task ShowAlertSafelyAsync", source, StringComparison.Ordinal);
        Assert.Contains("await DisplayAlertAsync(title, message, cancel);", source, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(operation, ex);", source, StringComparison.Ordinal);
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
