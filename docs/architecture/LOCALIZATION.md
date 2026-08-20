# Localization Architecture

CipherNest ships neutral English resources and reviewed Hindi (`hi-IN`) satellite resources for interface surfaces that have been migrated to localization. The project still does **not** claim that every user-facing UI literal has been translated.

## Current structure

- `AppLanguagePreference` persists `System`, `English`, or `Hindi`.
- `Resources/Localization/AppStrings.resx` is the primary neutral English catalog for shared and previously migrated strings.
- `Resources/Localization/AppStrings.hi-IN.resx` is the reviewed Hindi satellite catalog for the same primary-catalog keys.
- Feature catalogs can keep a complete screen/workflow migration cohesive without turning `AppStrings` into an unbounded single file. The first feature catalog pair is `TrashStrings.resx` / `TrashStrings.hi-IN.resx`.
- `LocalizationService` owns UI-culture selection and ordered resource lookup: the primary `AppStrings` catalog is checked first, then registered feature catalogs. If no catalog contains a key, the service returns the key name so missing resources remain visible during development instead of becoming blank text.
- Each culture-specific feature catalog must retain exact key parity with its neutral feature catalog; focused source tests enforce this for Trash.
- `Localization/TranslateExtension.cs` provides a reusable XAML markup extension so fixed page text and semantic/accessibility descriptions can resolve the active reviewed catalog without duplicating `ResourceManager` access in views.
- `TranslateExtension` is marked `AcceptEmptyServiceProvider` because it does not consume XAML's supplied service-provider context; its lookup deliberately uses the registered application localization service instead.
- Dynamic TOTP period/validity text, TOTP operation status/error messages, Unlock workflow statuses, onboarding strength labels, vault-creation statuses, Settings security-operation statuses, and Trash runtime/destructive-action statuses resolve reviewed resources and use `CultureInfo.CurrentUICulture` where formatting is required.
- Explicit English maps to `en-US`; explicit Hindi maps to `hi-IN`; System preserves the process-start system UI culture.
- The saved preference is applied at startup/resume and when the user changes it in Settings.
- Missing culture-specific translations fall back to neutral English through normal `ResourceManager` fallback within the catalog that owns the key.
- Markup-extension values are resolved when the XAML element is constructed. A page that was already constructed before a language change can retain its existing fixed text until that page is reconstructed; do not claim live in-place translation for every existing visual tree.
- `LocalizationSourceTests` protects primary-catalog parity, preference wiring, fallback behavior, and honest completeness wording.
- Dedicated localization source tests guard TOTP, Unlock, onboarding/recovery, About security/privacy, Settings, and Trash resource catalogs, XAML usage, dynamic/status formatting, fail-safe messages, and removal of selected previous hard-coded security copy.

## Reviewed Hindi scope

The reviewed Hindi resources cover the resource-backed product/title/navigation controls already represented by `AppStrings` plus the security-sensitive local-only, audit-status, recovery-limitation, language-preference status messages, migrated TOTP workflow, initial Unlock workflow, initial vault-onboarding/recovery workflow, About security/privacy claims, migrated Settings fixed surface/security operations, and the complete fixed/runtime Trash permanent-deletion surface.

### TOTP workflow

The TOTP resource-backed surface includes:

- the TOTP heading and local-only seed/code explanation;
- setup-URI import heading, bounded URI explanation, placeholder, and import action;
- setup-URI copy action;
- algorithm/digit labels and refresh/copy-code actions;
- semantic descriptions for setup-URI import/copy and generated-code copy;
- period and refreshed-code validity formatting;
- generation/import/copy success and failure statuses;
- the authorization/clipboard-history/synchronization warning.

### Unlock workflow

The resource-backed Unlock surface includes:

- local-only/recovery limitation statements;
- biometric action and accessibility description;
- master-passphrase/recovery-key fallback labels and credential semantics;
- periodic master-passphrase requirement status;
- bounded failed-attempt delay formatting;
- authentication failure status;
- biometric prompt, cancellation/failure, protected-secret loss, and mismatch statuses.

