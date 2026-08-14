namespace CipherNest.UiTests;

public sealed class RepositoryUiStructureTests
{
    [Fact]
    public void Shell_ContainsCoreSecurityRoutes()
    {
        var shell = File.ReadAllText(PathAt("src", "CipherNest.App", "AppShell.xaml"));
        Assert.Contains("Route=\"unlock\"", shell, StringComparison.Ordinal);
        Assert.Contains("Route=\"vault\"", shell, StringComparison.Ordinal);
        Assert.Contains("Route=\"settings\"", shell, StringComparison.Ordinal);
        Assert.Contains("Route=\"security-info\"", shell, StringComparison.Ordinal);
    }

    [Fact]
    public void SensitiveScreens_ExposeSupportedSemanticDescriptionMetadata()
    {
        var unlock = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "UnlockPage.xaml"));
        var vault = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "VaultPage.xaml"));
        var item = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml"));

        Assert.Contains("SemanticProperties.Description", unlock, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description", vault, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description", item, StringComparison.Ordinal);
        Assert.DoesNotContain("SemanticProperties.LiveSetting", unlock, StringComparison.Ordinal);
        Assert.DoesNotContain("SemanticProperties.LiveSetting", vault, StringComparison.Ordinal);
        Assert.DoesNotContain("SemanticProperties.LiveSetting", item, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalizationCatalog_AndLanguageSettingExist()
    {
        var resx = File.ReadAllText(PathAt("src", "CipherNest.App", "Resources", "Localization", "AppStrings.resx"));
        var settings = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "SettingsPage.xaml"));

        Assert.Contains("ProductName", resx, StringComparison.Ordinal);
        Assert.Contains("SelectedLanguage", settings, StringComparison.Ordinal);
        Assert.Contains("SaveLanguageCommand", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void VaultActions_WrapAndLargeResultSetsLoadIncrementally()
    {
        var vault = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "VaultPage.xaml"));
        var viewModel = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "VaultViewModel.cs"));
        Assert.Contains("<FlexLayout", vault, StringComparison.Ordinal);
        Assert.Contains("Wrap=\"Wrap\"", vault, StringComparison.Ordinal);
        Assert.Contains("LoadMoreCommand", vault, StringComparison.Ordinal);
        Assert.Contains("private const int PageSize = 50", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemEditor_ProvidesExplicitTimedCopyActionsWithoutShowingCustomSecretValues()
    {
        var item = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml"));
        Assert.Contains("CopyUsernameCommand", item, StringComparison.Ordinal);
        Assert.Contains("CopyCustomSecretCommand", item, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding SecretCustomFields}\"", item, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Value}\"", item, StringComparison.Ordinal);
    }

    [Fact]
    public void SensitivePages_ClearCredentialsWhenTheyDisappear()
    {
        string[] paths =
        [
            PathAt("src", "CipherNest.App", "Views", "UnlockPage.xaml.cs"),
            PathAt("src", "CipherNest.App", "Views", "SettingsPage.xaml.cs"),
            PathAt("src", "CipherNest.App", "Views", "TransferPage.xaml.cs"),
            PathAt("src", "CipherNest.App", "Views", "TrashPage.xaml.cs"),
            PathAt("src", "CipherNest.App", "Views", "ItemEditorPage.xaml.cs"),
            PathAt("src", "CipherNest.App", "Views", "OnboardingPage.xaml.cs")
        ];

        foreach (var path in paths)
        {
            var source = File.ReadAllText(path);
            Assert.Contains("OnDisappearing", source, StringComparison.Ordinal);
            Assert.Contains("ClearSensitiveState", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MasterPassphraseChange_EndsSecuritySessionAndLocksVault()
    {
        var settings = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.cs"));
        Assert.Contains("_sessionSecurity.Clear();", settings, StringComparison.Ordinal);
        Assert.Contains("await _vault.LockAsync();", settings, StringComparison.Ordinal);
        Assert.Contains("Settings.ChangeMasterPassphrase.Clipboard", settings, StringComparison.Ordinal);
        Assert.Contains("//unlock", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void BackupRestore_BoundsExternalFileBeforeCacheStaging()
    {
        var settings = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.cs"));

        Assert.Contains("BackupStagingPolicy.CopyToNewFileAsync", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("source.CopyToAsync(destination)", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void TrashManualDeletion_RequiresMasterReauthentication()
    {
        var trash = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "TrashViewModel.cs"));
        var page = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "TrashPage.xaml"));
        Assert.Contains("ConfirmMasterPassphraseAsync", trash, StringComparison.Ordinal);
        Assert.Contains("ReauthenticateAsync", trash, StringComparison.Ordinal);
        Assert.Contains("EmptyTrashCommand", page, StringComparison.Ordinal);
        Assert.Contains("DeletionPassphrase", page, StringComparison.Ordinal);
    }

    [Fact]
    public void UnlockCapabilityProbe_DoesNotWriteRawExceptionMessageToDebug()
    {
        var unlockPage = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "UnlockPage.xaml.cs"));
        Assert.Contains("IPrivacySafeExceptionReporter", unlockPage, StringComparison.Ordinal);
        Assert.DoesNotContain("Debug.WriteLine", unlockPage, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", unlockPage, StringComparison.Ordinal);
    }

    [Fact]
    public void SplashAndBrandingSources_IncludeRequiredCreatorCreditAndVariants()
    {
        var splash = File.ReadAllText(PathAt("src", "CipherNest.App", "Resources", "Splash", "splash.svg"));
        Assert.Contains("CipherNest", splash, StringComparison.Ordinal);
        Assert.Contains("Made by the Sanskar", splash, StringComparison.Ordinal);
        Assert.True(File.Exists(PathAt("src", "CipherNest.App", "Resources", "AppIcon", "appicon-mono.svg")));
        Assert.True(File.Exists(PathAt("src", "CipherNest.App", "Resources", "Images", "ciphernest_logo_dark.svg")));
    }

    [Fact]
    public void ExceptionReporter_DoesNotLogExceptionObjectMessageOrStack()
    {
        var reporter = File.ReadAllText(PathAt("src", "CipherNest.App", "Services", "PrivacySafeExceptionReporter.cs"));
        Assert.DoesNotContain("exception.Message", reporter, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.StackTrace", reporter, StringComparison.Ordinal);
        Assert.DoesNotContain("logger.LogError(NonFatalEvent, exception", reporter, StringComparison.Ordinal);
        Assert.Contains("HResult", reporter, StringComparison.Ordinal);
    }

    [Fact]
    public void AboutSurface_ReferencesLegalPrivacyAndThirdPartyNotices()
    {
        var about = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AboutPage.xaml"));
        Assert.Contains("GPL-3.0-or-later", about, StringComparison.Ordinal);
        Assert.Contains("PRIVACY.md", about, StringComparison.Ordinal);
        Assert.Contains("TERMS.md", about, StringComparison.Ordinal);
        Assert.Contains("THIRD_PARTY_NOTICES.md", about, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectSupportLink_IsCentralizedVisibleRegisteredAndBuildToggleable()
    {
        const string supportUrl = "https://buymeacoffee.com/sanskarIN";
        var constants = File.ReadAllText(PathAt("src", "CipherNest.Shared", "AppConstants.cs"));
        var about = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AboutPage.xaml"));
        var aboutCode = File.ReadAllText(PathAt("src", "CipherNest.App", "Views", "AboutPage.xaml.cs"));
        var featureFlags = File.ReadAllText(PathAt("src", "CipherNest.App", "Services", "BuildFeatureFlags.cs"));
        var project = File.ReadAllText(PathAt("src", "CipherNest.App", "CipherNest.App.csproj"));
        var readme = File.ReadAllText(PathAt("README.md"));
        var support = File.ReadAllText(PathAt("SUPPORT.md"));
        var funding = File.ReadAllText(PathAt(".github", "FUNDING.yml"));

        Assert.Contains($"BuyMeACoffeeUrl = \"{supportUrl}\"", constants, StringComparison.Ordinal);
        Assert.Contains("shared:AppConstants.BuyMeACoffeeUrl", about, StringComparison.Ordinal);
        Assert.Contains("shared:AppConstants.RepositoryUrl", about, StringComparison.Ordinal);
        Assert.Contains("shared:AppConstants.CreatorUrl", about, StringComparison.Ordinal);
        Assert.Contains("shared:AppConstants.BusinessEmail", about, StringComparison.Ordinal);
        Assert.Contains("shared:AppConstants.SupportEmail", about, StringComparison.Ordinal);
        Assert.Contains("SupportDevelopmentFrame", about, StringComparison.Ordinal);
        Assert.Contains("SupportDevelopmentMetadataLabel", about, StringComparison.Ordinal);
        Assert.Contains("OnBuyMeACoffeeClicked", about, StringComparison.Ordinal);
        Assert.Contains("BuildFeatureFlags.IsFundingLinkEnabled", aboutCode, StringComparison.Ordinal);
        Assert.Contains("AppConstants.BuyMeACoffeeUrl", aboutCode, StringComparison.Ordinal);
        Assert.Contains("_exceptions.Report(\"About.ExternalLink\"", aboutCode, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", aboutCode, StringComparison.Ordinal);
        Assert.Contains("DisplayAlertAsync", aboutCode, StringComparison.Ordinal);
        Assert.Contains("CIPHERNEST_DISABLE_FUNDING_LINK", featureFlags, StringComparison.Ordinal);
        Assert.Contains("CipherNestEnableFundingLink", project, StringComparison.Ordinal);
        Assert.Contains(supportUrl, readme, StringComparison.Ordinal);
        Assert.Contains(supportUrl, support, StringComparison.Ordinal);
        Assert.Contains(supportUrl, funding, StringComparison.Ordinal);
        Assert.Contains("voluntary", about, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContinuousIntegration_CompilesAllTargetFamiliesAndRunsReproducibleCoreGates()
    {
        var ci = File.ReadAllText(PathAt(".github", "workflows", "dotnet-desktop.yml"));
        var codeql = File.ReadAllText(PathAt(".github", "workflows", "codeql.yml"));
        var dependencyReview = File.ReadAllText(PathAt(".github", "workflows", "dependency-review.yml"));

        Assert.Contains("build-windows:", ci, StringComparison.Ordinal);
        Assert.Contains("build-android:", ci, StringComparison.Ordinal);
        Assert.Contains("build-apple:", ci, StringComparison.Ordinal);
        Assert.Contains("net10.0-android", ci, StringComparison.Ordinal);
        Assert.Contains("net10.0-ios", ci, StringComparison.Ordinal);
        Assert.Contains("net10.0-maccatalyst", ci, StringComparison.Ordinal);
        Assert.Contains("CipherNestEnableFundingLink=false", ci, StringComparison.Ordinal);
        Assert.Contains("dotnet format", ci, StringComparison.Ordinal);
        Assert.Contains("cancel-in-progress: true", ci, StringComparison.Ordinal);
        Assert.Contains("timeout-minutes:", ci, StringComparison.Ordinal);

        Assert.Contains("maui-android", codeql, StringComparison.Ordinal);
        Assert.Contains("src/CipherNest.App/CipherNest.App.csproj", codeql, StringComparison.Ordinal);
        Assert.Contains("cancel-in-progress: true", codeql, StringComparison.Ordinal);
        Assert.Contains("timeout-minutes:", codeql, StringComparison.Ordinal);

        Assert.Contains("NuGetAudit=true", dependencyReview, StringComparison.Ordinal);
        Assert.Contains("NuGetAuditMode=all", dependencyReview, StringComparison.Ordinal);
        Assert.Contains("CipherNest.UnitTests", dependencyReview, StringComparison.Ordinal);
        Assert.Contains("CipherNest.IntegrationTests", dependencyReview, StringComparison.Ordinal);
        Assert.Contains("CipherNest.UiTests", dependencyReview, StringComparison.Ordinal);
        Assert.Contains("cancel-in-progress: true", dependencyReview, StringComparison.Ordinal);
        Assert.Contains("timeout-minutes:", dependencyReview, StringComparison.Ordinal);

        Assert.True(File.Exists(PathAt("scripts", "verify-core.ps1")));
        Assert.True(File.Exists(PathAt("scripts", "verify-core.sh")));
        Assert.True(File.Exists(PathAt("scripts", "verify-windows.ps1")));
        Assert.True(File.Exists(PathAt("scripts", "verify-android.sh")));
        Assert.True(File.Exists(PathAt("scripts", "verify-apple.sh")));
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
