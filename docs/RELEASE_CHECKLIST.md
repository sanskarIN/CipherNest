# Release Checklist

Use this checklist for every CipherNest release candidate. A successful build alone is not release evidence.

## Candidate freeze and provenance

- [ ] Freeze one exact release-candidate commit/tag before collecting final evidence.
- [ ] Stop moving the candidate while final CI/device/manual evidence is collected.
- [ ] Record the exact candidate SHA/tag, UTC build time, source branch, SDK/workload/toolchain versions, package graph, and build flags.
- [ ] Record whether `CipherNestEnableFundingLink` is `true` or `false` for every distributed package.
- [ ] Confirm the candidate contains no real vaults, decrypted backups, passphrases, recovery material, secondary secrets, private keys, signing credentials, store tokens, TOTP seeds/setup URIs/current codes, or secret-bearing screenshots.
- [ ] Verify actual Git author/committer metadata independently when release provenance requires it; do not infer author email from a commit-message trailer.

## Exact-head automated verification

- [ ] Run `scripts/verify-core.ps1` or `scripts/verify-core.sh` against the exact candidate.
- [ ] Confirm UnitTests pass.
- [ ] Confirm IntegrationTests pass.
- [ ] Confirm UI/source tests pass.
- [ ] Confirm analyzer-enabled builds pass with warnings treated as errors.
- [ ] Confirm configured formatting checks pass.
- [ ] Confirm no skipped tests are being silently treated as equivalent to executed coverage unless the skip is explicitly reviewed and documented.
- [ ] Run/observe the exact-candidate Windows Release build.
- [ ] Run/observe the exact-candidate Windows `CipherNestEnableFundingLink=false` Release build.
- [ ] Run/observe the exact-candidate Android Release build.
- [ ] Run/observe the exact-candidate iOS simulator Release build on a compatible Apple host.
- [ ] Run/observe the exact-candidate Mac Catalyst Release build on a compatible Apple host.
- [ ] Run/observe CodeQL for the exact candidate, including the MAUI application path.
- [ ] Run/observe dependency review/vulnerability scanning for the exact restored package graph.
- [ ] Record exact workflow/run identifiers and results; configured workflows or historical green runs are not evidence for a later SHA.

## Cryptography and vault-header security

- [ ] Run Argon2id known-answer tests and AES-GCM round-trip/tamper/wrong-key/AAD tests.
- [ ] Confirm hostile KDF metadata is rejected before expensive Argon2 work.
- [ ] Confirm accepted KDF compatibility bounds and current new-wrapper defaults match documentation.
- [ ] Confirm historical supported vault-header versions remain readable.
- [ ] Confirm current vault-header writes use the current documented version/schema.
- [ ] Confirm duplicate/unknown/missing/case-variant/wrong-kind/deep/future/hybrid vault-header metadata is rejected before typed deserialization/wrapped-key unwrap.
- [ ] Confirm the 64 KiB vault-header UTF-8 and depth-16 JSON ceilings remain enforced.
- [ ] Confirm replacement-database validation applies the same strict vault-header policy before active DB/WAL/SHM mutation.
- [ ] Confirm no cryptographic/framing/schema compatibility change was made without an explicit version/migration decision and tests.

## Authentication, session, and destructive authorization

- [ ] Verify master-passphrase unlock.
- [ ] Verify independent recovery-material unlock.
- [ ] Verify recovery material does not authorize current-master-only sensitive operations.
- [ ] Verify invalid credentials map to authentication failure without raw crypto leakage.
- [ ] Verify bounded interactive failed-attempt backoff and reset-after-success behavior.
- [ ] Verify manual lock removes/zeroes shared key state where practical and cancels the current session token.
- [ ] Verify key-using operations use private zeroing session-linked key leases.
- [ ] Verify lock cancels blocked/cancellable key-using work.
- [ ] Verify master/recovery unlock, secondary unlock, lock, and full-vault deletion remain serialized through the security transition gate.
- [ ] Verify a late unlock cannot republish a session after an already-requested lock.
- [ ] Verify full-vault deletion keeps live-session authorization while waiting for the transition gate and fails if an intervening lock/re-unlock invalidates it.
- [ ] Verify caller cancellation cannot interrupt required destructive cleanup/rollback after the documented commit point.
- [ ] Verify cancellation callback/cleanup errors cannot reverse or mask an already-completed security transition.
- [ ] Verify master-passphrase rotation clears remembered master-auth state, locks the vault, and requires the new master before biometric convenience resumes.

