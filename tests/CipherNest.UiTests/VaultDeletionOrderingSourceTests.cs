namespace CipherNest.UiTests;

public sealed class VaultDeletionOrderingSourceTests
{
    [Fact]
    public void PermanentDeletion_RemovesDatabaseRecordBeforeBestEffortAttachmentCleanup()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));
        var methodStart = source.IndexOf("public async Task DeletePermanentlyAsync", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = source.IndexOf("public async Task<IReadOnlyList<VaultItem>> SearchAsync", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var method = source[methodStart..methodEnd];

        var recordDelete = method.IndexOf("await _store.DeleteItemAsync", StringComparison.Ordinal);
        var attachmentDelete = method.IndexOf("TryDeleteAttachment", StringComparison.Ordinal);
        Assert.True(recordDelete >= 0);
        Assert.True(attachmentDelete > recordDelete);
        Assert.Contains("catch (IOException)", source, StringComparison.Ordinal);
        Assert.Contains("catch (UnauthorizedAccessException)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultHeader_RejectsUnsupportedVersions()
    {
        var service = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultService.cs"));
        var policy = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "VaultHeaderJsonPolicy.cs"));

        Assert.Contains("public const int MinimumSupportedVersion = 1;", policy, StringComparison.Ordinal);
        Assert.Contains("public const int CurrentVersion = 2;", policy, StringComparison.Ordinal);
        Assert.Contains("version is < MinimumSupportedVersion or > CurrentVersion", policy, StringComparison.Ordinal);
        Assert.Contains("header.Version is < VaultHeaderJsonPolicy.MinimumSupportedVersion or > VaultHeaderJsonPolicy.CurrentVersion", service, StringComparison.Ordinal);
        Assert.Contains("VaultHeaderJsonPolicy.Validate(headerJson);", service, StringComparison.Ordinal);
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
