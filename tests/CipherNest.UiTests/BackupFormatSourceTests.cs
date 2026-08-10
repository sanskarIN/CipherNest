namespace CipherNest.UiTests;

public sealed class BackupFormatSourceTests
{
    [Fact]
    public void Restore_ValidatesHeaderBeforeKeyDerivation()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "EncryptedBackupService.cs"));
        var validate = source.IndexOf("BackupFormatPolicy.ValidateHeader", StringComparison.Ordinal);
        var derive = source.IndexOf("key = _crypto.DeriveKey", validate, StringComparison.Ordinal);

        Assert.True(validate >= 0);
        Assert.True(derive > validate);
        Assert.Contains("header.Salt is null || header.Kdf is null", source, StringComparison.Ordinal);
        Assert.Contains("TryDeleteFile(tempOutput);", source, StringComparison.Ordinal);
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