### Onboarding and recovery workflow

The resource-backed onboarding surface includes:

- local-vault setup title and local-only statement;
- one-time recovery-key explanation and accessibility semantics;
- explicit acknowledgement that the recovery key was stored separately;
- unrecoverable-vault warning when both master passphrase and recovery material are lost;
- master-passphrase and confirmation labels/placeholders;
- optional recovery-key choice and recovery-limit acknowledgement;
- onboarding password-strength presentation labels;
- master-passphrase bound/requirement feedback;
- vault-exists/initialization and unexpected setup-failure statuses.

The authoritative password-strength score and setup eligibility remain application behavior; localization changes presentation labels, not the strength algorithm or authorization policy.

### Settings workflow

The resource-backed Settings surface includes:

- Settings title/back navigation;
- appearance/accessibility section labels, theme/language labels, language-scope explanation, language-save action, reduced-motion and larger-interface labels;
- lock/privacy timing labels, background-lock, clipboard cleanup, screenshot-protection and trash-retention labels;
- local backup/review reminder labels plus the local-only reminder privacy explanation;
- generator-defaults section/action;
- biometric availability/security guidance and enable/disable controls;
- security-review navigation;
- encrypted backup/restore decision text and operation statuses;
- storage/cache fixed guidance and actions;
- import/export guidance;
- optional Buy Me a Coffee card text and accessibility descriptions while preserving the rule that funding never changes rights, security, privacy, recovery, licensing, or support priority;
- About/legal navigation;
- master-passphrase change and destructive-vault-deletion controls/statuses.

Some Settings values are generated dynamically at runtime (for example measured storage usage). A migrated fixed Settings surface does not by itself mean every dynamic message or every other screen in the application is fully localized.

### Trash and permanent deletion workflow

`TrashStrings.resx` and `TrashStrings.hi-IN.resx` own the Trash workflow as a cohesive feature catalog. The migrated surface includes:

- page title, Back action, status accessibility description, empty-view text, deleted-date label, Restore and Delete actions;
- permanent-deletion heading, current-master placeholder, Empty trash action, and the warning that recovery keys are not accepted for destructive re-authentication;
- trash-count/retention status formatting and already-empty state;
- per-item permanent-delete title/body/accept action;
- empty-trash title/count-formatted body/accept action;
- explicit storage-remnant/forensic limitations in destructive confirmations;
- missing-master and failed-master-confirmation statuses;
- completed empty-trash status.

The empty-trash command now publishes its completed success status after clearing the in-memory Trash list instead of immediately calling the general reload path that would overwrite the success message with `Trash is empty.`. This is a presentation-state fix only; permanent deletion still depends on the existing master re-authentication and vault-service deletion behavior.

## Security meaning requirements

Translated security text must preserve these meanings:

- the vault remains local to the device in ordinary operation;
- CipherNest has not completed an independent professional security audit;
- a forgotten master passphrase is not remotely recoverable and recovery depends on retained configured recovery material;
- an optional recovery key is shown during setup and must be retained separately because CipherNest cannot later retrieve it for the user;
- biometric unlock is convenience authentication and never removes the configured periodic master-passphrase requirement or recovery limitation;
- biometric cancellation/failure, protected-secret loss, or pairing mismatch must fall back to the master-passphrase path rather than weakening authentication;
- repeated failed interactive unlock attempts remain subject to the existing rate limiter;
- failed vault creation must not be represented as successful;
- TOTP setup URIs contain the seed and must be protected like the seed itself;
- TOTP URI parsing/import remains local and bounded, while HOTP remains intentionally unsupported at this boundary;
- clipboard history/synchronization can still expose copied secrets despite CipherNest's best-effort timed cleanup;
- failed TOTP operations must not be described as successful and must preserve documented unchanged-state guarantees;
- review reminders remain local and must not imply that vault details are sent to an external notification service;
- cache cleanup must not be described as deleting the encrypted vault database, encrypted attachment store, or app-data backups;
- Trash permanent deletion requires current-master re-authentication and must not imply that a recovery key is accepted for that destructive confirmation;
- deleting CipherNest-managed encrypted records/attachments must not be described as guaranteed physical erasure because filesystem, flash-storage, backup, snapshot, shared-copy, or forensic remnants can remain outside application control;
- optional funding must not imply different product rights, security, privacy, licensing, recovery, or support priority;
- untranslated or not-yet-migrated interface text elsewhere in CipherNest may still appear in English.

