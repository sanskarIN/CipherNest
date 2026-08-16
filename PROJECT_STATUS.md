# CipherNest Project Status

## Current release line

**0.1.0 + unreleased hardening/documentation**

This status separates:

1. implemented current source;
2. exact hosted evidence;
3. platform/external release validation still required;
4. deliberately deferred future-version work.

> **Security status:** CipherNest has **not** completed an independent professional security audit. Passing tests, platform compilation, and CodeQL are valuable engineering evidence but do not justify claims such as “unhackable”, “military-grade”, or “100% secure”.

# 1. Current immutable implementation baseline used for documentation

The complete-documentation expansion is grounded in exact implementation commit:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

For that exact SHA:

- CipherNest CI run `31937127961`: **success**;
- CodeQL run `31937127900`: **success**;
- UnitTests: **346 passed**;
- IntegrationTests: **98 passed**;
- UI/source tests: **111 passed**;
- total: **555 passed, 0 failed, 0 skipped**;
- analyzer-enabled core test builds completed with zero build warnings/errors;
- configured core formatting passed;
- Windows default Release passed;
- Windows `CipherNestEnableFundingLink=false` Release passed;
- Android Release passed;
- iOS simulator Release passed;
- Mac Catalyst Release passed;
- CodeQL v4 passed after analyzable core and MAUI application builds.

That is exact evidence only for `8566980f...`. Documentation commits after it require their own exact-head runs before the later SHA can be called release-candidate verified.

Historical 240-test and 554-test verification records remain preserved for their original exact candidates.

# 2. Documentation status

The project now has a full canonical documentation suite including:

- `docs/QUICK_START.md`;
- `docs/FEATURE_MATRIX.md`;
- `docs/UI_REFERENCE.md`;
- `docs/CONFIGURATION_REFERENCE.md`;
- `docs/COMPLETE_PROJECT_DOCUMENTATION.md`;
- `docs/USER_GUIDE.md`;
- `docs/FAQ.md`;
- `docs/DEVELOPER_GUIDE.md`;
- `docs/MAINTAINER_GUIDE.md`;
- `docs/API_REFERENCE.md`;
- `docs/LIMITS_AND_DEFAULTS.md`;
- full architecture/security/privacy/format/build/testing/release/operations documentation;
- documentation verification records and source-regression tests.

The complete-documentation source-to-doc gate is `docs/verification/COMPLETE_DOCUMENTATION_2026_08_16.md`.

# 3. Implemented core architecture

- .NET 10 / .NET MAUI multi-project solution.
- `CipherNest.Shared`, `CipherNest.Domain`, `CipherNest.Application`, `CipherNest.Infrastructure`, `CipherNest.App` separation.
- Unit, Integration, and UI/source test projects.
- Dependency-inverted Application abstractions implemented by Infrastructure/App.
- MAUI composition root and explicit Shell route model.
- Windows, Android, iOS, and Mac Catalyst application targets.

# 4. Implemented cryptography and vault security

- Random 256-bit vault DEK.
- Argon2id master-passphrase key wrapping.
- Optional independent recovery wrapper.
- Optional independent secondary/biometric wrapper.
- AES-256-GCM authenticated encryption for wrapped keys, records, backup chunks, and attachment chunks.
- Unique nonces and contextual associated data.
- Explicit KDF resource bounds before expensive work.
- Explicit cryptographic/database/header/backup/attachment compatibility versions.
- Strict vault-header JSON schema, size, depth, duplicate/unknown/property-kind validation before unwrap.
- Historical v1 vault-header compatibility with current v2 writer.
- Managed-memory limitations explicitly documented.

# 5. Implemented authentication/session behavior

- Master-passphrase unlock.
- Recovery-material unlock.
- Current-master re-authentication for sensitive actions.
- Optional Android/iOS/Mac Catalyst biometric convenience unlock.
- Windows master-passphrase fallback.
- Fresh-process master requirement before convenience unlock can later be used.
- Configurable periodic master-passphrase interval.
- Bounded interactive failed-attempt backoff.
- Serialized create/unlock/secondary-unlock/lock/full-delete transitions.
- Private 32-byte cancellable `VaultKeyLease` copies.
- Shared session key zero/removal on lock where practical.
- Session cancellation for cancellable in-flight key-using work.
- Live-session authorization for destructive full-vault deletion.
- Separate cancellable attachment mutation serialization.

