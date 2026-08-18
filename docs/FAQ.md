# CipherNest FAQ

This FAQ answers common user, contributor, security, build, and release questions for the current CipherNest source tree. For deeper detail, use the canonical documents linked throughout this file.

Quick references: [`QUICK_START.md`](QUICK_START.md) · [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md) · [`UI_REFERENCE.md`](UI_REFERENCE.md) · [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md) · [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md)

## General

### What is CipherNest?

CipherNest is a local-first open-source encrypted vault built with C#, .NET 10, and .NET MAUI. It is designed for passwords, credentials, identities, secure notes, TOTP, custom secrets, encrypted attachments, collections, tags, reminders, and related encrypted vault metadata.

### Does CipherNest require an account?

No application account, email address, phone number, or CipherNest-hosted server is required for ordinary use in the current source.

### Does CipherNest synchronize through the cloud?

No. Cloud synchronization is not part of the current release. It is deliberately deferred because synchronization would introduce account, protocol, conflict-resolution, metadata, device-trust, and threat-model requirements that need separate design and review.

### Is CipherNest open source?

Yes. The project is licensed under GPL-3.0-or-later. See `LICENSE`.

### Where is the source code?

`https://github.com/sanskarIN/CipherNest`

### Where should a new user start?

Start with [`QUICK_START.md`](QUICK_START.md), then use [`USER_GUIDE.md`](USER_GUIDE.md) for detailed workflows.

### Where should a developer start?

Read [`DEVELOPER_GUIDE.md`](DEVELOPER_GUIDE.md), [`architecture/ARCHITECTURE.md`](architecture/ARCHITECTURE.md), [`security/THREAT_MODEL.md`](security/THREAT_MODEL.md), and [`security/CRYPTOGRAPHIC_DESIGN.md`](security/CRYPTOGRAPHIC_DESIGN.md).

## Security

### Has CipherNest completed an independent professional security audit?

No. CipherNest has **not** completed an independent professional security audit. Passing automated tests, hosted compilation, and CodeQL does not replace independent cryptographic/security review or target-device validation.

### Is CipherNest unhackable, military-grade, or 100% secure?

No. Those are unsupported absolute claims. CipherNest intentionally documents its threat model, limitations, managed-memory constraints, plaintext boundaries, platform dependencies, and remaining release gates.

### What encryption does CipherNest use?

The current design uses AES-256-GCM authenticated encryption and Argon2id for passphrase-based key derivation/wrapping. A random 256-bit vault data-encryption key protects records/attachments. See [`security/CRYPTOGRAPHIC_DESIGN.md`](security/CRYPTOGRAPHIC_DESIGN.md).

### Is the master passphrase stored?

No. The current design does not store the master passphrase. Argon2id derives a wrapping key used to unwrap the random vault DEK.

### Can CipherNest recover a forgotten master passphrase from a server?

No. CipherNest does not operate a password-reset server for the local vault. Optional recovery material is an independent local wrapped-key path and must be stored separately by the user.

### Does the recovery key authorize everything the master passphrase can?

No. Current authorization distinguishes recovery/unlock from actions that specifically require current-master re-authentication.

### What happens when I lock the vault?

The current lock transition removes/zeroes the shared session-key buffer where practical, cancels the current unlock-session cancellation source, and invalidates session-linked key leases. Cancellable key-using operations should stop rather than intentionally continue with stale authorization.

### Does CipherNest perfectly wipe all secrets from RAM?

No. Owned byte arrays and sensitive ViewModel properties are cleared where practical, but .NET managed strings, runtime copies, OS components, clipboard history, share targets, storage media, or other applications may retain copies CipherNest cannot deterministically erase.

### Does locking clear the clipboard?

CipherNest attempts conditional cleanup for CipherNest-copied secret values where the platform permits it. Delayed state keeps a SHA-256 fingerprint rather than the copied plaintext and avoids clearing unrelated newer clipboard content. OS clipboard history/sync and third-party clipboard managers remain outside CipherNest's guarantee.

### Can screenshots always be blocked?

No. Screenshot/task-preview protections are platform-dependent and require target-specific validation. Unsupported paths must not claim protection the OS does not provide.

### Does passing CodeQL mean the app is secure?

No. CodeQL is a useful static-analysis gate, not an independent security audit or proof that every runtime/platform threat is eliminated.