## Biometrics and platform secure storage

Android:

- [ ] Test supported/enrolled biometric state.
- [ ] Test no enrollment.
- [ ] Test hardware unavailable.
- [ ] Test cancel/deny/failed match/lockout.
- [ ] Confirm the current source remains compatible with the API-28 `BiometricPrompt` baseline rather than depending on a newer preflight API.
- [ ] Test secure-storage loss/enrollment changes where feasible.

Apple:

- [ ] Test Face ID/Touch ID support on iOS/Mac Catalyst targets.
- [ ] Test denial/cancellation/failure.
- [ ] Verify request cancellation invalidates the native authentication context.
- [ ] Test secure-storage lifecycle/enrollment changes where feasible.

Across platforms:

- [ ] Verify a fresh process requires master-auth state before biometric convenience can later be used.
- [ ] Verify the configured periodic master requirement.
- [ ] Verify restore clears local biometric pairing.
- [ ] Verify master-passphrase change requires the new master before convenience unlock resumes.
- [ ] Confirm Windows does not advertise Windows Hello convenience unlock unless separately implemented/reviewed/tested.
- [ ] Do not claim hardware-backed cryptographic binding of every secure-storage retrieval unless separately proven for the exact implementation/platform.

## TOTP code generation and seed storage

- [ ] Run RFC 6238 known-answer vectors for SHA-1, SHA-256, and SHA-512.
- [ ] Verify 6- and 8-digit output.
- [ ] Verify 15–120-second periods and the documented default.
- [ ] Verify formatted/lowercase/grouped Base32 normalization.
- [ ] Verify malformed alphabet, impossible lengths/padding, non-zero residual bits, over-limit input, unsupported settings, and pre-epoch timestamps fail safely.
- [ ] Verify the deterministic hostile Base32 corpus executes with unique case IDs.
- [ ] Verify TOTP validity-window calculation does not overflow at `DateTimeOffset.MaxValue`.
- [ ] Verify temporary owned normalization/decoded/hash/counter buffers are cleared where practical.
- [ ] Verify `VaultItemValidator` applies TOTP seed/settings validation before save.
- [ ] Verify a synthetic TOTP item survives encrypted SQLite save/reopen and the seed is not present as plaintext bytes in the stored encrypted envelope.
- [ ] Verify generated codes remain transient presentation state and are not persisted.
- [ ] Verify changing seed/algorithm/digits/period/item type clears the displayed code.
- [ ] Verify protected TOTP items cannot generate/display/import/export sensitive material until required re-authentication succeeds.
- [ ] Verify TOTP seeds are excluded from ordinary password weakness/reuse semantics while duplicate detection includes TOTP settings.

## TOTP setup-URI interoperability

The text-based bounded `otpauth://totp/...` surface is implemented current functionality. QR/camera enrollment, HOTP, and automatic provider/autofill enrollment remain separate deferred work.

Automated/source gates:

- [ ] Run all `TotpUriCodecTests` against the exact candidate.
- [ ] Verify canonical TOTP URI parse and format/parse round trip.
- [ ] Verify omitted algorithm/digits/period use the documented SHA-1/6-digit/30-second defaults.
- [ ] Verify URI text is bounded to 8,192 characters and query processing to 16 pairs.
- [ ] Verify query names are bounded/validated and duplicate keys are rejected case-insensitively.
- [ ] Verify account and issuer metadata ceilings and Unicode Control/Format rejection.
- [ ] Verify label/query issuer disagreement is rejected when both are present.
- [ ] Verify wrong scheme, HOTP, `counter`, user-info, custom port, fragment, multi-segment label paths, malformed query syntax, invalid percent encoding, unsupported settings, malformed seed, and empty account are rejected.
- [ ] Verify imported seed/settings pass through the authoritative `TotpPolicy` rather than a weaker parallel validator.
- [ ] Verify `ITotpUriCodec -> TotpUriCodec` is registered once in the composition root.
- [ ] Verify the Item Editor routes parsing/formatting through the Application abstraction and does not contain a second URI parser.
- [ ] Verify the setup-URI input is masked and treated as transient sensitive state.
- [ ] Verify the bound URI field clears after successful/failed import and when Item Editor sensitive state is cleared.
- [ ] Verify setup URI is not stored as another `VaultItem` property/duplicate seed copy.
- [ ] Verify **Copy setup URI** uses the existing secret clipboard service/timed conditional cleanup path.
- [ ] Verify diagnostics/error text never includes the actual URI or seed.
- [ ] Confirm the implementation adds no QR/camera/network/cloud/provider dependency for this text-only interoperability surface.

