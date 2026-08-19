# Localization Architecture

CipherNest ships a neutral English catalog and includes a reviewed Hindi (`hi-IN`) satellite catalog for the resource-backed interface that has been migrated to localization resources. The project still does **not** claim that every user-facing UI literal has been translated.

## Current structure

- `AppLanguagePreference` persists `System`, `English`, or `Hindi`.
- `Resources/Localization/AppStrings.resx` is the neutral English catalog.
- `Resources/Localization/AppStrings.hi-IN.resx` is the reviewed Hindi catalog for the same resource keys.
- `LocalizationService` owns UI-culture selection and `ResourceManager` lookup.
- `Localization/TranslateExtension.cs` provides a reusable XAML markup extension so fixed page text and semantic/accessibility descriptions can resolve the active reviewed catalog without duplicating `ResourceManager` access in views.
- `TranslateExtension` is marked `AcceptEmptyServiceProvider` because it does not consume XAML's supplied service-provider context; its lookup deliberately uses the registered application localization service instead.
- Dynamic TOTP period/validity text and TOTP operation status/error messages resolve the same reviewed catalog from `ItemEditorViewModel.Totp.cs` and format values with `CultureInfo.CurrentUICulture`.
- Explicit English maps to `en-US`; explicit Hindi maps to `hi-IN`; System preserves the process-start system UI culture.
- The saved preference is applied at startup/resume and when the user changes it in Settings.
- Missing culture-specific translations fall back to the neutral English resources through normal `ResourceManager` fallback.
- Markup-extension values are resolved when the XAML element is constructed. A page that was already constructed before a language change can retain its existing fixed text until that page is reconstructed; do not claim live in-place translation for every existing visual tree.
- `LocalizationSourceTests` requires key parity, non-empty Hindi values, security-critical translation coverage, runtime preference wiring, and documentation that does not overstate translation completeness.
- Dedicated TOTP localization source tests guard catalog entries, translation-extension wiring, MAUI service-provider annotation, localized item-editor usage, localized dynamic formatting, localized TOTP operation statuses/errors, and removal of the prior hard-coded setup-URI security text.

## Reviewed Hindi scope

The current Hindi catalog covers the resource-backed product/title/navigation controls already represented by `AppStrings` plus the security-sensitive local-only, audit-status, recovery-limitation, language-preference status messages, and the migrated TOTP item-editor security/workflow surface.

The TOTP resource-backed surface includes:

- the TOTP heading and local-only seed/code explanation;
- setup-URI import heading, bounded URI explanation, placeholder, and import action;
- setup-URI copy action;
- algorithm/digit labels and refresh/copy-code actions;
- semantic descriptions for setup-URI import/copy and generated-code copy;
- period and refreshed-code validity formatting;
- generation/import/copy success and failure statuses;
- the authorization/clipboard-history/synchronization warning.

The wording intentionally preserves these security meanings:

- the vault remains local to the device in ordinary operation;
- CipherNest has not completed an independent professional security audit;
- a forgotten master passphrase is not remotely recoverable and recovery depends on retained configured recovery material;
- TOTP setup URIs contain the seed and must be protected like the seed itself;
- TOTP URI parsing/import remains local and bounded, while HOTP remains intentionally unsupported at this boundary;
- clipboard history/synchronization can still expose copied secrets despite CipherNest's best-effort timed cleanup;
- failed TOTP operations must not be described as successful and must preserve the documented unchanged-state guarantees;
- untranslated or not-yet-migrated interface text elsewhere in CipherNest may still appear in English.

Hindi is therefore a supported **resource-backed language preference**, not a claim that the complete application UI is fully translated today.

## Adding or expanding a language

1. Add or update a culture-specific resource catalog such as `AppStrings.hi-IN.resx` using exactly the same keys as the neutral catalog.
2. Extend `AppLanguagePreference` only for a language whose reviewed catalog is actually shipped.
3. Map that preference to an explicit `CultureInfo` in `LocalizationService`.
4. For fixed XAML text, prefer `TranslateExtension` instead of direct hard-coded security copy.
5. Keep custom MAUI markup extensions explicit about XAML service-provider requirements (`RequireService` when consuming services supplied by XAML, or `AcceptEmptyServiceProvider` when the extension intentionally does not require that context).
6. For dynamic formatted values or ViewModel operation messages, resolve reviewed resource keys and format with the active UI culture rather than embedding English-only `StringFormat` or status literals.
7. Move remaining literal UI copy to resource-backed bindings/services screen by screen; do not mark a screen translated until every user-facing/security-sensitive literal on it has been reviewed.
8. Keep resource keys language-neutral and stable. Do not encode a language into persistence, vault records, cryptographic associated data, or backup formats.
9. Test long strings, formatting placeholders, pluralization, screen-reader pronunciation, keyboard navigation, layout at large text sizes, and right-to-left behavior for languages where it applies.
10. Keep security warnings semantically equivalent; translations must not weaken recovery, export, audit, deletion, biometric, clipboard, TOTP, or platform-limit wording.
11. Keep the neutral English value as the fallback so a missing satellite entry cannot produce a blank security warning.

## Release validation

For each language-enabled release candidate:

- verify neutral/satellite key parity;
- verify no blank translated values;
- verify required format placeholders such as `{0}` remain present in every translated dynamic format;
- exercise language selection, page reconstruction/navigation, app restart, suspend/resume, and fallback behavior on target platforms;
- review every translated security warning and TOTP operation status against the canonical English security documentation;
- test localized TOTP generation/setup-URI import/copy controls without placing real seeds, URIs, or codes in screenshots/logs/test artifacts;
- test responsive layout and accessibility services with the translated strings;
- keep any not-yet-migrated screens documented as potentially English.

Localization remains presentation-only and must not change vault data, crypto formats, database schema, backup compatibility, recovery behavior, authorization semantics, or security boundaries.