## Biometrics

### Which platforms include biometric convenience unlock source?

Android, iOS, and Mac Catalyst. Windows currently falls back to master-passphrase unlock.

### Does biometric unlock replace the master passphrase?

No. Biometrics are optional convenience authorization. A fresh process requires master-auth state before convenience unlock can later be used, configured intervals require the master again, and sensitive actions can require current-master re-authentication.

### Is the master passphrase stored in secure storage for biometrics?

No. CipherNest creates a separate random secondary secret and protects it using platform secure storage. That secondary secret protects an independent wrapper for the same vault DEK.

### What happens to biometric unlock after backup restore?

Successful restore clears the local biometric pairing/convenience state so restored vault metadata is not silently paired with stale local secure-storage material.

### What happens after changing the master passphrase?

Master-passphrase rotation ends the current security session and requires the new master passphrase before biometric convenience unlock can resume.

### Does CipherNest claim every biometric secret retrieval is hardware-bound to the prompt?

No. The current design does not claim hardware-backed cryptographic binding of every secure-storage retrieval to each biometric operation.

## Vault data and storage

### Is vault data stored in plaintext SQLite columns?

Vault item contents are encrypted. CipherNest deliberately avoids plaintext searchable indexes for decrypted vault fields. Minimized structural metadata still exists as required to operate the local database.

### What is the SQLite schema?

Schema version 1 contains `VaultHeader`, `VaultItems`, `AppSettings`, and `MigrationHistory`. See [`architecture/DATABASE.md`](architecture/DATABASE.md).

### Does CipherNest impose data limits?

Yes. Limits exist to bound memory, CPU, storage, archive, parser, and UI work. See [`LIMITS_AND_DEFAULTS.md`](LIMITS_AND_DEFAULTS.md).

### Why are limits needed if encryption is correct?

Cryptographic correctness does not prevent resource-exhaustion attacks or accidental denial of service. Bounded parsing/storage validation prevents malformed data from requesting arbitrary work.

### Is there a plaintext full-text-search database?

No. Search/filter/audit operates over decrypted authenticated items only while the vault is unlocked.

## Vault items

### Which item types are supported?

Login, Secure Note, Identity, Payment Card Reference, Wi-Fi Credential, Software License, Server/SSH Reference, Document, Custom, and Time-Based One-Time Password (TOTP).

### What organizational features exist?

Collections/folders, tags, favorites, review dates, recent-access timestamps, search, type filters, review-due filters, sorting, and incremental visual paging.

### Can an item require another master-passphrase check?

Yes. Items can require per-item current-master re-authentication before their decrypted fields are displayed/modified in the protected flow.

## TOTP

### Is TOTP implemented?

Yes. Local RFC-compatible TOTP generation and bounded single-item `otpauth://totp/...` setup-URI interoperability are implemented.

### Where is the TOTP seed stored?

For a TOTP item, the Base32 seed is kept inside the authenticated encrypted `VaultItem` record. A setup URI is not persisted as a second vault field.

### Which algorithms are supported?

SHA-1, SHA-256, and SHA-512.

### Which code lengths are supported?

6 or 8 digits.

### Which periods are supported?

15–120 seconds, with 30 seconds as the default.

### Are generated codes saved into the vault?

No. Generated TOTP codes are transient presentation state and are not persisted as vault fields.

### Does CipherNest refresh TOTP automatically in the background?

No. The current UI uses explicit refresh/copy actions rather than a background refresh timer.

### Is `otpauth://` import/export implemented?

Yes, for bounded TOTP-only setup URIs. The Item Editor can import an `otpauth://totp/...` value locally and can format/copy the current TOTP item as a canonical setup URI. The parser applies URI/query/display limits, rejects duplicate query keys, checks issuer consistency, and reuses the normal TOTP seed/settings validation.

### Is HOTP supported through setup URIs?

No. `otpauth://hotp/...` and `counter` input are deliberately rejected rather than silently converted to TOTP.

### Does setup-URI import contact the provider?

No. Parsing is local. CipherNest does not verify that a server-side enrollment exists or that the displayed issuer/account is trustworthy. Review imported metadata before saving.

### Is a copied setup URI as safe as a copied one-time code?

