# CipherNest Feature Matrix

This document provides a source-grounded feature/status matrix for the current CipherNest repository. It distinguishes **implemented source behavior**, **implemented but platform-dependent behavior**, **external release validation**, and **deliberately deferred future work**.

> Passing source tests or hosted compilation is not equivalent to independent security review, store approval, or physical-device validation.

## Status legend

| Status | Meaning |
|---|---|
| **Implemented** | Present in current source and covered by the documented source/test architecture. |
| **Implemented / platform-dependent** | Source path exists, but behavior depends on OS/device capabilities and still requires target validation. |
| **Release validation required** | Not a missing implementation feature; evidence must be gathered on exact release targets or external systems. |
| **Deferred** | Intentionally not represented as complete in the current release. |
| **Not a product capability** | Explicitly outside the current local-first product model. |

## 1. Core product and storage

| Capability | Status | Notes |
|---|---|---|
| Local-first vault | **Implemented** | Ordinary operation requires no CipherNest-hosted account/server. |
| Local encrypted SQLite persistence | **Implemented** | Vault records remain authenticated encrypted envelopes. |
| Plaintext full-text SQL index | **Not a product capability** | CipherNest intentionally avoids plaintext searchable indexes for vault fields. |
| Random vault data-encryption key | **Implemented** | 256-bit random DEK protects vault records/attachments. |
| Master-passphrase wrapped key | **Implemented** | Argon2id-derived KEK wraps the random DEK. |
| Independent recovery wrapper | **Implemented** | Optional recovery material wraps the same DEK through a separate authenticated path. |
| Optional secondary/biometric wrapper | **Implemented / platform-dependent** | Separate random secondary secret protects another DEK wrapper. |
| SQLite schema migrations | **Implemented** | Ordered transactional migration runner with required-shape validation. |
| Database replacement validation | **Implemented** | `quick_check`, schema, header, IDs, resource ceilings before active replacement. |
| WAL/SHM recovery staging | **Implemented** | Unique recovery sets and component-aware rollback. |
| Storage/resource ceilings | **Implemented** | Record, aggregate, attachment, parser, note, backup, and settings bounds. |

## 2. Authentication and session security

| Capability | Status | Notes |
|---|---|---|
| Master-passphrase vault creation | **Implemented** | Minimum 12 characters, maximum 4,096 characters. |
| Master-passphrase unlock | **Implemented** | Invalid credentials map to authentication failure rather than raw crypto errors. |
| Recovery-material unlock | **Implemented** | Recovery is independent local material, not server reset. |
| Current-master re-authentication | **Implemented** | Used for sensitive/destructive operations. |
| Interactive failed-attempt backoff | **Implemented** | Bounded exponential client-side delay; not offline-attack protection. |
| Manual lock | **Implemented** | Clears shared session key and cancels session-linked operations where practical. |
| Inactivity lock | **Implemented** | Configurable 5–3,600 seconds. |
| Lock on background | **Implemented / platform-dependent** | Enabled by default; lifecycle behavior still requires device validation. |
| Serialized unlock/lock/delete transitions | **Implemented** | Prevents stale late unlocks/destructive authorization across sessions. |
| Cancellable private vault-key leases | **Implemented** | 32-byte copies, linked caller + session cancellation, zero on disposal. |
| Periodic master requirement after biometric use | **Implemented** | Configurable 1–168 hours. |
| Fresh-process master requirement before biometric convenience | **Implemented** | Prevents convenience unlock from becoming a master-passphrase replacement. |
| Perfect managed-memory erasure | **Not a product capability** | .NET strings/GC/OS copies cannot be deterministically erased. |

## 3. Biometric convenience unlock

