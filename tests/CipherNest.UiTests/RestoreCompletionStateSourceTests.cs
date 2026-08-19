using System.Xml.Linq;

namespace CipherNest.UiTests;

public sealed class RestoreCompletionStateSourceTests
{
    [Fact]
    public void RestoreCommand_DistinguishesReplacementSuccessFromCleanupFailure()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.cs"));
        var neutral = ReadCatalog("AppStrings.resx");
        var restoreStart = source.IndexOf("private async Task RestoreBackupAsync()", StringComparison.Ordinal);
        var changeMasterStart = source.IndexOf("private async Task ChangeMasterPassphraseAsync()", restoreStart, StringComparison.Ordinal);
        var method = source[restoreStart..changeMasterStart];

        Assert.Contains("var restoreCompleted = false;", method, StringComparison.Ordinal);
        Assert.Contains("restoreCompleted = true;", method, StringComparison.Ordinal);
        Assert.Contains("Settings.BackupRestore.BiometricCleanup", method, StringComparison.Ordinal);
        Assert.Contains("StatusMessage = restoreCompleted", method, StringComparison.Ordinal);
        Assert.Contains("SettingsText(\"SettingsRestorePostCleanupFailure\")", method, StringComparison.Ordinal);
        Assert.Contains("SettingsText(\"SettingsRestoreFailure\")", method, StringComparison.Ordinal);
        Assert.Contains("The backup was restored and the vault remains locked", neutral["SettingsRestorePostCleanupFailure"], StringComparison.Ordinal);
        Assert.Contains("The active vault was not intentionally replaced by this failed restore attempt", neutral["SettingsRestoreFailure"], StringComparison.Ordinal);
    }

    private static Dictionary<string, string> ReadCatalog(string fileName)
    {
        var document = XDocument.Load(PathAt("src", "CipherNest.App", "Resources", "Localization", fileName));
        return document.Root!
            .Elements("data")
            .ToDictionary(
                element => (string)element.Attribute("name")!,
                element => element.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
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