No. A normal setup URI contains the long-lived seed and can therefore enable future code generation. CipherNest uses its timed secret-clipboard path, but OS clipboard history/synchronization and other applications remain outside guaranteed cleanup.

### Is QR scanning/rendering implemented?

No. Camera/QR scanning and QR rendering remain deferred, as do automatic provider/autofill enrollment surfaces.

### Does putting a password and TOTP seed in one vault preserve cryptographic factor separation?

Not necessarily. If both are stored in one unlocked vault, compromise of that vault can expose both. See [`security/TOTP.md`](security/TOTP.md).

## Attachments

### Can I store attachments?

Yes. Attachments are authenticated encrypted streaming `.cna` containers with opaque GUID-based storage names.

### What is the maximum attachment size?

100 MiB plaintext per attachment under the current policy.

### How many attachments can an item have?

25 per item. The global referenced-attachment budget is 10,000.

### Can CipherNest preview every file type?

No. Small supported UTF-8 text-family files can be previewed in bounded memory. Rich binary/PDF preview and scanning remain deferred.

### Is attachment export encrypted?

No. Explicit attachment export produces plaintext for the OS share flow. CipherNest warns first and attempts temporary staging cleanup afterward, but the OS/destination may retain copies.

### Can CipherNest guarantee exported plaintext is erased?

No. It cannot guarantee deletion from OS caches, share providers, destination apps, snapshots, backups, indexers, or physical storage remnants.

## Secure notes and generators

### Does Secure Note render arbitrary HTML?

No. CipherNest uses a deliberately small Markdown-like subset and neutralizes raw HTML rather than rendering active content.

### What are the Secure Note limits?

200,000 characters and 5,000 lines under the shared note policy.

### How are passwords generated?

Password generation uses cryptographically secure randomness with configurable character groups and ambiguous-character exclusion.

### How are memorable passphrases generated?

The current implementation uses a validated local list of exactly 256 unique lowercase words, cryptographically secure random selection, 6–16 word bounds, and an eight-word default.

### Is pronounceable-password mode implemented?

No. It remains deferred until a reviewed design exists.

## Backup and recovery

### What is the recommended transfer/recovery mechanism?

Use the authenticated encrypted `.cnbak` backup/restore path. CSV exists for plaintext interoperability, not as the preferred secure backup mechanism.

### Are attachments included in encrypted backup?

Yes. Current encrypted backup includes the database snapshot and encrypted attachment containers subject to archive/resource ceilings.

### Does backup use the vault master passphrase automatically?

No. Backup uses a separate backup passphrase through the backup API design.

### What happens if a backup is corrupted?

Restore validates header/framing/KDF bounds, encrypted chunks, archive paths/counts/sizes, actual extracted lengths, SQLite integrity/schema, vault header, item IDs, and storage budgets before active replacement. Invalid/corrupt input should fail instead of silently becoming active.

### Can a cancelled restore leave the active vault half replaced?

The source includes staging/recovery behavior. Once active mutation begins, rollback uses an uncancelled recovery token so caller cancellation does not cancel required recovery work. Broader device/filesystem stress validation remains a release gate.

### Should I test backups?

Yes. Periodically test restore using safe disposable/controlled data. See [`operations/BACKUP_RECOVERY_RUNBOOK.md`](operations/BACKUP_RECOVERY_RUNBOOK.md).

## CSV import/export

### Can CipherNest import CSV?

Yes. Import uses bounded parsing and explicit user mapping. CipherNest does not silently guess arbitrary secret columns.

### Is CSV export encrypted?

No. It is deliberately plaintext interoperability.

### What does plaintext export require?

The current UI requires the exact confirmation phrase `EXPORT PLAINTEXT`, current-master re-authentication, and a separate warning/confirmation.

### Does plaintext CSV export include attachments?

No.

### Is TOTP setup-URI interoperability part of CSV?

No. It is a dedicated single-item Item Editor path. Generic CSV remains generic plaintext interoperability rather than an authenticator migration format.

### Does importing a CSV delete or encrypt the original external CSV?

No. The source file remains outside CipherNest and must be handled separately by the user.

## Trash and deletion

### Can deleted items be restored?

Items moved to Trash can be restored until permanent deletion or retention cleanup.

### What is the default trash retention?

30 days. The normalized range is 1–365 days.

