# TOTP Workflow Localization Verification — 2026-08-19

## Scope

This record covers the August 19, 2026 repository-side migration of CipherNest's TOTP setup-URI/security workflow from hard-coded English literals to the existing reviewed neutral-English/Hindi (`hi-IN`) localization system.

The migrated TOTP scope includes fixed XAML copy, semantic/accessibility descriptions, dynamic period/validity formatting, and TOTP generation/import/copy operation success and failure messages.

It is a **source and review contract**, not a substitute for exact-head CI, device testing, accessibility testing, or independent professional security review.

## Starting point

The continuation started from `main` commit:

`2c7894f29f6d0d752342a0f864b0a3dbf3fa0f67`

The older exact implementation baseline with previously recorded all-platform CI and CodeQL evidence remains:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

Historical evidence must remain attached to that historical SHA.

## Implementation under review

### XAML translation extension

`src/CipherNest.App/Localization/TranslateExtension.cs`

Required properties:

- implements MAUI `IMarkupExtension`;
- exposes a required localization resource key;
- rejects blank markup keys;
- resolves the registered `ILocalizationService`;
- delegates value lookup to the canonical localization service;
- does not introduce persistence, network, telemetry, or cryptographic behavior.

### Resource catalogs

The following catalogs must retain matching keys:

- `src/CipherNest.App/Resources/Localization/AppStrings.resx`
- `src/CipherNest.App/Resources/Localization/AppStrings.hi-IN.resx`

The TOTP workflow is represented by these keys:

- `TotpHeading`
- `TotpSeedSummary`
- `TotpImportHeading`
- `TotpImportSummary`
- `TotpImportPlaceholder`
- `TotpImportButton`
- `TotpCopySetupUriButton`
- `TotpAlgorithmLabel`
- `TotpDigitsLabel`
- `TotpRefreshCodeButton`
- `TotpCopyCodeButton`
- `TotpImportSemanticDescription`
- `TotpCopySetupUriSemanticDescription`
- `TotpCurrentCodeSemanticDescription`
- `TotpCopyCodeSemanticDescription`
- `TotpSafetyWarning`
- `TotpPeriodFormat`
- `TotpValidityFormat`
- `TotpInvalidSeedSettingsError`
- `TotpGenerateError`
- `TotpCopyCodeError`
- `TotpImportMissingUriError`
- `TotpImportSuccess`
- `TotpImportInvalidError`
- `TotpImportFailureError`
- `TotpCopyUriSuccess`
- `TotpCopyUriInvalidError`
- `TotpCopyUriFailureError`

The URI placeholder remains the protocol skeleton `otpauth://totp/...` in both catalogs; that protocol text is not translated. Dynamic format resources must preserve their `{0}` replacement placeholder.

## Security meaning that translations must preserve

The reviewed wording must continue to communicate that:

1. the Secret field for a TOTP item is the Base32 seed;
2. the seed is stored inside the encrypted vault record;
3. generated codes are computed locally while the vault is unlocked and are not persisted;
4. setup-URI processing is local and bounded;
5. a setup URI contains the seed and is sensitive;
6. HOTP is intentionally rejected by the current setup-URI boundary;
7. only authorized TOTP seeds should be imported;
8. copied seeds/setup URIs/codes may be exposed through operating-system clipboard history or synchronization;
9. CipherNest's timed clipboard cleanup is best effort and is not a promise to erase OS-managed clipboard history;
10. error and success text must accurately preserve the operation result and unchanged-state guarantees.

A translation that weakens any of those meanings is a security/documentation defect even if it is linguistically fluent.

## UI and ViewModel integration

`src/CipherNest.App/Views/ItemEditorPage.xaml` uses `l10n:Translate` for fixed TOTP setup-URI/security text and binds dynamic labels to `TotpPeriodText` and `TotpValidityText`.

`src/CipherNest.App/ViewModels/ItemEditorViewModel.Totp.cs`:

- resolves dynamic format strings from the same reviewed localization catalog;
- formats period and remaining-time values with `CultureInfo.CurrentUICulture`;
- notifies the dynamic properties when their source values change;
- resolves TOTP generation/import/copy status and error messages by resource key rather than embedded English literals.

Security-sensitive setup-URI actions, warnings, semantic descriptions, dynamic status text, and operation result messages must not silently regress to duplicated hard-coded English literals.

This migration still intentionally does **not** claim that the whole Item Editor or application is fully localized. Unmigrated UI outside the TOTP workflow can still appear in English.

## Automated source guards added

### `TranslationExtensionSourceTests.cs`

Guards:

- markup-extension contract;
- service resolution;
- localization lookup;
- fail-closed blank-key behavior.

### `TotpLocalizationCatalogSourceTests.cs`

Guards:

- presence of every TOTP localization key in both catalogs;
- nonblank resource values;
- Hindi translation of security/action/dynamic/status strings;
- exact protocol placeholder preservation;
- preservation of required dynamic-format placeholders.

### `TotpLocalizationUiSourceTests.cs`

Guards:

- item-editor localization namespace wiring;
- use of all fixed TOTP resource keys;
- absence of selected previous hard-coded English TOTP setup-URI strings/warnings.

### `TotpLocalizedStatusSourceTests.cs`

Guards:

- resource-backed dynamic period/validity properties;
- current-culture formatting;
- property-change notifications for dynamic labels;
- resource-backed TOTP operation messages;
- removal of prior hard-coded dynamic `StringFormat` values and selected operation messages.

The pre-existing neutral/Hindi catalog-parity source test remains an additional global guard.

## Manual target validation still required

Using synthetic TOTP data only, validate on supported targets:

- English, Hindi, and System language selection;
- navigation/page reconstruction after changing language;
- application restart and suspend/resume behavior;
- no blank security warning/status when a satellite resource is unavailable;
- period and remaining-time values render correctly in both reviewed languages;
- generation/import/copy success and failure statuses render correctly in both reviewed languages;
- long Hindi strings at normal and large text sizes;
- TalkBack, VoiceOver, Narrator, and keyboard focus where applicable;
- semantic descriptions do not reveal real setup URIs, seeds, or generated codes;
- setup-URI import/copy behavior remains unchanged by localization;
- clipboard cleanup policy remains unchanged by localization;
- narrow phone, landscape, tablet, and resizable desktop layout behavior.

Do not place real user TOTP seeds, setup URIs, or generated codes in screenshots, logs, bug reports, or verification artifacts.

## Exact-head CI gates

Before calling the resulting release candidate exact-head verified, observe successful configured gates for the frozen SHA:

- core restore/build/test/format;
- Windows default Release;
- Windows funding-disabled Release;
- Android Release;
- iOS simulator Release;
- Mac Catalyst Release;
- CodeQL;
- applicable dependency/security review.

No success result is inferred from source inspection alone.

## Release wording

Accurate current wording may say that CipherNest includes a reviewed resource-backed Hindi catalog for the migrated interface and that the TOTP setup-URI/security workflow—including its migrated dynamic and operation-status text—is included in that resource-backed scope.

Do **not** claim:

- complete Hindi translation of every screen;
- instant live translation of every already-constructed page;
- QR/camera/HOTP/provider enrollment support;
- universal third-party authenticator compatibility;
- guaranteed clipboard-history deletion;
- physical-device validation merely from source tests;
- an independent professional security audit.
