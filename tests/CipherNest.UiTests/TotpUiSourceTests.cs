namespace CipherNest.UiTests;

public sealed class TotpUiSourceTests
{
    [Fact]
    public void ItemEditor_ExposesTotpOnlyForTotpItemsAndUsesExplicitCommands()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml"));
        var viewModel = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.Totp.cs"));

        Assert.Contains("IsVisible=\"{Binding IsTotpItem}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("RefreshTotpCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("CopyTotpCodeCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("ImportTotpUriCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("CopyTotpUriCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("generated codes are computed locally while unlocked and are not persisted", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value == VaultItemType.OneTimePassword", viewModel, StringComparison.Ordinal);
        Assert.Contains("CopySecretAsync(CurrentTotpCode", viewModel, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<ITotpUriCodec>().Parse", viewModel, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<ITotpUriCodec>().Format", viewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("PeriodicTimer", viewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Threading.Timer", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void TotpPresentation_IsClearedWhenSensitiveInputsChange()
    {
        var viewModel = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.Totp.cs"));

        Assert.Contains("OnSecretChanged", viewModel, StringComparison.Ordinal);
        Assert.Contains("OnSelectedTotpAlgorithmChanged", viewModel, StringComparison.Ordinal);
        Assert.Contains("OnTotpDigitsChanged", viewModel, StringComparison.Ordinal);
        Assert.Contains("OnTotpPeriodSecondsChanged", viewModel, StringComparison.Ordinal);
        Assert.Contains("CurrentTotpCode = string.Empty", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void TotpUriImport_IsTreatedAsSensitiveTransientInput()
    {
        var xaml = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml"));
        var totpViewModel = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.Totp.cs"));
        var clipboardViewModel = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.Clipboard.cs"));
        var vaultItem = File.ReadAllText(PathAt("src", "CipherNest.Domain", "Models", "VaultItem.cs"));

        Assert.Contains("Text=\"{Binding TotpUriImportText}\" IsPassword=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HOTP is intentionally rejected", xaml, StringComparison.Ordinal);
        Assert.Contains("TotpUriImportText = string.Empty", totpViewModel, StringComparison.Ordinal);
        Assert.Contains("CopySecretAsync(uriText", totpViewModel, StringComparison.Ordinal);
        Assert.Contains("TotpUriImportText = string.Empty", clipboardViewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("TotpUri", vaultItem, StringComparison.Ordinal);
        Assert.DoesNotContain("OtpAuth", vaultItem, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TotpUriImport_ClearsSensitiveTextBeforeEligibilityGuardReturns()
    {
        var viewModel = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "ItemEditorViewModel.Totp.cs"));
        var methodStart = viewModel.IndexOf("private void ImportTotpUri()", StringComparison.Ordinal);
        var capture = viewModel.IndexOf("var uriText = TotpUriImportText;", methodStart, StringComparison.Ordinal);
        var clear = viewModel.IndexOf("TotpUriImportText = string.Empty;", capture, StringComparison.Ordinal);
        var guard = viewModel.IndexOf("if (!IsTotpItem || IsReauthenticationRequired) return;", clear, StringComparison.Ordinal);

        Assert.True(methodStart >= 0, "ImportTotpUri method was not found.");
        Assert.True(capture > methodStart, "TOTP setup URI must be captured inside ImportTotpUri.");
        Assert.True(clear > capture, "The bound setup URI must be cleared immediately after capture.");
        Assert.True(guard > clear, "The bound setup URI must be cleared before an ineligible/reauthentication guard can return.");
    }

    [Fact]
    public void TotpUriCodec_IsRegisteredExactlyOnceAndRemainsLocalOnly()
    {
        var composition = File.ReadAllText(PathAt("src", "CipherNest.App", "MauiProgram.cs"));
        var codec = File.ReadAllText(PathAt("src", "CipherNest.Infrastructure", "Services", "TotpUriCodec.cs"));
        const string registration = "AddSingleton<ITotpUriCodec, TotpUriCodec>()";

        Assert.Contains(registration, composition, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(composition, registration));
        Assert.Contains("MaximumUriCharacters = 8_192", codec, StringComparison.Ordinal);
        Assert.Contains("MaximumQueryPairs = 16", codec, StringComparison.Ordinal);
        Assert.Contains("StringComparer.OrdinalIgnoreCase", codec, StringComparison.Ordinal);
        Assert.Contains("TotpPolicy.NormalizeSecret", codec, StringComparison.Ordinal);
        Assert.Contains("TotpPolicy.ValidateSettings", codec, StringComparison.Ordinal);
        Assert.Contains("Only otpauth://totp/... URIs are supported; HOTP is not supported.", codec, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", codec, StringComparison.Ordinal);
        Assert.DoesNotContain("WebRequest", codec, StringComparison.Ordinal);
        Assert.DoesNotContain("ZXing", codec, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Camera", codec, StringComparison.OrdinalIgnoreCase);
    }

    private static int CountOccurrences(string value, string needle)
    {
        var count = 0;
        var offset = 0;
        while ((offset = value.IndexOf(needle, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += needle.Length;
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
