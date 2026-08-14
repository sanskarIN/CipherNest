# CipherNest FAQ

This FAQ answers common user, contributor, security, build, and release questions for the current CipherNest source tree. For security-sensitive implementation detail, use the linked canonical documents rather than relying only on this summary.

## General

### What is CipherNest?

CipherNest is a local-first open-source encrypted vault built with .NET MAUI and C#. It is designed for passwords, credentials, identities, secure notes, custom secrets, encrypted attachments, collections, tags, reminders, and related vault metadata.

### Does CipherNest require an account?

No application account, email address, phone number, or CipherNest-hosted server is required for ordinary use in the current source.

### Does CipherNest synchronize through the cloud?

No. Cloud synchronization is not part of the current release. It is deliberately deferred because synchronization would introduce account, protocol, conflict-resolution, metadata, device-trust, and threat-model requirements that need separate design and review.

### Is CipherNest open source?

Yes. The project is licensed under GPL-3.0-or-later. See `LICENSE`.

### Where is the source code?

The repository is `https://github.com/sanskarIN/CipherNest`.

## Security

### Has CipherNest completed an independent professional security audit?

No. CipherNest has **not** completed an independent professional security audit. Passing automated tests, CodeQL, and hosted compilation does not replace independent cryptographic/security review or target-device testing.

### Is CipherNest unhackable or 100% secure?

No software should be represented that way. CipherNest intentionally documents its threat model, limitations, managed-memory constraints, platform dependencies, and remaining external validation gates.

### Is my master passphrase stored?

The current design does not store the master passphrase. It derives a wrapping key using Argon2id and uses that to unwrap a randomly generated vault data-encryption key.

### What encryption does CipherNest use?

The current design uses AES-256-GCM authenticated encryption for security-sensitive encrypted objects and Argon2id for passphrase-based key derivation/wrapping. See `docs/security/CRYPTOGRAPHIC_DESIGN.md`.

### Can CipherNest recover a forgotten master passphrase from a server?

No. CipherNest does not operate a password-reset server for the local vault. Optional recovery material provides an independent local wrapped-key path and must be stored separately by the user.

### Does the recovery key authorize everything the master passphrase can?

No. Current authorization boundaries distinguish vault recovery/unlock from operations that specifically require current-master re-authentication.

### Does CipherNest wipe all secrets perfectly from RAM?

No. CipherNest clears owned arrays and sensitive ViewModel properties where practical, but .NET managed strings, runtime copies, operating-system components, clipboard history, share destinations, storage media, or other applications may retain copies that CipherNest cannot deterministically erase.

### What happens when I lock the vault?

The current session transition removes and zeroes the shared session key, cancels the unlock-session cancellation source, and invalidates key leases tied to that session. Cancellable key-using operations are expected to stop rather than intentionally continue with a stale session.

### Does locking clear the clipboard?

CipherNest attempts conditional cleanup for CipherNest-copied secret values where the platform permits it. Cleanup is best-effort. The delayed state keeps a SHA-256 fingerprint rather than the copied plaintext and avoids clearing unrelated newer clipboard content.

### Can screenshots always be blocked?

No. Screenshot/task-preview protections are platform-dependent and require target-specific validation. Unsupported paths must not claim protection that the operating system does not provide.

## Biometrics

### Which platforms support biometric convenience unlock?

The current source includes convenience-unlock implementations for Android, iOS, and Mac Catalyst. Windows currently falls back to master-passphrase unlock.

### Does biometric unlock replace the master passphrase permanently?

No. A fresh process and configured re-authentication intervals require the master passphrase, and sensitive actions can require current-master re-authentication.

### What happens to biometric unlock after a backup restore?

Restore invalidates local biometric pairing/convenience state according to the current implementation, so restored vault material is not silently trusted by a previous local biometric pairing.

### What happens after changing the master passphrase?

Master-passphrase rotation ends the current security session and requires the new master passphrase before biometric convenience unlock can resume.

## Data and storage

### Is vault data stored in plaintext SQLite columns?

Vault record contents are encrypted. CipherNest deliberately avoids plaintext searchable indexes for decrypted vault fields. Minimized structural metadata still exists as required to operate the database.

