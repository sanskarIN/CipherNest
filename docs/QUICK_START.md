# CipherNest Quick Start

This guide is the shortest safe path from a fresh checkout or installation to a usable CipherNest vault. It covers both end users and contributors. For complete design, security, format, build, test, and release detail, use [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md) and the documentation hub in [`README.md`](README.md).

> **Security status:** CipherNest has not completed an independent professional security audit. Do not describe it as unhackable, military-grade, 100% secure, or capable of guaranteed physical or managed-memory erasure.

## 1. Project at a glance

- Product: **CipherNest**
- Version in source: **0.1.0**
- Technology: **C# / .NET 10 / .NET MAUI**
- License: **GPL-3.0-or-later**
- Repository: `https://github.com/sanskarIN/CipherNest`
- Application ID: `in.sanskar.ciphernest`
- Supported MAUI targets in source: Android, iOS, Mac Catalyst, Windows
- Local database: SQLite (`ciphernest.db`)
- Encrypted backup extension: `.cnbak`
- Encrypted attachment extension: `.cna`
- Business contact: `sanskarin@outlook.in`
- Support contact: `supportramsandesh@gmail.com`
- Optional development support: `https://buymeacoffee.com/sanskarIN`

CipherNest is local-first. Ordinary use does not require a CipherNest account, email address, phone number, cloud service, or application server.

## 2. End-user quick start

### Step 1 — Start CipherNest

On a new installation the application starts at the Startup route and checks whether a local vault exists.

- No vault: CipherNest routes to Onboarding.
- Existing vault: CipherNest routes to Unlock.

### Step 2 — Create a master passphrase

During onboarding:

1. Enter a unique master passphrase.
2. The accepted cryptographic input range is 12–4,096 characters.
3. The onboarding strength policy must accept the selected passphrase before vault creation.
4. Do not reuse the master passphrase for another service.
5. CipherNest does not store the master passphrase.

The master passphrase derives an Argon2id wrapping key. A separate random 256-bit vault data-encryption key protects vault data.

### Step 3 — Save recovery material

If recovery is enabled, CipherNest returns independent recovery material after vault creation.

- Save it before leaving the recovery flow.
- Keep it separately from the device and master passphrase.
- Never put it in public issues, screenshots, documentation, chat logs, source control, or support messages.
- Recovery can unlock the local vault, but it does not replace current-master authorization for sensitive operations that explicitly require the master passphrase.
- If both the master passphrase and all usable recovery material are lost, CipherNest has no server-side reset path.

### Step 4 — Unlock and lock

The Unlock page accepts the master passphrase or usable recovery material.

On supported Android, iOS, and Mac Catalyst installations, biometric convenience unlock can later be enabled from Settings after current-master re-authentication and OS biometric authentication. Windows currently uses master-passphrase unlock.

Use **Lock** whenever leaving the device. Locking removes/zeroes the shared in-process vault-key buffer where practical and cancels session-linked key leases used by cancellable operations.

## 3. Create the first vault item

From Vault:

1. Choose the new-item action.
2. Select an item type.
3. Enter a required title.
4. Add the optional fields relevant to the item.
5. Optionally assign tags, collection, favorite status, review date, custom fields, attachments, or per-item re-authentication.
6. Save.

Current item types are:

- Login
- Secure Note
- Identity
- Payment Card Reference
- Wi-Fi Credential
- Software License
- Server/SSH Reference
- Document
- Custom
- Time-Based One-Time Password (TOTP)

Item data remains inside authenticated encrypted record payloads at rest. CipherNest intentionally does not create a plaintext full-text-search index for vault fields.

## 4. TOTP quick start

For a TOTP item:

1. Choose **Time-Based One-Time Password (TOTP)**.
2. Put the authorized Base32 seed in the Secret field.
3. Select SHA-1, SHA-256, or SHA-512 according to the provider.
4. Select 6 or 8 digits.
5. Select a period between 15 and 120 seconds; 30 seconds is common.
6. Save the item.
7. Use **Refresh code** when you need a current code.
8. Use **Copy code** only when necessary.

Generated codes are transient and are not persisted in the vault item. QR scanning/rendering and `otpauth://` import/export are not implemented by the current source.

## 5. Add an encrypted attachment

A new item must be saved before adding an attachment.

1. Open the saved item.
2. Choose **Add Attachment**.
3. Pick the source file.
4. CipherNest validates display name/media type.
5. The file is streamed into an authenticated encrypted `.cna` container.

Important current limits:

- 100 MiB maximum plaintext size per attachment;
- 25 attachments maximum per item;
- 10,000 referenced attachments maximum across the vault resource policy;
- 240 UTF-16 code units maximum display name;
- 256 UTF-16 code units maximum media type.

Small supported UTF-8 text-family files can be previewed in bounded memory. Other formats require deliberate plaintext export.

## 6. Search and organize

