# Settings Surface Localization Verification — 2026-08-19

## Scope

This record freezes the repository-completable localization scope for the remaining fixed-text Settings surface added on August 19, 2026.

The migration covers the fixed presentation text for:

- appearance and accessibility;
- theme and language labels plus language-scope guidance;
- reduced-motion and larger-interface preferences;
- lock and privacy timing controls;
- background locking, clipboard cleanup, screenshot protection and trash retention;
- local backup/review reminders and their local-only privacy statement;
- generator defaults;
- storage/cache fixed guidance and actions;
- optional Buy Me a Coffee support card and accessibility descriptions;
- About/legal navigation.

Previously migrated Settings security surfaces remain covered by their existing source tests and include biometrics, local security review, encrypted backup/restore, transfer, master-passphrase change, and vault deletion.

## Resource keys

The following keys are required in both the neutral and `hi-IN` catalogs:

```text
SettingsAppearanceAccessibilityTitle
SettingsThemeLabel
SettingsLanguageLabel
SettingsLanguageSummary
SettingsSaveLanguageButton
SettingsReducedMotionLabel
SettingsLargerInterfaceLabel
SettingsLockPrivacyTitle
SettingsLockTimeoutLabel
SettingsLockOnBackgroundLabel
SettingsClipboardClearLabel
SettingsScreenshotProtectionLabel
SettingsTrashRetentionLabel
SettingsLocalRemindersTitle
SettingsBackupReminderLabel
SettingsReviewRemindersLabel
SettingsReviewReminderLeadLabel
SettingsReviewReminderSummary
SettingsSaveSettingsButton
SettingsGeneratorDefaultsTitle
SettingsGeneratorDefaultsSummary
SettingsConfigureGeneratorButton
SettingsStorageCacheTitle
SettingsStorageCacheSummary
SettingsRefreshStorageButton
SettingsClearCacheButton
SettingsFundingTitle
SettingsFundingBadgeSemanticDescription
SettingsFundingSummary
SettingsFundingButton
SettingsFundingButtonSemanticDescription
SettingsAboutLegalTitle
SettingsAboutLegalSummary
SettingsOpenAboutLegalButton
```

## Security and privacy semantics that translations must preserve

1. Language support is resource-backed and still incomplete outside migrated surfaces; Hindi preference must not be described as complete application-wide translation.
2. Review reminders are calculated locally after unlock from encrypted item metadata; the text must not claim that vault details are sent to an external notification service.
3. Cache cleanup must not imply deletion of the encrypted vault database, encrypted attachment store, or backups intentionally kept under app data.
4. Buy Me a Coffee support remains optional and must never imply different feature access, security, privacy, licensing, recovery, or support priority.
5. Fixed translated text remains presentation-only and must not modify persisted preferences, vault records, backup formats, cryptographic associated data, authorization rules, or destructive-operation behavior.

## Automated source guards

`tests/CipherNest.UiTests/SettingsSurfaceLocalizationSourceTests.cs` verifies that:

- every key listed above is referenced by `SettingsPage.xaml` through `l10n:Translate`;
- the neutral and Hindi catalogs both contain non-empty values for every key;
- the reviewed Hindi values are distinct from the neutral values;
- selected previous hard-coded English literals and BMC accessibility descriptions are absent from the XAML;
- the neutral reminder wording preserves the local-only boundary;
- the neutral cache wording preserves the encrypted-vault non-deletion boundary;
- the neutral funding wording preserves optional/equal-treatment semantics.

The pre-existing catalog-parity test continues to require exact neutral/Hindi key parity across the full catalogs.

## Manual validation still required

Repository source tests do not replace physical target validation. Before release, verify on representative targets:

- English, Hindi, and System language selection;
- page reconstruction after language changes;
- phone and resizable desktop layout with longer Hindi strings;
- large-text/larger-interface behavior;
- TalkBack, VoiceOver, Narrator and keyboard/focus behavior where applicable;
- semantic descriptions for the funding badge/action;
- funding-disabled build behavior (`CipherNestEnableFundingLink=false`);
- reminder and privacy wording on actual screen sizes;
- storage/cache actions against disposable test data.

## Exact-head automation required before release claims

The source changes in this continuation do not by themselves prove a release candidate. The final exact head still requires the repository's configured build/test/format, platform build, CodeQL, and dependency/security gates before release-candidate wording is updated.

## Explicit non-claims

This migration does **not** establish:

- complete Hindi translation of CipherNest;
- live in-place retranslation of every already-constructed visual tree;
- physical assistive-technology validation;
- store acceptance, signing, provisioning or notarization;
- an independent professional security audit;
- absence of unknown defects.
