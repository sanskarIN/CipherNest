namespace CipherNest.UiTests;

public sealed class DecryptedRecordValidationSourceTests
{
    [Fact]
    public void DecryptedRecords_AreValidatedBeforeLeavingInfrastructureBoundary()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));
        var methodStart = source.IndexOf("private VaultItem DecryptItem", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = source.IndexOf("private async Task<VaultItem> GetItemRequiredAsync", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var method = source[methodStart..methodEnd];

        Assert.Contains("item.Id != row.Id", method, StringComparison.Ordinal);
        Assert.Contains("VaultItemValidator.Validate(item)", method, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.ZeroMemory(plaintext);", method, StringComparison.Ordinal);
        Assert.Contains("Stored record payload contains invalid metadata.", method, StringComparison.Ordinal);
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
