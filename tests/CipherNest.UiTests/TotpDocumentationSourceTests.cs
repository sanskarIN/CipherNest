namespace CipherNest.UiTests;

public sealed class TotpDocumentationSourceTests
{
    [Fact]
    public void TotpSecurityDocument_IsPresentAndKeepsCriticalLimitations()
    {
        var path = PathAt("docs", "security", "TOTP.md");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);

        Assert.Contains("RFC 6238", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not** completed an independent professional security audit", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("generated one-time codes", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not persisted", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("clipboard", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("QR", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocumentationHub_LinksTotpSecurityDocument()
    {
        var hub = File.ReadAllText(PathAt("docs", "README.md"));
        Assert.Contains("security/TOTP.md", hub, StringComparison.Ordinal);
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
