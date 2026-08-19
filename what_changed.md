# What Changed

Detailed historical ledgers are preserved at:

- [`docs/history/what_changed_through_2026_08_15.md`](docs/history/what_changed_through_2026_08_15.md) — implementation history through August 15, 2026.
- [`docs/history/what_changed_through_2026_08_18.md`](docs/history/what_changed_through_2026_08_18.md) — complete live ledger covering the August 16 documentation expansion and August 18 bounded TOTP setup-URI continuation.

This live ledger continues from **August 19, 2026**. Git history remains the authoritative commit-by-commit record.

---

## 2026-08-19 — TOTP workflow localization continuation

### Goal

Continue repository-completable work from the current release roadmap without pretending that physical-device, store, signing, accessibility certification, interoperability, or independent professional security-review gates can be completed through source edits alone.

The concrete repository gap selected for this continuation was the remaining localization migration around the new TOTP setup-URI workflow.

### Starting head

This continuation started from:

`2c7894f29f6d0d752342a0f864b0a3dbf3fa0f67`

The immutable historical implementation baseline with complete previously recorded CI evidence remains:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

with 555 passing tests and the historical Windows/Android/Apple/CodeQL evidence recorded elsewhere in this repository. That historical evidence is **not** automatically inherited by the August 19 source head.

### Reusable XAML localization path

Added:

`src/CipherNest.App/Localization/TranslateExtension.cs`

The extension:

- implements MAUI `IMarkupExtension`;
- requires a non-empty resource key;
- fails closed with `XamlParseException` when the key itself is absent from markup;
- resolves the already-registered `ILocalizationService` through application services;
- returns the active reviewed resource value through the existing `LocalizationService`/`ResourceManager` path;
- avoids introducing a second localization store or duplicating culture-selection logic inside views.

This is intentionally a presentation-layer facility. It does not alter persistence, cryptographic formats, backup compatibility, vault records, authentication, or authorization.

### Neutral and Hindi TOTP catalogs

Expanded both:

- `src/CipherNest.App/Resources/Localization/AppStrings.resx`
- `src/CipherNest.App/Resources/Localization/AppStrings.hi-IN.resx`

The reviewed resource-backed TOTP workflow now covers:

- TOTP heading;
- local-only seed/code explanation;
- setup-URI import heading and bounded/local processing explanation;
- setup-URI placeholder;
- setup-URI import action;
- setup-URI copy action;
- algorithm and digit labels;
- refresh/copy-code actions;
- semantic descriptions for URI import/copy and generated-code copy;
- authorization plus clipboard-history/synchronization warning;
- dynamic period text;
- dynamic refreshed-code validity text;
- invalid seed/settings status;
- code-generation failure status;
- code-copy failure status;
- missing setup-URI status;
- setup-URI import success/invalid/failure statuses;
- setup-URI copy success/invalid/failure statuses.

The Hindi wording preserves the same security boundaries as the canonical English text. The protocol skeleton `otpauth://totp/...` intentionally remains identical in both catalogs, while dynamic format resources preserve their required `{0}` placeholder.

### Item editor fixed-text migration

Updated:

`src/CipherNest.App/Views/ItemEditorPage.xaml`

The fixed TOTP setup-URI panel now uses the reusable `l10n:Translate` markup extension rather than hard-coded English for the migrated setup-URI/security controls and accessibility descriptions.

The migration includes the security-sensitive warning that:

- setup URIs contain the TOTP seed;
- only authorized seeds should be imported;
- copying a seed, setup URI, or generated code can expose it through operating-system clipboard history/synchronization;
- CipherNest's timed clipboard cleanup remains best effort rather than a guarantee of OS-history deletion.

### Dynamic and operation-status migration

Updated:

`src/CipherNest.App/ViewModels/ItemEditorViewModel.Totp.cs`

The ViewModel now:

- exposes `TotpPeriodText` and `TotpValidityText` from reviewed localization resources;
- formats dynamic values with `CultureInfo.CurrentUICulture`;
- raises property-change notifications when period/remaining-time values change;
- resolves TOTP generation/import/copy success and failure messages by resource key instead of embedding English strings.

The Item Editor now binds the period and validity labels to those resource-backed dynamic properties instead of English-only XAML `StringFormat` literals.

This means the migrated TOTP workflow no longer has the previously recorded dynamic-English period/validity caveat. Other unrelated Item Editor/application literals remain outside this continuation and can still appear in English.

### Regression protection

Added or expanded focused UI/source tests:

- `tests/CipherNest.UiTests/TranslationExtensionSourceTests.cs`
- `tests/CipherNest.UiTests/TotpLocalizationCatalogSourceTests.cs`
- `tests/CipherNest.UiTests/TotpLocalizationUiSourceTests.cs`
- `tests/CipherNest.UiTests/TotpLocalizedStatusSourceTests.cs`

They guard:

- translation-extension service wiring and missing-key behavior;
- neutral/Hindi TOTP key presence and non-empty values;
- reviewed Hindi translation of fixed, dynamic, and operation-status strings;
- exact preservation of the `otpauth://totp/...` placeholder;
- preservation of `{0}` in dynamic resource formats;
- item-editor use of every migrated fixed TOTP resource key;
- resource-backed period/validity bindings;
- current-culture formatting and required property-change notifications;
- use of resource keys for generation/import/copy result messages;
- removal of previous hard-coded TOTP setup-URI, warning, dynamic-format, and selected operation-status strings.

