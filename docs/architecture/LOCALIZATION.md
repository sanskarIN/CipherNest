# Localization Architecture

CipherNest ships a neutral English catalog and now includes a reviewed Hindi (`hi-IN`) satellite catalog for the resource-backed interface that has been migrated to localization resources. The project still does **not** claim that every user-facing UI literal has been translated.

## Current structure

- `AppLanguagePreference` persists `System`, `English`, or `Hindi`.
- `Resources/Localization/AppStrings.resx` is the neutral English catalog.
- `Resources/Localization/AppStrings.hi-IN.resx` is the reviewed Hindi catalog for the same resource keys.
- `LocalizationService` owns UI-culture selection and `ResourceManager` lookup.
- Explicit English maps to `en-US`; explicit Hindi maps to `hi-IN`; System preserves the process-start system UI culture.
- The saved preference is applied at startup/resume and when the user changes it in Settings.
- Missing culture-specific translations fall back to the neutral English resources through normal `ResourceManager` fallback.
- `LocalizationSourceTests` requires key parity, non-empty Hindi values, security-critical translation coverage, runtime preference wiring, and documentation that does not overstate translation completeness.

## Reviewed Hindi scope

The current Hindi catalog covers the resource-backed product/title/navigation controls already represented by `AppStrings` plus the security-sensitive local-only, audit-status, recovery-limitation, and language-preference status messages.

The wording intentionally preserves these security meanings:

- the vault remains local to the device in ordinary operation;
- CipherNest has not completed an independent professional security audit;
- a forgotten master passphrase is not remotely recoverable and recovery depends on retained configured recovery material;
- untranslated or not-yet-migrated interface text may still appear in English.

Hindi is therefore a supported **resource-backed language preference**, not a claim that the complete application UI is fully translated today.

## Adding or expanding a language

1. Add or update a culture-specific resource catalog such as `AppStrings.hi-IN.resx` using exactly the same keys as the neutral catalog.
2. Extend `AppLanguagePreference` only for a language whose reviewed catalog is actually shipped.
3. Map that preference to an explicit `CultureInfo` in `LocalizationService`.
4. Move remaining literal UI copy to resource-backed bindings/services screen by screen; do not mark a screen translated until every user-facing/security-sensitive literal on it has been reviewed.
5. Keep resource keys language-neutral and stable. Do not encode a language into persistence, vault records, cryptographic associated data, or backup formats.
6. Test long strings, pluralization, screen-reader pronunciation, keyboard navigation, layout at large text sizes, and right-to-left behavior for languages where it applies.
7. Keep security warnings semantically equivalent; translations must not weaken recovery, export, audit, deletion, biometric, clipboard, or platform-limit wording.
8. Keep the neutral English value as the fallback so a missing satellite entry cannot produce a blank security warning.

## Release validation

For each language-enabled release candidate:

- verify neutral/satellite key parity;
- verify no blank translated values;
- exercise language selection, app restart, suspend/resume, and fallback behavior on target platforms;
- review every translated security warning against the canonical English security documentation;
- test responsive layout and accessibility services with the translated strings;
- keep any not-yet-migrated screens documented as potentially English.

Localization remains presentation-only and must not change vault data, crypto formats, database schema, backup compatibility, recovery behavior, authorization semantics, or security boundaries.