Vault organization includes:

- local text search;
- collections;
- tags;
- favorites;
- item-type filtering;
- review-due filtering;
- favorite/title sorting;
- recent-use sorting;
- recent-modification sorting;
- title sorting;
- incremental 50-item visual result pages.

Search/audit works over decrypted authenticated objects only while the vault is unlocked.

## 7. Password and passphrase generator

The Generator supports:

- password mode;
- passphrase mode;
- configurable password length;
- uppercase/lowercase/digits/symbols;
- ambiguous-character exclusion;
- configurable passphrase word count.

Password randomness uses the cryptographic random-number generator. Passphrase mode uses a validated local list of exactly 256 unique lowercase words, with 6–16 word bounds and an eight-word default.

## 8. Secure notes

Secure Note preview supports a deliberately small Markdown-like subset:

- headings;
- paragraphs;
- bullet lists;
- checklists;
- fenced code blocks.

Raw HTML is neutralized rather than rendered. Current secure-note ceilings are 200,000 characters and 5,000 lines.

## 9. Clipboard behavior

Secret copy is always explicit. CipherNest uses a fixed-size SHA-256 fingerprint for delayed clipboard comparison instead of keeping the copied plaintext in timer state.

Clipboard clearing is best-effort. Operating-system clipboard history, clipboard sync, other applications, input methods, or accessibility software can retain content beyond CipherNest's control.

## 10. Trash and deletion

- Move to Trash is reversible until retention cleanup or permanent deletion.
- Default trash retention is 30 days.
- Manual permanent deletion requires current-master re-authentication plus destructive confirmation.
- Empty Trash requires current-master re-authentication plus destructive confirmation.
- Full local-vault deletion requires the exact confirmation phrase `DELETE MY VAULT`, current-master authentication, and final confirmation.

Application deletion is logical deletion. CipherNest cannot guarantee physical erasure from flash translation layers, snapshots, device backups, or copies outside its storage boundary.

## 11. Create an encrypted backup

Encrypted `.cnbak` backup is the recommended transfer/recovery path.

1. Open Settings.
2. Enter a strong backup passphrase within 12–4,096 characters.
3. Confirm backup creation.
4. CipherNest locks before creating a consistent snapshot.
5. The snapshot and encrypted attachments are placed into a bounded archive.
6. The archive is encrypted/authenticated with a separately derived backup key.
7. Save the resulting `.cnbak` file according to your recovery plan.
8. Store the backup passphrase separately from the file.
9. Periodically test restore using disposable/controlled data.

Do not assume an app-private backup path is automatically an off-device backup.

## 12. Restore an encrypted backup

1. Enter the backup passphrase.
2. Pick the `.cnbak` file.
3. Confirm replacement.
4. CipherNest locks the current vault.
5. Header/framing/archive/database/resource checks run before active replacement.
6. A valid staged database must pass SQLite integrity, schema, vault-header, item-ID, and resource validation.
7. On success, local biometric pairing is cleared and must be configured again deliberately.
8. Unlock the restored vault using the restored vault's master passphrase or recovery material.

Use [`operations/BACKUP_RECOVERY_RUNBOOK.md`](operations/BACKUP_RECOVERY_RUNBOOK.md) for operational recovery procedures.

## 13. CSV interoperability

CSV is plaintext interoperability, not the recommended secure transfer path.

### Import

Import requires explicit mapping for supported targets such as Title, Username, Secret, URL, Notes, Tags, Collection, and Type. Importing a CSV does not encrypt or remove the original source file outside CipherNest.

### Export

Plaintext export requires:

- exact confirmation phrase `EXPORT PLAINTEXT`;
- current-master re-authentication;
- a separate warning/confirmation.

The exported CSV and share destination are outside the encrypted vault boundary. Attachments are not included in CSV export.

## 14. Recommended first settings review

Open Settings and review:

- inactivity lock timeout;
- lock on background;
- clipboard-clear delay;
- screenshot-protection preference;
- biometric convenience unlock where supported;
- periodic master-passphrase interval;
- theme;
- language;
- reduced motion;
- larger interface;
- trash retention;
- backup reminder;
- review reminders;
- generator defaults.

Default security-relevant values include:

- lock timeout: 60 seconds;
- lock on background: enabled;
- clipboard clear: 30 seconds;
- screenshot-protection preference: enabled;
- periodic master-passphrase interval: 24 hours;
- trash retention: 30 days;
- backup reminder: 7 days;
- review reminder: enabled with 7-day lead time.

## 15. Optional Buy Me a Coffee support

CipherNest includes optional development-support surfaces pointing to:

`https://buymeacoffee.com/sanskarIN`

Support is voluntary and does not change feature access, privacy/security treatment, support priority, licensing, recovery behavior, or open-source rights.

A distribution build can hide the in-app funding CTA without editing source:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

