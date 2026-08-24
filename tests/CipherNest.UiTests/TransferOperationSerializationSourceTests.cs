namespace CipherNest.UiTests;

public sealed class TransferOperationSerializationSourceTests
{
    [Fact]
    public void TransferCommands_RefuseOverlapWhileBusy()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TransferViewModel.cs"));

        Assert.True(Count(source, "if (IsBusy) return;") >= 3, "Picker, import, and plaintext export should refuse overlapping operations.");
        Assert.Contains("if (IsBusy) return Task.CompletedTask;", source, StringComparison.Ordinal);
        Assert.Contains("IsBusy = true;", source, StringComparison.Ordinal);
        Assert.Contains("IsBusy = false;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PlaintextAcknowledgement_IsConsumedBeforeFirstAwait()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TransferViewModel.cs"));
        var exportStart = source.IndexOf("private async Task ExportPlaintextAsync()", StringComparison.Ordinal);
        var phraseClear = source.IndexOf("ExportConfirmationPhrase = string.Empty;", exportStart, StringComparison.Ordinal);
        var busySet = source.IndexOf("IsBusy = true;", exportStart, StringComparison.Ordinal);
        var firstAwait = source.IndexOf("await _vault.ReauthenticateAsync", exportStart, StringComparison.Ordinal);

        Assert.True(exportStart >= 0);
        Assert.True(phraseClear > exportStart);
        Assert.True(busySet > exportStart);
        Assert.True(phraseClear < firstAwait, "The plaintext acknowledgement must be consumed before asynchronous re-authentication starts.");
        Assert.True(busySet < firstAwait, "The plaintext export command must become busy before asynchronous re-authentication starts.");
    }

    [Fact]
    public void PlaintextCleanup_CannotMaskPrimaryOperationFailure()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TransferViewModel.cs"));

        Assert.Contains("_exceptions.Report(\"Transfer.ExportPlaintext.TempCleanup\", ex);", source, StringComparison.Ordinal);
        Assert.Contains("StatusMessage += Text(\"TransferCleanupWarningSuffix\");", source, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)", source, StringComparison.Ordinal);
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