| Platform | Status | Notes |
|---|---|---|
| Android biometric convenience unlock | **Implemented / platform-dependent** | Uses API-28 `BiometricPrompt` baseline; device enrollment/lockout/hardware matrix remains a release gate. |
| iOS biometric convenience unlock | **Implemented / platform-dependent** | Uses Apple authentication context; runtime Face ID/Touch ID validation required. |
| Mac Catalyst biometric convenience unlock | **Implemented / platform-dependent** | Same convenience model; secure-storage/runtime validation required. |
| Windows Hello unlock | **Deferred** | Current Windows flow uses master-passphrase fallback. |
| Biometric configuration after restore | **Implemented** | Restore clears local pairing; user must deliberately re-enable. |
| Biometric continuation after master-passphrase change | **Implemented** | Rotation clears remembered master-auth session and requires the new master first. |
| Hardware-backed biometric cryptographic guarantee | **Not claimed** | Current design does not claim every secure-storage retrieval is hardware-bound to each biometric operation. |

## 4. Vault item types

All current item types are encrypted as `VaultItem` payloads.

| Item type | Persisted enum value | Status |
|---|---:|---|
| Login | 0 | **Implemented** |
| Secure Note | 1 | **Implemented** |
| Identity | 2 | **Implemented** |
| Payment Card Reference | 3 | **Implemented** |
| Wi-Fi Credential | 4 | **Implemented** |
| Software License | 5 | **Implemented** |
| Server/SSH Reference | 6 | **Implemented** |
| Document | 7 | **Implemented** |
| Custom | 8 | **Implemented** |
| Time-Based One-Time Password | 9 | **Implemented** |

Persisted numeric values are compatibility-sensitive and must not be reordered without an explicit migration/version plan.

## 5. Item fields and organization

| Capability | Status | Notes |
|---|---|---|
| Required title | **Implemented** | Maximum 256 characters. |
| Username/identifier | **Implemented** | Maximum 2,048 characters. |
| Primary secret | **Implemented** | General maximum 100,000 characters. |
| URL | **Implemented** | Maximum 4,096 characters. |
| Notes | **Implemented** | Secure-note renderer has stricter shared note bounds. |
| Collection | **Implemented** | Maximum 128 characters. |
| Tags | **Implemented** | Up to 100, each up to 128 characters. |
| Favorite | **Implemented** | Stored encrypted. |
| Review date | **Implemented** | Stored encrypted. |
| Recent-access timestamp | **Implemented** | Stored encrypted; opening an item updates it without changing `ModifiedUtc`. |
| Per-item re-authentication requirement | **Implemented** | Used to protect sensitive reveal/export operations. |
| Custom fields | **Implemented** | Up to 100; secret fields are handled separately in quick-copy UI. |
| Attachment references | **Implemented** | References remain inside encrypted item JSON. |
| Trash state | **Implemented** | Reversible until retention/permanent deletion. |

## 6. Search, filters, sorting, and reminders

| Capability | Status | Notes |
|---|---|---|
| Local decrypted search while unlocked | **Implemented** | Search query capped at 4,096 trimmed characters. |
| Plaintext persistent search index | **Not a product capability** | Deliberately avoided. |
| Collection filtering | **Implemented** | Operates over decrypted items. |
| Item-type filtering | **Implemented** | Operates over decrypted items. |
| Favorites filtering | **Implemented** | Operates over decrypted items. |
| Review-due filtering | **Implemented** | Operates over decrypted items. |
| Favorite/title ordering | **Implemented** | Local sort. |
| Recently used sort | **Implemented** | Uses encrypted `LastAccessedUtc`. |
| Recently modified sort | **Implemented** | Local sort. |
| Title sort | **Implemented** | Local sort. |
| Incremental 50-item rendering | **Implemented** | Limits visual-tree growth for large result sets. |
| Backup reminders | **Implemented** | Configurable local preference. |
| Review reminders | **Implemented** | Local reminder summary/lead-time preference. |

## 7. Local security audit

| Finding/capability | Status | Notes |
|---|---|---|
| Weak secret detection | **Implemented** | Local heuristic over decrypted items. |
| Reused secret detection | **Implemented** | TOTP seeds excluded from password reuse semantics. |
| Exact duplicate entry detection | **Implemented** | Includes TOTP parameters in duplicate semantics. |
| Missing-title finding | **Implemented** | Validation normally requires title; audit can still report malformed legacy/programmatic data. |
| Overdue review finding | **Implemented** | Uses encrypted review metadata after decryption. |
| Independent code security audit | **Not a product capability** | The in-app audit is not an independent professional source-code audit. |

