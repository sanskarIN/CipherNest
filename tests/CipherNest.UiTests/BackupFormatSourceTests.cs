namespace CipherNest.UiTests;

public sealed class BackupFormatSourceTests
{
    [Fact]
    public void Restore_ValidatesHeaderSchemaAndResourcesBeforeKeyDerivation()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "EncryptedBackupService.cs"));
        var schema = source.IndexOf("BackupHeaderJsonPolicy.Validate(headerJson)", StringComparison.Ordinal);
        var deserialize = source.IndexOf("JsonSerializer.Deserialize<BackupHeader>", schema, StringComparison.Ordinal);
        var validate = source.IndexOf("BackupFormatPolicy.ValidateHeader", deserialize, StringComparison.Ordinal);
        var derive = source.IndexOf("key = _crypto.DeriveKey", validate, StringComparison.Ordinal);

        Assert.True(schema >= 0);
        Assert.True(deserialize > schema);
        Assert.True(validate > deserialize);
        Assert.True(derive > validate);
        Assert.Contains("BackupFormatPolicy.ValidateHeaderLength(headerLength);", source, StringComparison.Ordinal);
        Assert.Contains("header.Salt is null || header.Kdf is null", source, StringComparison.Ordinal);
        Assert.Contains("TryDeleteFile(tempOutput);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void HeaderSchemaPolicy_RejectsAmbiguousAndDeepMetadata()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "BackupHeaderJsonPolicy.cs"));
        var format = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "BackupFormatPolicy.cs"));

        Assert.Contains("MaxDepth = BackupFormatPolicy.MaximumHeaderJsonDepth", source, StringComparison.Ordinal);
        Assert.Contains("Backup header contains duplicate metadata.", source, StringComparison.Ordinal);
        Assert.Contains("Backup header contains unexpected metadata.", source, StringComparison.Ordinal);
        Assert.Contains("Backup key-derivation metadata contains a duplicate property.", source, StringComparison.Ordinal);
        Assert.Contains("Backup key-derivation metadata contains an unexpected property.", source, StringComparison.Ordinal);
        Assert.Contains("public const int MaximumHeaderJsonDepth = 16;", format, StringComparison.Ordinal);
        Assert.Contains("public const int MaximumHeaderBytes = 16_384;", format, StringComparison.Ordinal);
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
