namespace CipherNest.UiTests;

public sealed class SensitiveErrorSurfaceSourceTests
{
    [Fact]
    public void SettingsAndTransfer_DoNotRenderRawExceptionMessages()
    {
        var settings = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.cs"));
        var transfer = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TransferViewModel.cs"));

        Assert.DoesNotContain("ex.Message", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", transfer, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Settings.BackupExport\"", settings, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Settings.BackupRestore\"", settings, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Settings.DeleteVault\"", settings, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Transfer.PickCsv\"", transfer, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Transfer.ImportConfirm\"", transfer, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Transfer.ImportCsv\"", transfer, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Transfer.ExportPlaintext.Reauthenticate\"", transfer, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Transfer.ExportPlaintext.Confirm\"", transfer, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Transfer.ExportPlaintext\"", transfer, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Transfer.ExportPlaintext.TempCleanup\"", transfer, StringComparison.Ordinal);
        Assert.Contains("if (File.Exists(plaintextPath)) File.Delete(plaintextPath);", transfer, StringComparison.Ordinal);
    }

    [Fact]
    public void AttachmentFileFailures_UseRedactedReporterAndUniqueTempNames()
    {
        var editor = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.cs"));

        Assert.Contains("_exceptions.Report(\"ItemEditor.AddAttachment\"", editor, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"ItemEditor.ExportAttachment\"", editor, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"ItemEditor.ExportAttachment.TempCleanup\"", editor, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"ItemEditor.Load\"", editor, StringComparison.Ordinal);
        Assert.Contains("Guid.NewGuid():N", editor, StringComparison.Ordinal);
        Assert.DoesNotContain("Attachment export failed: {ex.Message}", editor, StringComparison.Ordinal);
        Assert.DoesNotContain("Could not open this item: {ex.Message}", editor, StringComparison.Ordinal);
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