## 8. TOTP

| Capability | Status | Notes |
|---|---|---|
| Encrypted Base32 TOTP seed storage | **Implemented** | Seed remains in authenticated encrypted item payload. |
| SHA-1 | **Implemented** | RFC-compatible known-answer coverage. |
| SHA-256 | **Implemented** | RFC-compatible known-answer coverage. |
| SHA-512 | **Implemented** | RFC-compatible known-answer coverage. |
| 6-digit codes | **Implemented** | Supported. |
| 8-digit codes | **Implemented** | Supported. |
| 15–120 second periods | **Implemented** | Default 30 seconds. |
| Manual code refresh | **Implemented** | No background refresh timer. |
| Explicit code copy | **Implemented** | Uses timed conditional clipboard cleanup. |
| Persist generated TOTP code | **Not a product capability** | Generated codes are transient presentation state. |
| QR scanning/rendering | **Deferred** | Requires separate parsing/camera/lifecycle/security design. |
| `otpauth://` import/export | **Deferred** | Requires bounded interoperability design. |
| Autofill/provider enrollment | **Deferred** | Separate platform/security project. |

## 9. Password/passphrase generation

| Capability | Status | Notes |
|---|---|---|
| Cryptographically random passwords | **Implemented** | Uses platform cryptographic RNG. |
| Uppercase/lowercase/digits/symbols | **Implemented** | User-configurable. |
| Ambiguous-character exclusion | **Implemented** | Configurable. |
| Password length | **Implemented** | Normalized to 8–256. |
| Memorable passphrase generation | **Implemented** | Validated local 256-word list. |
| Passphrase word count | **Implemented** | 6–16, default 8. |
| Generator defaults persistence | **Implemented** | Stored as non-secret settings. |
| Strength guidance | **Implemented** | Heuristic guidance, not cryptographic proof. |
| Pronounceable-password generator | **Deferred** | Requires reviewed design before implementation. |

## 10. Secure notes

| Capability | Status | Notes |
|---|---|---|
| Plain text paragraphs | **Implemented** | Safe renderer. |
| Headings | **Implemented** | Limited Markdown-like subset. |
| Bullets | **Implemented** | Limited subset. |
| Checklists | **Implemented** | Append/toggle helpers. |
| Fenced code | **Implemented** | Safe text rendering. |
| Raw HTML execution | **Not a product capability** | Angle brackets/raw HTML are neutralized. |
| 200,000-character limit | **Implemented** | Shared across storage/import/editor/renderer paths. |
| 5,000-line limit | **Implemented** | Shared across note paths. |

## 11. Attachments

| Capability | Status | Notes |
|---|---|---|
| Encrypted attachment storage | **Implemented** | Authenticated chunked `.cna` containers. |
| Streaming encryption/decryption | **Implemented** | Avoids whole-file plaintext materialization for normal operations. |
| 100 MiB plaintext file ceiling | **Implemented** | Defensive bound. |
| 25 attachments/item | **Implemented** | Defensive bound. |
| 10,000 global referenced attachments | **Implemented** | Aligns with backup entry budget. |
| Opaque GUID-based storage names | **Implemented** | Exact 36-character GUID-N + `.cna` shape. |
| Rune-aware metadata validation | **Implemented** | Rejects malformed UTF-16 and Unicode Control/Format runes. |
| Small UTF-8 text-family preview | **Implemented** | 512 KiB decrypted preview limit; 20,000 display-character limit. |
| Rich PDF/binary viewer | **Deferred** | Separate attack surface requiring review. |
| Document scanning | **Deferred** | Camera/platform/privacy design required. |
| Plaintext attachment export | **Implemented / platform-dependent** | Explicit warning + OS share flow + best-effort temp cleanup. |
| Guaranteed deletion of exported plaintext | **Not claimed** | OS/destination/cache/backups may retain copies. |

## 12. Encrypted backup and restore