Manual/interoperability gates using synthetic seeds only:

- [ ] Import representative compatible `otpauth://totp/...` values with percent-encoded account/issuer labels.
- [ ] Verify SHA-1/SHA-256/SHA-512 and 6-/8-digit settings with representative 30-/60-second periods where supported by the destination.
- [ ] Export a setup URI and verify representative compatible authenticators accept the expected synthetic account/settings.
- [ ] Verify the imported metadata is reviewed before save; local parsing does not prove issuer identity, provider enrollment, or account ownership.
- [ ] Verify deliberate HOTP/`counter` rejection remains clear and fail-closed.
- [ ] Verify the setup-URI field clears after success/failure and after leaving the Item Editor.
- [ ] Verify the actual URI/seed is not exposed through accessibility semantic text, screenshots, diagnostics, test logs, or store media.
- [ ] Verify platform clipboard history/synchronization behavior for copied setup URIs and document the limitation accurately.
- [ ] Treat setup-URI clipboard exposure as long-lived seed exposure, not as equivalent to one short-lived generated code.

## Vault items, storage bounds, and search

- [ ] Verify every persisted `VaultItemType` numeric value remains compatibility-safe.
- [ ] Verify null/empty/oversized/unknown item input is rejected through shared validation.
- [ ] Verify the combined item text/metadata ceiling remains enforced.
- [ ] Verify attachment metadata/ID/storage-name uniqueness validation remains enforced.
- [ ] Verify decrypted payload item ID equals the authenticated SQLite row ID before returning it to application/UI code.
- [ ] Verify serialized/decrypted item JSON, stored-envelope, item-count, aggregate-envelope, and global-attachment ceilings remain enforced.
- [ ] Verify SQLite count/length checks happen before BLOB materialization where practical.
- [ ] Verify search rejects over-limit queries before matching decrypted fields.
- [ ] Verify no plaintext persistent full-text index/cache for decrypted vault fields has been introduced without explicit privacy/security review.
- [ ] Verify filters/sorts/recent-access/reminders operate only on decrypted authenticated data while unlocked.
- [ ] Verify large result sets render incrementally without changing the at-rest privacy model.

## Attachments and plaintext preview/export

- [ ] Run attachment round-trip/tamper/truncation/chunk/AAD tests.
- [ ] Verify 100 MiB plaintext/file, 25 attachments/item, and 10,000 referenced attachments/global limits.
- [ ] Verify metadata normalization/validation occurs before encryption.
- [ ] Verify malformed UTF-16 and Unicode Control/Format metadata are rejected.
- [ ] Verify opaque storage names remain exact GUID-N `.cna` names bound to attachment ID and are validated before filesystem access.
- [ ] Verify encrypted attachment staging uses unique `CreateNew` behavior and no final overwrite.
- [ ] Verify reusable owned plaintext buffers are cleared where practical.
- [ ] Verify attachment mutation serialization does not block security lock from cancelling long work.
- [ ] Verify safe text preview type/UTF-8/size/display limits and no intended plaintext preview file.
- [ ] Verify attachment plaintext export requires explicit warning/confirmation.
- [ ] Verify exported plaintext staging uses a unique cache path and best-effort cleanup.
- [ ] Verify cleanup failure is reported without leaking sensitive paths.
- [ ] Confirm documentation does not promise guaranteed deletion from destination apps, OS caches, snapshots, backups, or physical media.

## Encrypted backup and restore

