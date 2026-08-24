namespace CipherNest.UiTests;

public sealed class TrashFailureContainmentSourceTests
{
    [Fact]
    public void TrashOperations_ReportFailuresWithoutRawExceptionText()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TrashViewModel.cs"));

        foreach (var operation in new[]
        {
            "Trash.Load",
            "Trash.Restore",
            "Trash.Delete.Confirm",
            "Trash.Delete",
            "Trash.Empty.Confirm",
            "Trash.Empty",
            "Trash.Reauthenticate"
        })
        {
            Assert.Contains($"_exceptions.Report(\"{operation}\", ex);", source, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("ex.Message", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.StackTrace", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TrashMasterPassphrase_IsClearedBeforeReauthenticationAwait()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TrashViewModel.cs"));
        var method = source.IndexOf("private async Task<bool> ConfirmMasterPassphraseAsync()", StringComparison.Ordinal);
        var capture = source.IndexOf("var passphrase = DeletionPassphrase;", method, StringComparison.Ordinal);
        var clear = source.IndexOf("DeletionPassphrase = string.Empty;", capture, StringComparison.Ordinal);
        var reauthenticate = source.IndexOf("await _vault.ReauthenticateAsync(passphrase)", clear, StringComparison.Ordinal);

        Assert.True(method >= 0);
        Assert.True(capture > method);
        Assert.True(clear > capture);
        Assert.True(reauthenticate > clear, "The bound destructive credential must be cleared before asynchronous re-authentication starts.");
        Assert.Contains("finally\n        {\n            passphrase = string.Empty;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TrashCommands_RefuseOverlapAndClearStaleDecryptedItems()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TrashViewModel.cs"));

        Assert.True(Count(source, "IsBusy") > 8);
        Assert.Contains("if (IsBusy) return;", source, StringComparison.Ordinal);
        Assert.Contains("if (item is null || IsBusy) return;", source, StringComparison.Ordinal);
        Assert.Contains("public void ClearSensitiveState()", source, StringComparison.Ordinal);
        Assert.Contains("DeletionPassphrase = string.Empty;\n        Items.Clear();", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PartialEmptyFailure_DoesNotPresentStaleTrashList()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TrashViewModel.cs"));

        Assert.Contains("_exceptions.Report(\"Trash.Empty\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("Items.Clear();\n                StatusMessage = TrashText(\"TrashEmptyFailureStatus\");", source, StringComparison.Ordinal);
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