| Capability | Status | Notes |
|---|---|---|
| `.cnbak` encrypted backup | **Implemented** | Preferred transfer/recovery path. |
| Separate backup passphrase | **Implemented** | Not the vault master API contract. |
| Consistent SQLite snapshot | **Implemented** | Vault locks before snapshot/export flow. |
| Include encrypted attachments | **Implemented** | Subject to global archive limits. |
| Versioned authenticated container | **Implemented** | Current backup format version 2 / magic `CNBK0002`. |
| Strict bounded backup-header JSON | **Implemented** | Exact schema/depth/type/duplicate rules before Argon2. |
| 1 GiB aggregate plaintext archive limit | **Implemented** | Defensive ceiling. |
| 10,001 ZIP entry maximum | **Implemented** | `vault.db` + attachment budget. |
| Duplicate normalized path rejection | **Implemented** | Restore rejects ambiguity/pathological entries. |
| Attachment container-size validation | **Implemented** | Rejects impossible encrypted attachment sizes. |
| Exact extracted-length validation | **Implemented** | Actual decompressed bytes must equal declared length. |
| Pre-swap replacement database validation | **Implemented** | Integrity/schema/header/ID/resource checks. |
| Cancellation-safe rollback after commit point | **Implemented** | Recovery token is not cancelled by caller after active mutation begins. |
| Clear biometric pairing after restore | **Implemented** | Prevents stale local secondary secret pairing. |
| Guaranteed recovery from lost backup passphrase | **Not a product capability** | CipherNest cannot decrypt without valid backup credentials. |

## 13. CSV transfer

| Capability | Status | Notes |
|---|---|---|
| Generic CSV header preview | **Implemented** | Bounded and control-safe. |
| Explicit column mapping | **Implemented** | Required for import. |
| CSV import | **Implemented** | Bounded streaming parser and validation. |
| Plaintext CSV export | **Implemented** | Requires explicit phrase, current-master auth, warning. |
| CSV attachment export | **Not a product capability** | Attachments are not included in plaintext CSV export. |
| TOTP `otpauth://` interoperability | **Deferred** | Generic CSV does not claim it. |
| Guaranteed removal of source/imported CSV | **Not a product capability** | Source file is outside CipherNest's storage boundary. |

## 14. Clipboard and plaintext handling

| Capability | Status | Notes |
|---|---|---|
| Explicit username copy | **Implemented** | User action required. |
| Explicit primary-secret copy | **Implemented** | User action required. |
| Explicit secret custom-field copy | **Implemented** | User action required. |
| Explicit TOTP code copy | **Implemented** | User action required. |
| Fingerprint-only delayed state | **Implemented** | SHA-256 fingerprint, not plaintext timer state. |
| Fixed-time matching | **Implemented** | Prevents ordinary variable-time fingerprint comparison. |
| Preserve newer unrelated clipboard content | **Implemented** | Clears only when the current clipboard still matches CipherNest's copy. |
| Clipboard-history deletion guarantee | **Not claimed** | OS history/sync/managers remain external. |

## 15. Settings and preferences

| Capability | Status | Default / range |
|---|---|---|
| Theme | **Implemented** | System / Light / Dark; default System. |
| Language | **Implemented** | System / English / Hindi; default System. |
| Lock timeout | **Implemented** | Default 60 s; 5–3,600 s. |
| Lock on background | **Implemented / platform-dependent** | Default enabled. |
| Clipboard clear delay | **Implemented / platform-dependent** | Default 30 s; 5–300 s. |
| Screenshot protection | **Implemented / platform-dependent** | Preference default enabled. |
| Biometric unlock | **Implemented / platform-dependent** | Default disabled. |
| Reduced motion | **Implemented** | Default disabled. |
| Larger interface | **Implemented** | Default disabled. |
| Trash retention | **Implemented** | Default 30 days; 1–365. |
| Master re-auth interval | **Implemented** | Default 24 h; 1–168. |
| Backup reminder | **Implemented** | Default 7 days; 1–365. |
| Review reminders | **Implemented** | Enabled by default. |
| Review reminder lead | **Implemented** | Default 7 days; 0–365. |
| Generator defaults | **Implemented** | Password length 20; passphrase word count 8. |
| Last successful backup timestamp | **Implemented** | Nullable non-secret preference. |
| Bounded JSON persistence | **Implemented** | 64 KiB, depth 16, bounded actual read. |
| Safe fallback for malformed settings | **Implemented** | Falls back to normalized defaults; cancellation still propagates. |

