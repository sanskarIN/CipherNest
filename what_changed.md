# What Changed

Detailed historical ledgers are preserved at:

- [`docs/history/what_changed_through_2026_08_15.md`](docs/history/what_changed_through_2026_08_15.md) — implementation history through August 15, 2026.
- [`docs/history/what_changed_through_2026_08_18.md`](docs/history/what_changed_through_2026_08_18.md) — complete live ledger covering the August 16 documentation expansion and August 18 bounded TOTP setup-URI continuation.

This live ledger continues from **August 19, 2026**. Git history remains the authoritative commit-by-commit record.

---

## 2026-08-19 — TOTP security-surface localization continuation

### Goal

Continue repository-completable work from the current release roadmap without pretending that physical-device, store, signing, accessibility certification, interoperability, or independent professional security-review gates can be completed through source edits alone.

The concrete repository gap selected for this continuation was explicitly identified in `docs/NEXT_STEPS.md`: continue migrating remaining literals to reviewed localization resources, including the new TOTP setup-URI UI strings.

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

with matching keys for the fixed TOTP security surface:

- TOTP heading;
- local-only seed/code explanation;
- setup-URI import heading and bounded/local processing explanation;
- setup-URI placeholder;
- setup-URI import action;
- setup-URI copy action;
- algorithm and digit labels;
- refresh/copy-code actions;
- semantic descriptions for URI import/copy and generated-code copy;
- authorization plus clipboard-history/synchronization warning.

The Hindi wording preserves the same security boundaries as the canonical English text. The literal URI skeleton `otpauth://totp/...` intentionally remains identical in both catalogs.

### Item editor migration

Updated:

`src/CipherNest.App/Views/ItemEditorPage.xaml`

The fixed TOTP setup-URI panel now uses the reusable `l10n:Translate` markup extension rather than hard-coded English for the newly added setup-URI/security controls and accessibility descriptions.

The migration includes the security-sensitive warning that:

- setup URIs contain the TOTP seed;
- only authorized seeds should be imported;
- copying a seed, setup URI, or generated code can expose it through operating-system clipboard history/synchronization;
- CipherNest's timed clipboard cleanup remains best effort rather than a guarantee of OS-history deletion.

### Regression protection

Added three focused UI/source test files:

- `tests/CipherNest.UiTests/TranslationExtensionSourceTests.cs`
- `tests/CipherNest.UiTests/TotpLocalizationCatalogSourceTests.cs`
- `tests/CipherNest.UiTests/TotpLocalizationUiSourceTests.cs`

They guard:

- translation-extension service wiring and missing-key behavior;
- neutral/Hindi TOTP key presence and non-empty values;
- reviewed Hindi translations for the TOTP security/action strings;
- exact preservation of the `otpauth://totp/...` placeholder;
- item-editor use of every new TOTP resource key;
- removal of the previous hard-coded English setup-URI action/warning strings.

The pre-existing `LocalizationSourceTests.HindiCatalog_MatchesNeutralCatalogKeys` also continues to enforce complete neutral/Hindi key parity across the catalogs.

### Localization architecture documentation

Updated:

`docs/architecture/LOCALIZATION.md`

It now documents:

- the reusable XAML translation extension;
- construction-time resolution semantics;
- the reviewed fixed TOTP Hindi scope;
- TOTP-specific security-meaning requirements;
- guidance to prefer resource-backed fixed text instead of new hard-coded security copy;
- the requirement to test translated TOTP controls with synthetic data only;
- the continued prohibition on claiming the entire application is fully translated.

A page already constructed before a language preference change can retain its existing markup-extension text until the page is reconstructed. The documentation therefore does **not** claim universal live in-place translation of every existing visual tree.

### Known localization work still remaining

This continuation deliberately does not overstate scope. Other application screens still contain unmigrated English literals, and the current TOTP panel still has dynamic English `StringFormat` text for period/remaining-time display. Those remain future resource-migration work rather than hidden completed translation.

### Historical ledger preservation

The prior live `what_changed.md` was preserved byte-for-byte as:

`docs/history/what_changed_through_2026_08_18.md`

This keeps all detailed August 16–18 implementation, documentation, security-hardening, verification, commit, and limitation notes available without forcing every future continuation to rewrite a very large live ledger.

### Commits in this continuation so far

- `4f7895f0fec9e455b4a7263d058d55abc94539f4` — `feat(localization): add reusable XAML translation extension`
- `a0961b38436235c8a2d931bcdad2ea28d3cc1c43` — `feat(localization): add neutral TOTP security strings`
- `007b333a646d745b57c703f72a9ab70e1d183b1e` — `feat(localization): add reviewed Hindi TOTP strings`
- `115b0b6b7139af6fe2c3f987803ec169c871420f` — `feat(ui): localize TOTP security surface`
- `4c3ad44a389265e8b5a02940ca0261f8e846f323` — `test(localization): guard XAML translation extension wiring`
- `99285fdfcdd025f9d1b971807edc605aa712b40f` — `test(localization): verify TOTP neutral and Hindi catalogs`
- `3fe3d5da2e9ee117b75f571829f0584796554f30` — `test(ui): guard localized TOTP security surface`
- `f927f27fa5dc7e0ff21512f84acb9c575b592669` — `docs(localization): document TOTP resource-backed UI path`
- `1cb1d575afefb810d190c177be59efe5039ffd57` — `docs(history): preserve August 18 live change ledger`

All commits observed on `main` use Git author/committer identity `Sanskar <sanskarin@outlook.in>`.

### Verification status

No new exact-head build/test success claim is made merely because source/tests were added.

The August 19 head must still complete the configured automated gates after the candidate is frozen, including:

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
