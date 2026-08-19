namespace CipherNest.UiTests;

public sealed class TranslationExtensionSourceTests
{
    [Fact]
    public void TranslationExtension_UsesRegisteredLocalizationServiceAndFailsClosedOnMissingKey()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "Localization", "TranslateExtension.cs"));

        Assert.Contains("AcceptEmptyServiceProvider", source, StringComparison.Ordinal);
        Assert.Contains("IMarkupExtension", source, StringComparison.Ordinal);
        Assert.Contains("ContentProperty(nameof(Key))", source, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<ILocalizationService>()", source, StringComparison.Ordinal);
        Assert.Contains(".Get(Key)", source, StringComparison.Ordinal);
        Assert.Contains("XamlParseException", source, StringComparison.Ordinal);
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