## 16. Privacy and diagnostics

| Capability | Status | Notes |
|---|---|---|
| Third-party analytics | **Not enabled** | Current source does not enable analytics service. |
| Third-party crash reporting | **Not enabled** | Current source does not enable a crash-reporting provider. |
| Privacy-safe internal exception reporter | **Implemented** | Stable operation ID, type, HResult, severity, fixed text. |
| Log raw exception message/stack | **Not a product capability** | Intentionally omitted from privacy-safe reporter. |
| Log decrypted vault content | **Not a product capability** | Explicitly prohibited by design/docs. |
| Developer diagnostics surface | **Implemented** | Redacted/best-effort temp cleanup. |

## 17. Accessibility and localization

| Capability | Status | Notes |
|---|---|---|
| Semantic labels/descriptions | **Implemented / release validation required** | Source coverage exists; assistive-technology validation remains required. |
| Live-region/status metadata | **Implemented / release validation required** | Runtime behavior requires platform testing. |
| Larger-interface typography | **Implemented** | Dynamic resources/preference. |
| Reduced-motion preference state | **Implemented** | Runtime visual review required. |
| Responsive phone/desktop layouts | **Implemented / release validation required** | Narrow/resizable-device testing remains required. |
| System/Light/Dark theme | **Implemented** | Runtime contrast/readability still needs target review. |
| Neutral English fallback | **Implemented** | Canonical fallback resources. |
| Reviewed Hindi `hi-IN` resource catalog | **Implemented** | Covers the resource-backed interface. |
| Every remaining literal translated | **Deferred** | Full UI translation is not claimed. |
| Additional languages | **Deferred** | Require reviewed catalogs. |

## 18. UI/application surfaces

| Page/surface | Status | Purpose |
|---|---|---|
| Startup | **Implemented** | Determines onboarding vs unlock. |
| Onboarding | **Implemented** | Creates vault/master/recovery. |
| Unlock | **Implemented** | Master/recovery/optional biometric convenience unlock. |
| Vault | **Implemented** | Search/filter/sort/list, navigation, lock, BMC support entry. |
| Item Editor | **Implemented** | Create/edit items, TOTP, notes, custom fields, attachments, re-auth actions. |
| Generator | **Implemented** | Password/passphrase generation. |
| Generator Defaults | **Implemented** | Persistent generator defaults. |
| Audit | **Implemented** | Local security findings. |
| Trash | **Implemented** | Restore/permanent delete/empty trash. |
| Settings | **Implemented** | Security/privacy/backup/appearance/language/storage/support controls. |
| Security Info | **Implemented** | Local security/privacy/threat-limit disclosure. |
| Transfer | **Implemented** | CSV import/export surface. |
| About | **Implemented** | Version/license/privacy/terms/repository/support/BMC/audit status. |
| Developer | **Implemented** | Redacted developer diagnostics/information. |

See [`UI_REFERENCE.md`](UI_REFERENCE.md) for route and interaction details.

## 19. Branding and project support

| Capability | Status | Notes |
|---|---|---|
| Primary vector app icon sources | **Implemented** | MAUI icon sources committed. |
| Splash screen | **Implemented** | Original vector splash/wordmark. |
| `Made by the Sanskar` creator credit | **Implemented** | Project branding metadata/splash documentation. |
| Dark-surface logo variant | **Implemented** | Branding asset. |
| Monochrome/adaptive source | **Implemented** | Platform packaging source asset. |
| BMC support SVG | **Implemented** | Original support presentation asset. |
| BMC README surface | **Implemented** | Prominent project-support presentation. |
| BMC Support.md surface | **Implemented** | Present. |
| BMC About surface | **Implemented** | User-initiated. |
| BMC Settings surface | **Implemented** | Full support card. |
| BMC Vault entry | **Implemented** | Compact support entry. |
| `.github/FUNDING.yml` | **Implemented** | Repository funding metadata. |
| Funding-disabled app build | **Implemented** | `CipherNestEnableFundingLink=false`. |
| Funding changes feature/security priority | **Not a product capability** | Funding is voluntary and does not alter product rights/treatment. |