# 6. Implemented vault item model

Current item types:

```text
Login = 0
SecureNote = 1
Identity = 2
PaymentCardReference = 3
WifiCredential = 4
SoftwareLicense = 5
ServerSshReference = 6
Document = 7
Custom = 8
OneTimePassword = 9
```

Implemented item features include:

- title;
- username/identifier;
- primary secret;
- URL;
- secure notes;
- collection/folder;
- tags;
- favorites;
- custom fields including secret custom fields;
- attachments;
- optional review date;
- encrypted last-accessed timestamp;
- trash state;
- optional per-item current-master re-authentication;
- TOTP settings for OTP items.

# 7. Implemented search, organization, and reminders

- Local search over decrypted authenticated data only while unlocked.
- No plaintext persistent FTS index for vault fields.
- Collection filtering.
- Item-type filtering.
- Favorites filtering.
- Review-due filtering.
- Favorites/title sort.
- Recently used sort.
- Recently modified sort.
- Title sort.
- Incremental 50-item visual rendering.
- Local backup reminders.
- Local review reminders.

# 8. Implemented local security audit

The vault-content audit reports:

- weak secrets;
- reused secrets;
- exact duplicate entries;
- missing titles;
- overdue review dates.

TOTP seeds are excluded from ordinary password weakness/reuse heuristics. This audit is not an independent audit of CipherNest source code.

# 9. Implemented TOTP

- Encrypted Base32 seed storage inside authenticated vault records.
- SHA-1, SHA-256, SHA-512.
- 6 or 8 digits.
- 15–120-second period.
- 30-second default.
- RFC 6238 known-answer tests.
- Bounded Base32 normalization/validation.
- Explicit manual refresh.
- Explicit code copy through the timed clipboard policy.
- Generated codes are transient and not persisted.
- Temporary decoded/hash/counter buffers zeroed where practical.
- Max-timestamp validity arithmetic safely clamped.

TOTP QR scanning/rendering, `otpauth://` import/export, and provider/autofill integration are not implemented.

# 10. Implemented generators and secure notes

## Generator

- Cryptographic RNG password generation.
- Uppercase/lowercase/digits/symbols controls.
- Ambiguous-character exclusion.
- Password length 8–256.
- Validated exactly-256-word local passphrase vocabulary.
- 6–16 words, default 8.
- Persisted generator defaults.
- Entropy/strength guidance.

## Secure notes

- Safe Markdown-like subset.
- Headings, paragraphs, bullets, checklists, fenced code.
- Raw HTML neutralized.
- Shared 200,000-character / 5,000-line limits.

# 11. Implemented encrypted attachments

- Authenticated chunked `.cna` format.
- Bounded streaming encryption/decryption.
- 100 MiB plaintext file ceiling.
- 25 attachments/item.
- 10,000 referenced attachments global ceiling.
- Exact GUID-N `.cna` opaque storage-name policy.
- Attachment-ID/storage-name binding.
- Rune-aware display/media metadata validation.
- Malformed UTF-16 and Unicode Control/Format rejection.
- Collision-resistant `CreateNew` staging.
- Final overwrite refusal.
- Owned plaintext chunk-buffer zeroing where practical.
- Bounded UTF-8 text-family preview.
- Explicit plaintext export warning/share/temp-cleanup flow.
- Session lock cancellation coverage for a blocked decrypted attachment export.

# 12. Implemented encrypted backup/restore

- `.cnbak` authenticated encrypted backup format.
- Separate backup passphrase.
- Consistent SQLite snapshot after vault lock.
- Encrypted attachment inclusion.
- Strict bounded version-2 backup-header JSON before Argon2.
- Backup destination protection against active DB/WAL/SHM/recovery/attachment paths.
- Collision-resistant encrypted staging.
- 10,001 archive-entry ceiling.
- 1 GiB aggregate plaintext archive ceiling.
- Duplicate normalized ZIP path rejection.
- Attachment encrypted-container size validation.
- Exact extracted-length checking.
- Pre-swap SQLite `quick_check`, schema, table/column, vault-header, item-ID, and resource validation.
- Unique DB/WAL/SHM recovery staging.
- Component-aware rollback.
- Uncancelled recovery token after active mutation begins.
- Biometric pairing cleared after successful restore.

