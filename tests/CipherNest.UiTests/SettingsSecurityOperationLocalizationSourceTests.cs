namespace CipherNest.UiTests;

public sealed class SettingsSecurityOperationLocalizationSourceTests
{
    [Fact]
    public void SettingsViewModel_UsesReviewedResourcesForSecurityOperations()
    {
        var source = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.cs"));
        var localization = File.ReadAllText(PathAt("src", "CipherNest.App", "ViewModels", "SettingsViewModel.Localization.cs"));

        Assert.Contains("SettingsText(string key)", localization, StringComparison.Ordinal);
        Assert.Contains("SettingsFormat(string key", localization, StringComparison.Ordinal);
        Assert.Contains("CultureInfo.CurrentUICulture", localization, StringComparison.Ordinal);
        Assert.Contains("LocalizedStrengthLabel", localization, StringComparison.Ordinal);

        foreach (var key in new[]
        {
            "SettingsScreenshotSupported",
            "SettingsScreenshotUnavailable",
            "SettingsBiometricConfigured",
            "SettingsBiometricAvailableNotConfigured",
            "SettingsBiometricUnavailable",
            "SettingsLoadFailure",
            "SettingsSavedStatus",
            "SettingsSaveFailure",
            "SettingsEnableBiometricMasterRequired",
            "SettingsBiometricUnavailableStatus",
            "SettingsMasterConfirmationFailed",
            "SettingsEnableBiometricPrompt",
            "SettingsBiometricAuthenticationFailed",
            "SettingsBiometricConfiguredSecureStorage",
            "SettingsBiometricEnabledStatus",
            "SettingsBiometricEnableFailure",
            "SettingsDisableBiometricMasterRequired",
            "SettingsBiometricDisabledStatus",
            "SettingsBiometricDisableFailure",
            "SettingsBackupPassphraseRangeFormat",
            "SettingsBackupConfirmTitle",
            "SettingsBackupConfirmBody",
            "SettingsBackupConfirmAccept",
            "SettingsBackupCreatedStatus",
            "SettingsBackupShareTitle",
            "SettingsBackupFailure",
            "SettingsRestorePassphraseRangeFormat",
            "SettingsRestorePickerTitle",
            "SettingsRestoreConfirmTitle",
            "SettingsRestoreConfirmBody",
            "SettingsRestoreConfirmAccept",
            "SettingsRestoreCleanupWarning",
            "SettingsRestoreSuccess",
            "SettingsRestorePostCleanupFailure",
            "SettingsRestoreFailure",
            "SettingsChangeMasterCurrentRequired",
            "SettingsChangeMasterMismatch",
            "SettingsChangeMasterTooLongFormat",
            "SettingsChangeMasterWeakFormat",
            "SettingsChangeMasterSuccess",
            "SettingsChangeMasterAuthFailure",
            "SettingsChangeMasterFailure",
            "SettingsDeleteExactPhraseFormat",
            "SettingsDeleteMasterRequired",
            "SettingsDeleteConfirmTitle",
            "SettingsDeleteConfirmBody",
            "SettingsDeleteConfirmAccept",
            "SettingsDeleteAuthFailure",
            "SettingsDeleteFailure",
            "SettingsDeleteConfirmFailure"
        })
        {
            Assert.Contains(key, source, StringComparison.Ordinal);
        }

        Assert.Contains("AuthenticateAsync(SettingsText(\"SettingsEnableBiometricPrompt\"))", source, StringComparison.Ordinal);
        Assert.Contains("SettingsText(\"CancelButton\")", source, StringComparison.Ordinal);
        Assert.Contains("SettingsFormat(\"SettingsDeleteExactPhraseFormat\", DeletePhrase)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Biometric unlock could not be enabled safely.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Backup restored. Unlock the restored vault", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Master passphrase was not changed because", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Vault deletion or local secure-storage cleanup could not finish safely", source, StringComparison.Ordinal);
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