Hindi is therefore a supported **resource-backed language preference**, not a claim that the complete application UI is fully translated today.

## Adding or expanding a language

1. Add or update the culture-specific satellite for every neutral catalog that participates in the migrated surface; keep exact key parity inside each catalog pair.
2. Use `AppStrings` for genuinely shared/cross-feature strings. Prefer a neutral/satellite feature-catalog pair when a complete workflow is large enough to be maintained more clearly as one unit.
3. Register every new feature catalog in `LocalizationService` and add a focused test so an unregistered resource file cannot silently exist without runtime lookup.
4. Extend `AppLanguagePreference` only for a language whose reviewed catalogs required by the claimed scope are actually shipped.
5. Map that preference to an explicit `CultureInfo` in `LocalizationService`.
6. For fixed XAML text, prefer `TranslateExtension` instead of direct hard-coded security copy.
7. Keep custom MAUI markup extensions explicit about XAML service-provider requirements (`RequireService` when consuming services supplied by XAML, or `AcceptEmptyServiceProvider` when the extension intentionally does not require that context).
8. For dynamic formatted values or ViewModel operation messages, resolve reviewed resource keys and format with the active UI culture rather than embedding English-only `StringFormat` or status literals.
9. Keep authoritative validation/scoring/authorization logic language-neutral; translate presentation labels and messages rather than branching security policy by culture.
10. Move remaining literal UI copy to resource-backed bindings/services screen by screen; do not mark a screen translated until every user-facing/security-sensitive literal on it has been reviewed.
11. Keep resource keys language-neutral and stable. Do not encode a language into persistence, vault records, cryptographic associated data, or backup formats.
12. Test long strings, formatting placeholders, pluralization, screen-reader pronunciation, keyboard navigation, layout at large text sizes, and right-to-left behavior for languages where it applies.
13. Keep security warnings semantically equivalent; translations must not weaken recovery, export, audit, deletion, biometric, clipboard, TOTP, funding, storage/cache, or platform-limit wording.
14. Keep neutral English values as fallback so a missing satellite entry cannot produce a blank security warning.

## Release validation

For each language-enabled release candidate:

- verify neutral/satellite key parity in every primary/feature catalog pair;
- verify no blank translated values;
- verify required formatting placeholders remain present in every translated dynamic format;
- exercise language selection, page reconstruction/navigation, app restart, suspend/resume, and fallback behavior on target platforms;
- review every translated recovery, biometric, TOTP, Trash/destructive-action, funding, storage/cache, and audit warning against the canonical English security documentation;
- test Unlock and onboarding/recovery flows with disposable synthetic vault credentials only;
- test localized TOTP generation/setup-URI import/copy controls without placing real seeds, URIs, or codes in screenshots/logs/test artifacts;
- test Settings language switching, fixed-text reconstruction, funding-disabled builds, reminder/privacy copy, and cache/storage layouts;
- test Trash retention presentation, current-master destructive confirmation, per-item deletion, empty-trash confirmation, success-state visibility, and storage-remnant caveats using disposable synthetic vault data only;
- test responsive layout and accessibility services with the translated strings;
- keep any not-yet-migrated screens documented as potentially English.

See:

- `../verification/TOTP_LOCALIZATION_2026_08_19.md`;
- `../verification/AUTHENTICATION_LOCALIZATION_2026_08_19.md`;
- `../verification/SETTINGS_SURFACE_LOCALIZATION_2026_08_19.md`.

Localization remains presentation-only and must not change vault data, crypto formats, database schema, backup compatibility, recovery behavior, authorization semantics, or security boundaries.