### Does CipherNest impose data limits?

Yes. Limits exist to prevent hostile or accidental unbounded work. Examples include record-size, aggregate-vault, attachment-count, note-size, search-query, settings-file, backup-size, ZIP-entry, and passphrase-input ceilings. See `docs/LIMITS_AND_DEFAULTS.md`.

### Why are there limits if encryption is correct?

Cryptographic correctness does not prevent resource-exhaustion attacks or accidental denial of service. Bounded parsing and storage validation reduce the chance that malformed data causes excessive CPU, memory, disk, archive, or UI work.

### Can I store attachments?

Yes. Attachments are encrypted using bounded streaming containers and opaque GUID-based `.cna` storage names. See `docs/formats/ATTACHMENTS.md`.

### Can CipherNest preview every file type?

No. Small supported UTF-8 text-family files can be previewed in bounded memory. Other formats require explicit plaintext export. Rich binary/PDF preview and scanning are deferred future work.

## Backup and recovery

### What is the recommended way to move a vault between devices?

Use the encrypted `.cnbak` backup/restore path when supported by your workflow. CSV exists for interoperability and is a plaintext boundary.

### Are attachments included in encrypted backup?

Yes. The current encrypted backup format includes the encrypted database and encrypted attachment containers subject to archive/resource limits.

### What happens if a backup is corrupted?

The restore path validates framing, KDF/header metadata, encrypted chunks, archive contents, database integrity/schema, required objects, IDs, and storage budgets before active replacement. Corrupt or unsupported input should fail rather than silently become the active vault.

### Can a cancelled restore leave the active vault half replaced?

The source includes recovery staging and rollback behavior. Once active mutation begins, rollback uses a recovery token that is not cancelled simply because the initiating request was cancelled. Device/filesystem stress validation is still a release gate.

### Should I test my backups?

Yes. A backup strategy should be tested periodically using safe disposable or controlled data. Use `docs/operations/BACKUP_RECOVERY_RUNBOOK.md`.

## CSV import/export

### Can CipherNest import CSV files?

Yes. Import uses explicit column mapping and bounded parsing. CipherNest should not guess which column contains a secret without explicit mapping.

### Is CSV export encrypted?

No. CSV export is intentionally plaintext for interoperability and therefore leaves the protected vault boundary.

### Does plaintext CSV export include attachments?

No. Attachments are not silently embedded into CSV export.

### Can CipherNest guarantee exported plaintext disappears from the device?

No. CipherNest can delete application-managed temporary staging where permitted, but operating systems, backups, file providers, share targets, history, or third-party applications may retain copies.

## Secure notes and generators

### Does Secure Notes render arbitrary HTML?

No. CipherNest uses a deliberately small Markdown-like safe subset and neutralizes raw HTML rather than rendering arbitrary active content.

### How are passwords generated?

Password generation uses cryptographically secure randomness with configurable character groups and ambiguous-character exclusion.

### How are memorable passphrases generated?

The current implementation uses a validated local 256-word lowercase list, cryptographically secure random selection, 6–16 word bounds, and an eight-word default. See `docs/security/PASSPHRASE_GENERATOR.md`.

## Trash and deletion

### Can deleted items be restored?

Items moved to trash can be restored before permanent deletion/retention cleanup.

### Does permanent deletion require authentication?

Manual permanent deletion and Empty Trash require current-master re-authentication plus destructive confirmation in the current source.

### Does deleting the vault guarantee forensic erasure from storage media?

No. CipherNest attempts logical deletion of application-managed database, sidecar, recovery, and encrypted attachment artifacts. Filesystems, SSD wear leveling, backups, snapshots, operating-system caches, or physical media may retain remnants outside the application's control.

## Platforms

### Does CipherNest support Android?

The MAUI project targets Android and hosted CI has compiled the recorded Android Release path. Physical-device validation remains required before release claims.

### Does CipherNest support iOS?

The MAUI project targets iOS and hosted CI has compiled the recorded simulator Release path. Signing, device behavior, secure storage, biometrics, lifecycle, and store validation remain external gates.