# 13. Implemented CSV transfer

- Bounded streaming parser.
- Explicit user column mapping.
- 256-column bound.
- 100,000-row bound.
- 256-character header-name bound.
- Rune-aware Control/Format header rejection.
- Final-field column enforcement at newline/EOF.
- Aggregate row/field limits.
- Mapped Tags bounded to canonical item limits before item construction.
- Guarded plaintext CSV export.
- Exact `EXPORT PLAINTEXT` acknowledgement.
- Current-master re-authentication.
- Explicit warning/confirmation.
- Best-effort temporary plaintext cleanup.
- Attachments excluded from plaintext CSV export.

# 14. Implemented SQLite/migration/replacement hardening

- Schema version 1.
- `VaultHeader`, `VaultItems`, `AppSettings`, `MigrationHistory`.
- Transactional ordered migrations.
- Future schema rejection.
- Required table/column shape validation after version resolution.
- Forged-current migration history rejection.
- Rollback-error containment.
- Read-only candidate validation before active replacement.
- Canonical item-ID checks.
- Count/per-row/aggregate encrypted-record budgets.
- DB/WAL/SHM unique recovery sets.
- Component-aware rollback.
- Full database deletion includes primary/sidecar/recovery artifact attempts.

# 15. Implemented settings, privacy, and lifecycle behavior

- System/Light/Dark theme.
- System/English/Hindi language preference.
- Lock timeout 5–3,600 seconds; default 60.
- Lock on background; default enabled.
- Clipboard clear 5–300 seconds; default 30.
- Screenshot-protection preference; default enabled.
- Biometric convenience preference.
- Master re-auth interval 1–168 hours; default 24.
- Reduced motion.
- Larger interface.
- Trash retention 1–365 days; default 30.
- Backup reminder 1–365 days; default 7.
- Review reminders and 0–365-day lead time; default 7.
- Generator defaults.
- Storage/cache inspection and cleanup.
- Backup/restore/transfer/security/About/legal routes.
- Master-passphrase change.
- Full-vault deletion.

Settings JSON is bounded to 64 KiB, read through a 64 KiB + 1 sentinel buffer, limited to depth 16, normalized after parsing, and safely falls back for malformed/unreadable non-secret content while preserving cancellation.

Lifecycle fail-closed paths contain/report secondary lock/clipboard errors rather than allowing another cleanup exception to escape the native callback.

# 16. Implemented clipboard/privacy-safe diagnostics

- Explicit copy for username, primary secret, secret custom fields, and TOTP codes.
- Delayed state stores SHA-256 fingerprint rather than copied plaintext.
- Fixed-time fingerprint comparison.
- Newer unrelated clipboard content preserved.
- Conditional lock-triggered cleanup where supported.
- Privacy-safe central exception reporter.
- Raw exception messages/stacks intentionally omitted from that reporter.
- No third-party analytics enabled.
- No third-party crash-reporting provider enabled.
- Sensitive ViewModel fields cleared on sensitive page disappearance where owned.
- Bound credential fields cleared before several longer operations where practical.

# 17. Implemented accessibility/localization source support

- Semantic UI metadata.
- Selected state/live-region semantics.
- Dynamic larger-interface typography.
- Reduced-motion preference state.
- Responsive phone/desktop layouts.
- Wrapping Vault actions for narrow/resizable windows.
- Neutral English fallback resources.
- System/English/Hindi persisted preference.
- Reviewed `hi-IN` resource-backed catalog.

Complete translation of every remaining literal is not claimed. Physical assistive-technology validation remains external.

# 18. Implemented BMC/branding/support surface

Current original project branding includes:

- app/adaptive icon vector sources;
- splash wordmark;
- `Made by the Sanskar` creator credit;
- monochrome source;
- dark-surface logo;
- original `bmc_support.svg`.