Repository funding metadata remains separate. Check the exact target store/region policy before shipping an external funding CTA.

---

# Contributor quick start

## 16. Clone and inspect

```bash
git clone https://github.com/sanskarIN/CipherNest.git
cd CipherNest
dotnet --info
dotnet workload list
```

The repository uses a .NET 10 SDK family selected by `global.json`, central package versions through `Directory.Packages.props`, nullable analysis, warnings-as-errors, analyzers, code-style enforcement, and deterministic managed builds.

## 17. Repository layout

```text
src/
  CipherNest.Shared/
  CipherNest.Domain/
  CipherNest.Application/
  CipherNest.Infrastructure/
  CipherNest.App/
tests/
  CipherNest.UnitTests/
  CipherNest.IntegrationTests/
  CipherNest.UiTests/
docs/
scripts/
.github/workflows/
```

Dependency direction is deliberately separated. UI code should not directly open the database, derive keys, parse encrypted containers, or obtain raw vault keys.

## 18. Core verification

PowerShell:

```powershell
./scripts/verify-core.ps1
```

POSIX:

```bash
sh scripts/verify-core.sh
```

The core gate restores/builds/runs UnitTests, IntegrationTests, and UiTests/source tests and verifies formatting across the non-MAUI source/test projects.

## 19. Windows verification

```powershell
./scripts/verify-windows.ps1
```

The Windows CI path compiles both the normal funding-enabled build and the `CipherNestEnableFundingLink=false` variant.

## 20. Android verification

```bash
sh scripts/verify-android.sh
```

The current app target is `net10.0-android`, with Android minimum API 26. Optional biometric convenience unlock uses the API-28 `BiometricPrompt` baseline.

## 21. Apple verification

On a compatible Mac/Xcode environment:

```bash
sh scripts/verify-apple.sh
```

The current source targets iOS 15+ and Mac Catalyst 15+. Hosted CI records its exact .NET/workload/Xcode combination in verification documentation.

## 22. Pre-documentation verified implementation baseline

The immutable implementation baseline immediately before this documentation expansion is:

- commit: `8566980ff981b8b4072f9010ec7b7ba54aba051e`;
- CipherNest CI run: `31937127961` — completed successfully;
- CodeQL run: `31937127900` — completed successfully;
- Unit tests: 346 passed;
- Integration tests: 98 passed;
- UI/source tests: 111 passed;
- total: **555 passed, 0 failed, 0 skipped**;
- core formatting: passed;
- Windows default Release: passed;
- Windows funding-disabled Release: passed;
- Android Release: passed;
- iOS simulator Release: passed;
- Mac Catalyst Release: passed;
- CodeQL v4: passed after analyzable core and MAUI application builds.

Any documentation commit after that SHA becomes a new exact head and must rerun configured gates before being described as an exact-head verified release candidate.

## 23. Before editing security-sensitive code

Read at minimum:

- [`security/THREAT_MODEL.md`](security/THREAT_MODEL.md)
- [`security/CRYPTOGRAPHIC_DESIGN.md`](security/CRYPTOGRAPHIC_DESIGN.md)
- [`security/SESSION_SECURITY.md`](security/SESSION_SECURITY.md)
- [`architecture/SESSION_AND_CONCURRENCY.md`](architecture/SESSION_AND_CONCURRENCY.md)
- [`architecture/DATABASE.md`](architecture/DATABASE.md)
- [`formats/VAULT_HEADER.md`](formats/VAULT_HEADER.md)
- [`formats/VAULT_RECORDS.md`](formats/VAULT_RECORDS.md)
- [`formats/ATTACHMENTS.md`](formats/ATTACHMENTS.md)
- [`formats/ENCRYPTED_BACKUP.md`](formats/ENCRYPTED_BACKUP.md)
- [`TEST_PLAN.md`](TEST_PLAN.md)
- [`RELEASE_CHECKLIST.md`](RELEASE_CHECKLIST.md)

Do not weaken bounds, cancellation, associated-data binding, warnings-as-errors, documentation disclaimers, or recovery/rollback rules to make a failing test/build disappear.

## 24. Where to go next

- Full consolidated reference: [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md)
- Feature status: [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md)
- Page-by-page UI: [`UI_REFERENCE.md`](UI_REFERENCE.md)
- Settings/build configuration: [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md)
- End-user details: [`USER_GUIDE.md`](USER_GUIDE.md)
- Developer details: [`DEVELOPER_GUIDE.md`](DEVELOPER_GUIDE.md)
- API/contracts: [`API_REFERENCE.md`](API_REFERENCE.md)
- Limits/defaults: [`LIMITS_AND_DEFAULTS.md`](LIMITS_AND_DEFAULTS.md)
- Build: [`setup/BUILD.md`](setup/BUILD.md)
- Troubleshooting: [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md)
- Roadmap: [`NEXT_STEPS.md`](NEXT_STEPS.md)