### Does CipherNest support macOS?

The current MAUI desktop target is Mac Catalyst. Hosted CI has compiled the recorded Mac Catalyst Release path, but packaging/notarization and real-device validation remain release work.

### Does CipherNest support Windows?

Yes, the MAUI project targets Windows and the recorded hosted baseline compiled both default and funding-disabled Release configurations.

### Does CipherNest support Linux?

There is no shipping Linux MAUI application target in the current solution.

## Accessibility and localization

### Does CipherNest include accessibility support?

The source includes semantic labels/live regions, larger-interface typography, reduced-motion preference state, responsive layout work, and theme support. This is not a certification; TalkBack, VoiceOver, Narrator, keyboard, focus, scaling, and device-layout testing remain required.

### Which languages are included?

English is the shipping resource catalog. A System/English preference and localization architecture are implemented. Complete additional catalogs, including Hindi, are future work until all security-sensitive wording can be translated and reviewed correctly.

## Build and development

### What SDK does CipherNest use?

The current repository targets .NET 10 with .NET MAUI. Exact toolchain requirements and target-specific commands are documented in `docs/setup/BUILD.md`.

### How do I run core verification?

Use the committed verification scripts for your platform, including `scripts/verify-core.ps1` or `scripts/verify-core.sh`. Additional Windows, Android, and Apple-host verification scripts are present.

### Can the in-app Buy Me a Coffee link be disabled for a store build?

Yes. Use the documented MSBuild property:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

The correct setting depends on the exact store, region, and distribution policy at release time.

### Does disabling the in-app funding CTA remove repository funding metadata?

No. The build property controls the application UI surface, not the repository's separate funding metadata.

## Testing and CI

### What automated tests exist?

The repository contains UnitTests, IntegrationTests, and UiTests/source tests covering crypto, persistence, session policy, backup/restore, attachment handling, CSV parsing, settings, lifecycle, ViewModels, documentation invariants, branding, support metadata, and other current behavior.

### How many tests passed in the recorded hosted baseline?

For exact historical candidate `2327abba1646082a4d94a689d452b1116701cc0b`, the recorded hosted evidence is 106 unit + 60 integration + 74 UI/source = 240 passed, 0 failed, 0 skipped, plus formatting and platform compile gates and CodeQL. Later commits must rerun the gates rather than inheriting this result automatically.

### Why can a project have passing CI and still not be release-ready?

CI cannot fully prove physical-device biometrics, secure storage, clipboard/history, lifecycle timing, screenshot behavior, accessibility-service behavior, package signing, notarization, store review, or an independent professional security audit.

## Release and distribution

### Where is the release process documented?

Use `docs/releases/RELEASE_PROCESS.md` and `docs/RELEASE_CHECKLIST.md`.

### Are signing keys stored in the repository?

They should not be. Signing identities, private keys, passwords, store API keys, and similar release secrets must remain outside Git history and be supplied through protected environments.

### Can I publish directly from any successful developer build?

A successful developer build is not sufficient. Release candidates need exact CI evidence, target-device validation, dependency/security review, packaging/signing, store-policy checks, documentation freeze, and release provenance.

## Support and reporting

### How do I report a bug?

Use the repository issue templates when the report does not expose sensitive vault content. Do not post passphrases, recovery keys, plaintext secrets, decrypted backups, private keys, or secret-bearing diagnostics in public issues.

### How do I report a security problem?

Follow `SECURITY.md` and `docs/operations/SECURITY_RESPONSE.md`.

### What information should support never request?

Support should not ask users to upload master passphrases, recovery keys, real vault contents, decrypted backups, private keys, or secret-bearing diagnostics.

### Where can I ask general questions?

Use the support contact and repository guidance in `SUPPORT.md`.

## Project roadmap

### What is still planned?

`docs/NEXT_STEPS.md` separates repository work from device/store/audit gates and later-version features. Deferred work must not be advertised as implemented.

### Where can I see the current implementation status?

Use `PROJECT_STATUS.md` for implemented source and external validation gates, `CHANGELOG.md` for release/unreleased changes, and `what_changed.md` for the chronological implementation ledger.
