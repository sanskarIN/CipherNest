# TOTP and Reviewed Hindi Verification — 2026-08-14

This document defines the repository-side verification contract for the August 14, 2026 CipherNest local TOTP and reviewed Hindi resource-catalog continuation.

It records what must be true before the current source candidate can be treated as repository-gate complete. It does **not** claim physical-device certification, store approval, signing/notarization, or an independent professional security audit.

## Source scope

The continuation includes:

- a reviewed `hi-IN` satellite resource catalog with neutral-English fallback;
- persisted System / English / Hindi language preferences;
- localization parity/security-message source tests;
- `VaultItemType.OneTimePassword = 9` while preserving all earlier persisted numeric item-type values, especially `Custom = 8`;
- encrypted TOTP seed/settings fields on `VaultItem` with backward-compatible defaults for older JSON records;
- `ITotpService`, `TotpCodeResult`, `TotpPolicy`, and the platform-independent `TotpService` implementation;
- local RFC 6238-compatible SHA-1, SHA-256, and SHA-512 generation;
- 6- or 8-digit output and bounded 15–120-second periods;
- bounded formatted/normalized Base32 input and malformed alphabet/length/padding rejection;
- temporary decoded-seed/hash/counter byte-buffer zeroing where the implementation owns those buffers;
- Item Editor manual refresh and explicit clipboard copy with no background TOTP timer;
- re-authentication gating for protected TOTP items;
- password-audit exclusion for TOTP seeds while exact duplicate signatures retain TOTP parameters;
- encrypted SQLite/VaultService round-trip coverage using synthetic data;
- canonical TOTP security/format/API/user/developer/release documentation.

## Required automated tests

The exact current-head candidate must pass at least these new/affected checks in addition to all existing repository tests:

1. RFC 6238 known-answer vectors for SHA-1, SHA-256, and SHA-512 at the published test timestamps.
2. 6- and 8-digit generation.
3. lowercase/grouped Base32 normalization.
4. maximum normalized seed boundary under the formatted-input ceiling.
5. invalid alphabet, impossible Base32 length, invalid/non-terminal padding, unsupported algorithm/digits/period, formatted-input ceiling, and pre-Unix-epoch rejection.
6. `VaultItemValidator` rejection of invalid TOTP seed/settings.
7. `SecurityAuditService` exclusion of TOTP seeds from weak/reused-password findings.
8. duplicate-signature differentiation by TOTP algorithm/digits/period.
9. legacy `type: 8` encrypted-JSON compatibility remaining `VaultItemType.Custom`, with newly added TOTP fields taking their defaults when absent.
10. real encrypted vault round-trip of a synthetic TOTP item and absence of the synthetic Base32 seed as a plaintext UTF-8 subsequence in the stored encrypted envelope.
11. source tests requiring explicit refresh/copy TOTP UI, clearing of transient displayed code when sensitive settings change, and absence of a background timer.
12. neutral/Hindi resource key parity, non-empty Hindi values, reviewed security-critical translations, System/English/Hindi preference normalization, and `hi-IN` wiring.
13. documentation/source tests preserving TOTP security limitations and independent-audit wording.

## Required hosted current-head gate

Because this continuation changes Domain models, Application validation/contracts, Infrastructure cryptographic behavior, App UI/resources/DI, tests, and canonical documentation, historical green CI is not sufficient.

The immutable final candidate must receive its own successful GitHub Actions results for:

- UnitTests restore/build/analyzer/test;
- IntegrationTests restore/build/analyzer/test;
- UiTests/source tests restore/build/analyzer/test;
- core formatting verification;
- Windows Release build with analyzers;
- Windows Release build with `CipherNestEnableFundingLink=false`;
- Android Release build with analyzers;
- iOS simulator Release build with analyzers;
- Mac Catalyst Release build with analyzers;
- CodeQL initialization, analyzable core build, MAUI Android application build, and final analysis.

Any failed test, warning-as-error build failure, formatting failure, platform compile failure, or CodeQL failure is release-blocking until corrected on a new exact-head candidate.

## Device/manual gates that remain external

Repository-only automation cannot certify:

- real device clock correctness/drift behavior against an authentication provider;
- TOTP code readability/layout at all text scales and screen sizes;
- VoiceOver/TalkBack/Narrator pronunciation and focus behavior for the TOTP surface;
- real platform clipboard history/synchronization behavior for copied TOTP seeds/codes;
- screenshot/task-preview behavior around displayed codes;
- suspend/resume behavior while a TOTP code is displayed;
- secure-storage/biometric lifecycle interactions with protected TOTP items;
- complete Hindi layout/fallback behavior across all target devices;
- the meaning/review quality of every not-yet-migrated UI literal;
- signing/notarization/store-policy review;
- independent professional security review of the TOTP implementation or the wider application.

These remain release gates in `../TEST_PLAN.md`, `../RELEASE_CHECKLIST.md`, `../NEXT_STEPS.md`, `../security/TOTP.md`, and the platform/security documentation.

## Security claims that must remain explicit

- CipherNest has **not** completed an independent professional security audit.
- A TOTP seed is a long-lived authentication secret.
- Generated codes are not persisted, but they exist as managed presentation strings while displayed.
- Managed strings and operating-system copies cannot be deterministically erased by CipherNest.
- Copying a seed or code crosses into the platform clipboard boundary; cleanup is best effort.
- Storing a password and its TOTP seed in the same unlocked vault does not provide cryptographic factor separation from compromise of that vault.
- QR scanning/rendering, `otpauth://` enrollment import/export, provider enrollment, and autofill integration are not implemented by the current source.
- The reviewed Hindi catalog covers the resource-backed interface; CipherNest does not claim every remaining UI literal is translated.

## Compatibility rule

`VaultItemType` values are persisted numerically by the current JSON serializer. Existing values are therefore compatibility data. The new TOTP type is appended as `9`; `Custom` remains `8`. Future enum additions must preserve all existing values or introduce an explicit versioned migration/compatibility boundary.

The TOTP JSON members are additive and have defaults, so older supported records can deserialize without the new members. Any future incompatible TOTP representation change requires format/version review, migration/compatibility tests, and updates to `../formats/VAULT_RECORDS.md`.

## Commit identity

Repository commits in this continuation use the requested identity:

`Sanskar <sanskarin@outlook.in>`