### Does permanent deletion require authentication?

Manual permanent deletion and Empty Trash require current-master re-authentication plus destructive confirmation.

### What does full-vault deletion require?

The exact phrase `DELETE MY VAULT`, current-master authentication, and final confirmation. Authorization remains tied to the live session while waiting for the destructive transition.

### Does deletion guarantee forensic erasure?

No. CipherNest performs logical application-managed deletion. Filesystems, SSD wear leveling, backups, snapshots, caches, and physical media may retain remnants outside application control.

## Clipboard and plaintext lifecycle

### Which values have explicit copy actions?

Username, primary secret, secret custom fields, TOTP codes, and TOTP setup URIs.

### Does the timer retain the copied plaintext?

The delayed cleanup state retains a SHA-256 fingerprint rather than the copied plaintext value.

### Why does CipherNest not always clear whatever is on the clipboard later?

It conditionally clears only when the current clipboard still matches the value CipherNest previously copied, which avoids erasing unrelated newer clipboard content.

## Settings

### Which settings are available?

Theme, System/English/Hindi language preference, lock timeout, background lock, clipboard delay, screenshot preference, biometric convenience unlock, master re-auth interval, reduced motion, larger interface, trash retention, backup reminders, review reminders, generator defaults, storage/cache controls, backup/restore, CSV transfer, security information, About/legal, master-passphrase change, and full-vault deletion.

### Are settings encrypted secret storage?

No. `AppPreferences` is non-secret local configuration.

### What happens if the settings JSON is malformed?

Malformed, invalid UTF-8, over-depth, or oversized local non-secret settings fall back to normalized defaults under the current bounded settings policy. Cancellation is not swallowed into that fallback.

## Platforms

### Does CipherNest support Android?

The MAUI project targets Android. The immutable pre-documentation implementation baseline compiled Android Release successfully. Physical-device behavior remains a release gate.

### Does CipherNest support iOS?

The project targets iOS. The current pre-documentation implementation baseline compiled the iOS simulator Release target successfully. Signing, device behavior, secure storage, biometrics, lifecycle, and store validation remain external gates.

### Does CipherNest support macOS?

The current desktop Apple target is Mac Catalyst. The pre-documentation baseline compiled Mac Catalyst Release successfully; notarization/package/device behavior remains release work.

### Does CipherNest support Windows?

Yes. The pre-documentation baseline compiled both default Windows Release and the funding-disabled Windows Release configuration successfully.

### Does CipherNest support Linux?

There is no shipping Linux MAUI application target in the current solution.

## Accessibility and localization

### Does CipherNest include accessibility support?

Source includes semantic metadata, larger-interface typography, reduced-motion preference state, responsive layouts, and theme support. This is not accessibility certification; TalkBack, VoiceOver, Narrator, keyboard/focus, scaling, contrast, and representative layout testing remain release gates.

### Which languages are included?

The current preference model supports **System, English, and Hindi**. Neutral English is the fallback. A reviewed `hi-IN` resource-backed catalog is implemented for the migrated interface. CipherNest does **not** claim that every remaining literal has been translated; some non-resource UI text can still appear in English.

### Is full Hindi translation complete?

No. The reviewed resource-backed Hindi catalog is implemented, but complete migration/review of every remaining UI literal is still deferred.

## Privacy and diagnostics

### Does CipherNest enable third-party analytics?

No third-party analytics service is enabled in current source.

### Does CipherNest enable third-party crash reporting?

No third-party crash-reporting provider is enabled in current source.

### What does the privacy-safe reporter record?

Sanitized operation identifiers, exception type, HResult, severity, and fixed omission text. It intentionally omits raw exception messages/stacks and decrypted vault content.

### Can diagnostics contain a TOTP seed or setup URI?

They must not. TOTP seeds, generated codes, and setup URIs are sensitive and must not be emitted through diagnostics or copied into support artifacts.

## Build and development

### What SDK does CipherNest use?

The repository uses .NET 10 / .NET MAUI. `global.json` requests SDK `10.0.100` with `latestFeature` roll-forward and no prerelease SDK selection. See [`setup/BUILD.md`](setup/BUILD.md).

### How do I run core verification?

Use `scripts/verify-core.ps1` or `scripts/verify-core.sh`. Platform scripts also exist for Windows, Android, and Apple.