BMC support is highlighted in:

- `.github/FUNDING.yml`;
- root README;
- `SUPPORT.md`;
- About;
- Settings full BMC card;
- Vault `☕ Support` action.

Funding is voluntary and does not change product rights/treatment. In-app funding UI can be disabled at build time with:

```text
CipherNestEnableFundingLink=false
```

# 19. Resource ceilings currently enforced

Highlights:

```text
Vault header: 64 KiB / depth 16
Decrypted item JSON: 16 MiB
Stored encrypted envelope: 24 MiB/row
Item count: 100,000
Aggregate encrypted envelopes: 256 MiB
Referenced attachments: 10,000
Combined item text/metadata: 2,000,000 chars
Attachment plaintext: 100 MiB/file
Attachments/item: 25
Secure Note: 200,000 chars / 5,000 lines
Search query: 4,096 trimmed chars
Settings JSON: 64 KiB / depth 16
Backup archive: 1 GiB aggregate / 10,001 entries
Crypto-bound passphrase input: 12–4,096 chars
```

See `docs/LIMITS_AND_DEFAULTS.md` for the authoritative full table.

# 20. What still requires external release validation

Repository automation does not complete these gates:

- physical Android biometric enrollment/absence/cancellation/lockout/secure-storage testing;
- iOS/Mac Catalyst Face ID/Touch ID/secure-storage runtime testing;
- real clipboard history/sync/cleanup behavior;
- background/sleep/resume lifecycle timing;
- screenshot/app-switcher privacy behavior;
- OS share-sheet plaintext retention/cleanup;
- TalkBack/VoiceOver/Narrator/keyboard/focus/large-text/contrast validation;
- representative phone/tablet/resizable-desktop layout validation;
- stress/interleaving/filesystem-recovery testing beyond automated cases;
- historical released-version migration/backup compatibility testing;
- exact release package dependency/advisory/license review;
- signing/provisioning/notarization;
- store privacy declarations and submission/review;
- store/region external BMC/funding policy verification;
- independent professional cryptographic/security review.

# 21. Deliberately deferred future-version features

- cloud synchronization/accounts/collaboration/server storage;
- multi-device conflict resolution;
- browser/application autofill;
- Windows Hello convenience unlock;
- TOTP QR scanning/rendering;
- bounded `otpauth://` import/export;
- TOTP provider/autofill enrollment;
- rich PDF/binary preview and document scanning;
- pronounceable-password mode;
- destructive automatic wipe after failed attempts;
- complete migration/review of remaining UI literals into Hindi;
- additional complete language catalogs.

Deferred features are not represented as complete.

# 22. Current documentation suite

Canonical entry points:

- `docs/README.md`
- `docs/QUICK_START.md`
- `docs/FEATURE_MATRIX.md`
- `docs/UI_REFERENCE.md`
- `docs/CONFIGURATION_REFERENCE.md`
- `docs/COMPLETE_PROJECT_DOCUMENTATION.md`
- `docs/USER_GUIDE.md`
- `docs/FAQ.md`
- `docs/DEVELOPER_GUIDE.md`
- `docs/MAINTAINER_GUIDE.md`
- `docs/API_REFERENCE.md`
- `docs/LIMITS_AND_DEFAULTS.md`
- architecture/security/privacy/format/testing/release/operations specialist docs.

The complete-documentation verification contract is `docs/verification/COMPLETE_DOCUMENTATION_2026_08_16.md`.

# 23. Next steps

The ordered external-validation/release/future-development roadmap is maintained in `docs/NEXT_STEPS.md`.

Immediate release-oriented sequence:

1. preserve one immutable candidate;
2. run exact-head CI/CodeQL/dependency/documentation gates;
3. execute device security/lifecycle/clipboard/screenshot tests;
4. validate backup/restore/recovery/interoperability on targets;
5. execute accessibility/localization/responsive/performance validation;
6. review dependencies/licenses/advisories;
7. package/sign/notarize;
8. complete store privacy/policy review including BMC setting;
9. obtain independent security review before broader security claims;
10. tag/publish only with recorded evidence.
