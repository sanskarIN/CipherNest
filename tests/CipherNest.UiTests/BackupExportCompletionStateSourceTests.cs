namespace CipherNest.UiTests;

public sealed class BackupExportCompletionStateSourceTests
{
    [Fact]
    public void BackupCreation_PublishesSuccessBeforeOptionalFollowUpOperations()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.cs"));
        var method = source.IndexOf("private async Task ExportBackupAsync()", StringComparison.Ordinal);
        var export = source.IndexOf("await _backup.ExportEncryptedAsync(path, backupPassphrase);", method, StringComparison.Ordinal);
        var success = source.IndexOf("StatusMessage = SettingsText(\"SettingsBackupCreatedStatus\");", export, StringComparison.Ordinal);
        var reminder = source.IndexOf("Settings.BackupExport.Reminder", success, StringComparison.Ordinal);
        var share = source.IndexOf("Settings.BackupExport.Share", success, StringComparison.Ordinal);
        var navigation = source.IndexOf("Settings.BackupExport.Navigation", success, StringComparison.Ordinal);

        Assert.True(method >= 0);
        Assert.True(export > method);
        Assert.True(success > export);
        Assert.True(reminder > success);
        Assert.True(share > success);
        Assert.True(navigation > success);
    }

    [Fact]
    public void PostCreationFailures_DoNotOverwriteSuccessfulBackupStatus()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.cs"));
        var method = source.IndexOf("private async Task ExportBackupAsync()", StringComparison.Ordinal);
        var success = source.IndexOf("StatusMessage = SettingsText(\"SettingsBackupCreatedStatus\");", method, StringComparison.Ordinal);
        var methodEnd = source.IndexOf("[RelayCommand]\n    private async Task RestoreBackupAsync()", success, StringComparison.Ordinal);
        var afterSuccess = source[success..methodEnd];

        Assert.Contains("_exceptions.Report(\"Settings.BackupExport.Reminder\", ex);", afterSuccess, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Settings.BackupExport.Share\", ex);", afterSuccess, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"Settings.BackupExport.Navigation\", ex);", afterSuccess, StringComparison.Ordinal);
        Assert.DoesNotContain("SettingsBackupFailure", afterSuccess, StringComparison.Ordinal);
    }

    [Fact]
    public void BackupExport_ConsumesBoundPassphraseAndRefusesOverlap()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.cs"));
        var method = source.IndexOf("private async Task ExportBackupAsync()", StringComparison.Ordinal);
        var busyGuard = source.IndexOf("if (IsBusy) return;", method, StringComparison.Ordinal);
        var fieldClear = source.IndexOf("BackupPassphrase = string.Empty;", method, StringComparison.Ordinal);
        var busySet = source.IndexOf("IsBusy = true;", fieldClear, StringComparison.Ordinal);
        var firstAwait = source.IndexOf("await Shell.Current.DisplayAlertAsync", busySet, StringComparison.Ordinal);

        Assert.True(busyGuard > method);
        Assert.True(fieldClear > busyGuard);
        Assert.True(busySet > fieldClear);
        Assert.True(firstAwait > busySet);
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