### Can I disable the in-app Buy Me a Coffee link?

Yes:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

The exact store/region policy determines the release setting.

### Does disabling the in-app funding CTA remove repository funding metadata?

No. The application build switch is separate from `.github/FUNDING.yml`.

## Buy Me a Coffee support

### Where is BMC highlighted?

Current surfaces include the root README, `SUPPORT.md`, `.github/FUNDING.yml`, About, the Settings BMC card, and the Vault `☕ Support` entry when funding UI is enabled.

### Does BMC support change product access or priority?

No. Financial support is voluntary and does not change feature access, security/privacy treatment, support priority, licensing, recovery behavior, or open-source rights.

## Testing and CI

### What automated test projects exist?

- `CipherNest.UnitTests`
- `CipherNest.IntegrationTests`
- `CipherNest.UiTests`

They cover cryptography, validation, persistence, session policy, backup/restore, attachments, CSV, settings, lifecycle, TOTP code generation/setup-URI interoperability, generator, documentation, branding, support metadata, build workflows, and other current invariants.

### What is the current immutable pre-documentation implementation baseline?

The complete-documentation work is grounded in exact implementation commit:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

For that exact SHA:

- 346 UnitTests passed;
- 98 IntegrationTests passed;
- 111 UI/source tests passed;
- **555 total passed, 0 failed, 0 skipped**;
- core analyzer builds passed with zero build warnings/errors;
- core formatting passed;
- Windows default Release passed;
- Windows funding-disabled Release passed;
- Android Release passed;
- iOS simulator Release passed;
- Mac Catalyst Release passed;
- CodeQL v4 passed.

Runs:

- CipherNest CI `31937127961`
- CodeQL `31937127900`

That baseline remains immutable historical evidence for its exact SHA. The August 18 setup-URI continuation is a newer exact head and must pass its own configured gates before receiving an exact-head verified release-candidate claim.

### What about the older 240-test and 554-test documents?

They remain valid historical evidence for their original exact SHAs. Historical verification files are intentionally preserved rather than rewritten to pretend they describe later commits.

### Why can CI be green and the project still not be release-ready?

CI cannot fully prove physical-device biometrics, secure storage, clipboard/history, lifecycle timing, screenshots, representative third-party TOTP setup-URI compatibility, assistive-technology behavior, package signing, notarization, store review, release dependency/license state, or an independent professional security audit.

## Release and distribution

### Where is the release process documented?

Use [`releases/RELEASE_PROCESS.md`](releases/RELEASE_PROCESS.md) and [`RELEASE_CHECKLIST.md`](RELEASE_CHECKLIST.md).

### Are signing keys stored in the repository?

They should not be. Signing identities, private keys, passwords, store API keys, certificates, and similar secrets must remain outside Git history.

### Can I publish directly from a successful developer build?

No. A release candidate needs exact CI evidence, device/simulator validation, dependency/security/license review, packaging/signing, accessibility/localization checks, store-policy checks, documentation freeze, and provenance.

## Support and reporting

### How do I report a normal bug?

Use repository issue guidance only when the report does not expose sensitive data. Include synthetic reproduction steps, app/commit/platform information, and fixed/redacted error text.

### How do I report a security problem?

Follow `SECURITY.md` and [`operations/SECURITY_RESPONSE.md`](operations/SECURITY_RESPONSE.md).

### What should support never request?

Support should not request master/backup passphrases, recovery material, real vault contents, TOTP seeds/setup URIs, decrypted backups, private keys, secondary secrets, or secret-bearing diagnostics.

### What are the support contacts?

Business: `sanskarin@outlook.in`  
Support: `supportramsandesh@gmail.com`

## Roadmap

### What is still planned?

See [`NEXT_STEPS.md`](NEXT_STEPS.md) and [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md). Major deferred areas include cloud/accounts/collaboration, autofill, Windows Hello, TOTP QR/camera and provider enrollment, HOTP interoperability, rich PDF/binary preview/scanning, pronounceable-password mode, destructive wipe-on-failure, and complete translation of remaining literals/additional language catalogs.

### Where can I see current source/evidence status?

Use `PROJECT_STATUS.md`, `CHANGELOG.md`, `what_changed.md`, and `docs/verification/`.