- [ ] Run normal encrypted backup/restore round trips using synthetic vaults with and without attachments/TOTP items.
- [ ] Verify the backup passphrase remains separate from the vault-master API contract.
- [ ] Verify backup locks the vault before consistent snapshot creation.
- [ ] Verify the destination cannot target the active DB/WAL/SHM/recovery/attachment paths.
- [ ] Verify encrypted staging is collision resistant.
- [ ] Verify backup header strict schema/depth/size/type/duplicate rules before Argon2.
- [ ] Verify wrong backup passphrase/corrupt/truncated/unsupported framing fails without replacing the active vault.
- [ ] Verify export and restore share the same 10,001-entry/1 GiB archive resource envelope.
- [ ] Verify duplicate normalized ZIP paths and unexpected/nested paths are rejected.
- [ ] Verify encrypted attachment entries fit the attachment-container size envelope.
- [ ] Verify actual extracted bytes must exactly equal declared uncompressed length and cannot exceed the remaining aggregate budget.
- [ ] Verify staged SQLite candidates pass integrity/schema/header/ID/resource validation before active replacement.
- [ ] Verify failed replacement preserves/restores DB/WAL/SHM correctly.
- [ ] Verify caller cancellation cannot cancel required rollback after the active-mutation commit point.
- [ ] Verify restored biometric pairing is cleared.
- [ ] Verify restored synthetic TOTP seed/settings still generate expected codes.
- [ ] Verify setup-URI text itself is not expected as a second persisted backup field; only the encrypted TOTP item data is restored.

## CSV import/export

- [ ] Verify valid quoting, commas/newlines, Unicode, BOM handling, and explicit mapping.
- [ ] Verify duplicate/empty/unsafe/oversized headers are rejected.
- [ ] Verify 256-column enforcement includes the final field at newline/EOF.
- [ ] Verify field/aggregate-row/data-row ceilings.
- [ ] Verify mapped Tags enforce the canonical 100-tag/128-character limits before item construction without unbounded whole-field materialization.
- [ ] Verify warnings do not echo secret-bearing raw rows.
- [ ] Verify plaintext CSV export requires exact `EXPORT PLAINTEXT`, current-master re-authentication, warning, and confirmation.
- [ ] Verify attachments are not silently included in plaintext CSV export.
- [ ] Verify temporary plaintext staging cleanup is best effort and path-safe.
- [ ] Confirm generic CSV is documented separately from dedicated single-item TOTP setup-URI interoperability.

## Settings, lifecycle, clipboard, and privacy

- [ ] Verify complete `AppPreferences` round trip and normalization.
- [ ] Verify malformed/invalid UTF-8/over-depth/oversized settings use normalized fallback while cancellation propagates.
- [ ] Verify the 64 KiB settings ceiling and 64 KiB + 1 actual-read sentinel behavior.
- [ ] Verify lock timeout, background lock, screenshot preference, clipboard delay, biometric settings, master-auth interval, trash retention, reminders, generator defaults, theme/language/accessibility settings.
- [ ] Verify manual/background/timeout lock behavior on target platforms.
- [ ] Verify lifecycle fallback contains/reports secondary lock/clipboard failures instead of allowing cleanup exceptions to escape native callbacks.
- [ ] Verify username/secret/custom-secret/TOTP-code/TOTP-setup-URI copy is always explicit.
- [ ] Verify delayed clipboard state contains only a fixed-size fingerprint, comparison is fixed-time, initiating caller cancellation does not disable the security timer, and unrelated newer clipboard content is preserved.
- [ ] Verify operating-system clipboard history/synchronization limitations are documented truthfully.
- [ ] Verify sensitive credential/setup-URI fields are cleared when the owning page/workflow can shorten their lifetime.
- [ ] Verify managed-memory erasure limitations remain explicit.

## Diagnostics and support safety

- [ ] Verify no third-party analytics/crash-reporting provider was enabled without a separate privacy/security review.
- [ ] Verify privacy-safe diagnostic events use stable operation identifiers/fixed text and omit raw exception messages/stacks.
- [ ] Verify diagnostic/support paths do not include master/backup passphrases, recovery material, secondary secrets, DEKs/KEKs, decrypted item/attachment content, raw secret-bearing CSV rows, identifying filesystem paths, TOTP seeds, generated codes, or setup URIs.
- [ ] Verify support/security guidance instructs reporters to use synthetic reproduction data and never send a real setup URI/seed/current code.

## Accessibility and localization

- [ ] Validate TalkBack on Android.
- [ ] Validate VoiceOver on iOS/Mac Catalyst where applicable.
- [ ] Validate Narrator and keyboard/focus behavior on Windows.
- [ ] Validate focus order/visibility, semantic names/descriptions/live regions, touch targets, large text, Larger Interface, Reduced Motion, and light/dark/system readability.
- [ ] Validate narrow/landscape/tablet/resizable-desktop layouts.
- [ ] Verify TOTP setup-URI semantic metadata describes the field/action without containing the actual URI/seed.
- [ ] Verify System/English/Hindi preference, fallback, restart/resume, and reviewed `hi-IN` resource catalog behavior.
- [ ] Verify new TOTP setup-URI UI strings remain usable at large text/narrow widths; migrate/review them before claiming complete Hindi coverage.
- [ ] Do not claim every remaining literal is translated until the resource migration/review is complete.