## 20. Build and continuous integration

| Capability | Status | Notes |
|---|---|---|
| .NET 10 MAUI | **Implemented** | Current target family. |
| Central package management | **Implemented** | `Directory.Packages.props`. |
| Nullable analysis | **Implemented** | Shared build policy. |
| Warnings as errors | **Implemented** | Shared build policy. |
| Analyzers/code style | **Implemented** | Shared build policy. |
| Deterministic managed compilation | **Implemented** | Shared build policy. |
| Core PowerShell verification script | **Implemented** | `scripts/verify-core.ps1`. |
| Core POSIX verification script | **Implemented** | `scripts/verify-core.sh`. |
| Windows verification script | **Implemented** | `scripts/verify-windows.ps1`. |
| Android verification script | **Implemented** | `scripts/verify-android.sh`. |
| Apple verification script | **Implemented** | `scripts/verify-apple.sh`. |
| Windows hosted compile | **Implemented / verified baseline** | Includes funding-enabled + disabled variants. |
| Android hosted compile | **Implemented / verified baseline** | Release compile. |
| iOS simulator hosted compile | **Implemented / verified baseline** | Release compile. |
| Mac Catalyst hosted compile | **Implemented / verified baseline** | Release compile. |
| CodeQL v4 | **Implemented / verified baseline** | Builds analyzable core + MAUI application path. |
| Pull-request dependency review | **Implemented** | High-severity threshold. |

## 21. Pre-documentation verified implementation baseline

The implementation head immediately before the complete-documentation expansion was:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

Recorded results:

| Gate | Result |
|---|---|
| Unit tests | 346 passed |
| Integration tests | 98 passed |
| UI/source tests | 111 passed |
| Total tests | **555 passed, 0 failed, 0 skipped** |
| Analyzer builds | Passed with zero build warnings/errors in core test builds |
| Core formatting | Passed |
| Windows Release | Passed |
| Windows funding-disabled Release | Passed |
| Android Release | Passed |
| iOS simulator Release | Passed |
| Mac Catalyst Release | Passed |
| CodeQL v4 | Passed |

CI run: `31937127961`  
CodeQL run: `31937127900`

Later commits require their own exact-head verification before inheriting release-candidate status.

## 22. External release gates

These are not hidden product features; they are evidence still required for an actual release:

- physical Android biometric enrollment/denial/cancellation/lockout/secure-storage matrix;
- physical/simulator iOS Face ID/Touch ID and secure-storage behavior;
- Mac Catalyst biometric/runtime behavior;
- Windows/iOS/macOS/Android clipboard history/cleanup behavior;
- background/sleep/resume lifecycle behavior;
- screenshot/app-switcher privacy behavior;
- share-sheet temporary plaintext retention/cleanup behavior;
- TalkBack/VoiceOver/Narrator/keyboard/focus/large-text accessibility testing;
- representative narrow/large/resizable layout validation;
- stress/interleaving validation around sessions, attachments, restore, and recovery;
- historical migration/backup compatibility validation;
- exact dependency/advisory/license review for the release package graph;
- signing/provisioning/notarization;
- store privacy declarations and store review;
- target store/region policy review for external BMC/funding CTA;
- independent professional cryptographic/security review.

## 23. Deliberately deferred future-version features

The current source does **not** claim completed support for:

- CipherNest cloud synchronization;
- user accounts;
- collaboration/shared vaults;
- server-side vault storage;
- multi-device conflict resolution;
- browser/app autofill;
- Windows Hello convenience unlock;
- TOTP QR scanning/rendering;
- bounded `otpauth://` import/export;
- TOTP provider/autofill enrollment;
- rich binary/PDF preview beyond bounded safe text preview;
- document scanning;
- pronounceable-password mode;
- destructive automatic wipe after failed unlock attempts;
- complete translation of every remaining literal into Hindi;
- additional complete language catalogs.

Deferred items must remain visibly deferred until source, tests, threat/privacy review, documentation, and target validation support a stronger claim.