The pre-existing `LocalizationSourceTests.HindiCatalog_MatchesNeutralCatalogKeys` also continues to enforce complete neutral/Hindi key parity across the catalogs.

### Documentation and verification

Updated:

- `docs/architecture/LOCALIZATION.md`
- `docs/verification/TOTP_LOCALIZATION_2026_08_19.md`

The architecture reference now documents the reusable fixed-text path plus dynamic/status localization, current-culture formatting, security-meaning requirements, and the continued prohibition on claiming that the entire application is fully translated.

The dedicated August 19 verification contract records:

- the complete migrated TOTP resource-key surface;
- source-test guards;
- security semantics translations must preserve;
- manual target/accessibility checks still required;
- exact-head CI gates;
- accurate release wording and non-claims.

A page already constructed before a language preference change can retain markup-extension fixed text until the page is reconstructed. The documentation therefore does **not** claim universal live in-place translation of every existing visual tree.

### Historical ledger preservation

The prior live `what_changed.md` was preserved byte-for-byte as:

`docs/history/what_changed_through_2026_08_18.md`

This keeps all detailed August 16–18 implementation, documentation, security-hardening, verification, commit, and limitation notes available without forcing every future continuation to rewrite a very large live ledger.

### Commits in this continuation

- `4f7895f0fec9e455b4a7263d058d55abc94539f4` — `feat(localization): add reusable XAML translation extension`
- `a0961b38436235c8a2d931bcdad2ea28d3cc1c43` — `feat(localization): add neutral TOTP security strings`
- `007b333a646d745b57c703f72a9ab70e1d183b1e` — `feat(localization): add reviewed Hindi TOTP strings`
- `115b0b6b7139af6fe2c3f987803ec169c871420f` — `feat(ui): localize TOTP security surface`
- `4c3ad44a389265e8b5a02940ca0261f8e846f323` — `test(localization): guard XAML translation extension wiring`
- `99285fdfcdd025f9d1b971807edc605aa712b40f` — `test(localization): verify TOTP neutral and Hindi catalogs`
- `3fe3d5da2e9ee117b75f571829f0584796554f30` — `test(ui): guard localized TOTP security surface`
- `f927f27fa5dc7e0ff21512f84acb9c575b592669` — `docs(localization): document TOTP resource-backed UI path`
- `1cb1d575afefb810d190c177be59efe5039ffd57` — `docs(history): preserve August 18 live change ledger`
- `803e99cea62d5e74cb857a3b13408027b9f5e6db` — `docs(ledger): record August 19 localization continuation`
- `e3b21972d44935cd90c61f541f3484e627a886e7` — `docs(verification): add TOTP localization verification contract`
- `c18166541b240d77d9d59524ba605a6e66f5cd98` — `feat(localization): add TOTP dynamic and status resources`
- `81528d16bd896748627359b5595663db1eea2d40` — `feat(localization): add Hindi TOTP dynamic and status resources`
- `e1e6b33947d21f1107bc5ea98640d996c8afd39f` — `feat(totp): localize dynamic status and error text`
- `1a85635001c6b30ed6e1a71504b4895eb390dc91` — `feat(ui): localize TOTP dynamic period and validity text`
- `c2310fa23ece06ffe9aaffcc927f9136aed8b6d8` — `test(localization): cover TOTP dynamic and status resources`
- `244f1788d2ab0594b17fe53807b3a6482a42a72c` — `test(totp): guard localized dynamic and operation text`
- `373127cf34b15aeaccf26912f61c03b493b4a0ab` — `docs(localization): complete TOTP workflow localization scope`
- `41b46be267a4dfd1a62057a703f2c6b4fa48826c` — `docs(verification): expand TOTP localization verification scope`

All commits observed on `main` use Git author/committer identity `Sanskar <sanskarin@outlook.in>`.

### Verification status

No new exact-head build/test success claim is made merely because source/tests were added.

The available GitHub workflow-run helper did not expose push-triggered run evidence for this continuation, so no CI result is fabricated. The frozen August 19 candidate still requires observable configured automation, including:

- core restore/build/test/format;
- Windows default Release;
- Windows funding-disabled Release;
- Android Release;
- iOS simulator Release;
- Mac Catalyst Release;
- CodeQL application analysis;
- dependency/security review where applicable.

Physical-device lifecycle, biometrics, secure-storage, clipboard-history/synchronization, screenshot/task-preview, accessibility, translated-layout, store-policy, signing/notarization, third-party TOTP interoperability, and independent professional security review remain external evidence gates.

### Security/release claims intentionally unchanged

This work does **not** claim:

- full Hindi translation of the application;
- universal authenticator/provider compatibility;
- QR/camera/HOTP/provider enrollment support;
- guaranteed clipboard-history or synchronization deletion;
- physical-device validation;
- store acceptance/signing completion;
- absence of unknown defects;
- completion of an independent professional security audit.