## Branding, funding, legal, and store material

- [ ] Verify original CipherNest icon/splash/monochrome/dark-surface/BMC branding sources.
- [ ] Verify `Made by the Sanskar` creator credit remains on appropriate branding/About surfaces rather than user content.
- [ ] Verify BMC URL consistency across shared constants, README, SUPPORT, `.github/FUNDING.yml`, About, Settings, and Vault guarded surfaces.
- [ ] Verify optional funding does not change feature access, security/privacy treatment, support priority, licensing, recovery behavior, or open-source rights.
- [ ] Verify the target store/region currently permits the selected external funding CTA; use `CipherNestEnableFundingLink=false` where required and record that choice.
- [ ] Verify GPL-3.0-or-later, Privacy, Terms, SECURITY, Support, and third-party notices match the exact release.
- [ ] Verify store/listing copy accurately describes local TOTP generation and bounded text setup-URI interoperability without implying QR/camera enrollment, HOTP, provider enrollment, cloud sync, Windows Hello, full translation, or independent audit.
- [ ] Verify store screenshots/video/demo data contain only synthetic TOTP values and never a real seed/setup URI/current code.
- [ ] Verify no unsupported “unhackable”, “military-grade”, “100% secure”, guaranteed erasure, guaranteed biometric/hardware, or server-reset recovery claim appears.

## Dependencies and supply chain

- [ ] Review the exact restored direct/transitive package graph.
- [ ] Review current vulnerability advisories.
- [ ] Review dependency-review/CodeQL/secret-scanning results.
- [ ] Reconcile exact licenses/notices with `THIRD_PARTY_NOTICES.md`.
- [ ] Document owned exceptions with severity/owner/expiry rather than silently suppressing a finding.
- [ ] Confirm the TOTP setup-URI continuation did not introduce a QR/camera/network/parser dependency that escaped license/advisory review.

## Packaging, signing, and platform release

- [ ] Build release packages from the immutable candidate in protected environments.
- [ ] Keep signing private keys/certificates/passwords/store tokens outside Git history/logs/artifacts that expose them.
- [ ] Verify application ID, version/build number, permissions/capabilities, icons, splash assets, privacy declarations, and package metadata.
- [ ] Verify Android signing/package/install behavior.
- [ ] Verify iOS signing/provisioning/package/install behavior.
- [ ] Verify Mac Catalyst signing/notarization/package behavior.
- [ ] Verify Windows packaging/signing behavior for the intended distribution channel.
- [ ] Preserve package hashes/provenance where practical.

## Independent security review and final release decision

- [ ] Record the exact status/scope/date/version of any independent professional security review.
- [ ] If no independent professional audit exists, keep that limitation visible in README/docs/store/security surfaces.
- [ ] Review cryptographic key hierarchy, KDF/AAD/nonce assumptions, session/key-lease concurrency, vault-header/record/database replacement, attachments, backup/rollback, CSV, TOTP code generation, TOTP setup-URI parser/formatter, clipboard/plaintext lifecycle, resource ceilings, diagnostics, and dependency posture.
- [ ] Resolve or explicitly own outstanding findings before release.
- [ ] Verify `CHANGELOG.md`, `PROJECT_STATUS.md`, `docs/FEATURE_MATRIX.md`, `docs/NEXT_STEPS.md`, `what_changed.md`, and canonical documentation match the exact candidate.
- [ ] Create the release tag only after the applicable gates above are complete.
- [ ] Preserve exact run IDs/device matrix/interoperability results/dependency review/package provenance with the release record.

## Current historical baseline note

The immutable pre-documentation implementation baseline remains:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

Recorded historical evidence for that exact SHA is 346 Unit + 98 Integration + 111 UI/source = **555 passed, 0 failed, 0 skipped**, with Windows default/funding-disabled, Android, iOS simulator, Mac Catalyst, core formatting/analyzer gates, and CodeQL successful in the recorded runs.

The August 18, 2026 TOTP setup-URI implementation/documentation is newer than that baseline. It must not inherit the old exact-head verification claim. Freeze the final August 18 continuation head and record that head's completed configured runs separately before treating it as release-candidate verified.
