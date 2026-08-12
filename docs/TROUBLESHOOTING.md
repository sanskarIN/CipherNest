# CipherNest Troubleshooting

Use this guide for build/runtime problems without exposing real secrets. For normal product workflows see `USER_GUIDE.md`; for backup/restore investigation see `operations/BACKUP_RECOVERY_RUNBOOK.md`; for development/build details see `DEVELOPER_GUIDE.md` and `setup/BUILD.md`.

## Before troubleshooting

Record only non-sensitive environment information:

```text
CipherNest version/build or commit
operating system/version
.NET SDK/workload versions
platform SDK/JDK/Xcode/Windows SDK versions where relevant
fixed/redacted error text
whether the problem reproduces with synthetic data
```

Never post/send:

- master or backup passphrase;
- recovery material;
- biometric secondary secret;
- real vault contents/database;
- decrypted backup/attachment/CSV;
- signing keys/certificates/passwords;
- store/API tokens.

## .NET SDK does not match

Check:

```bash
dotnet --info
```

The repository uses `global.json` with the .NET 10 SDK family. If the requested SDK cannot be resolved, install an appropriate .NET 10 SDK rather than editing the project to an untested framework merely to make one machine build.

If roll-forward behavior is involved, change it only deliberately and re-run all affected verification gates.

## MAUI workload missing

Check:

```bash
dotnet workload list
```

Try:

```bash
dotnet workload restore
```

Then use the target-specific verification script from `setup/BUILD.md`.

Do not assume a successful core `.NET` test build proves MAUI workloads are installed.

## NuGet/package restore fails

Check configured feeds without sharing private credentials:

```bash
dotnet nuget list source
```

Then retry:

```bash
dotnet restore CipherNest.slnx
```

If a package/feed is unavailable, do not lower/remove security dependencies or pin arbitrary versions without reviewing `Directory.Packages.props`, compatibility, licenses, and vulnerabilities.

## Warnings are treated as errors

This is intentional. Repository build policy enables nullable analysis, latest analyzers, code-style enforcement, and `TreatWarningsAsErrors=true`.

Fix the warning/error rather than globally disabling the gate. For a platform-only false positive, document/narrow any suppression and verify all target builds.

## `dotnet format --verify-no-changes` fails

Run formatting against the affected project/source, review the diff, then rerun `scripts/verify-core.ps1` or `scripts/verify-core.sh`.

Do not remove the format gate solely to get a release candidate green.

## Windows MAUI build fails

Use a Windows host with required .NET MAUI/Windows SDK tooling.

Run:

```powershell
./scripts/verify-windows.ps1
```

Direct target:

```powershell
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -f net10.0-windows10.0.19041.0
```

If the default build succeeds but the funding-disabled build fails, verify `CipherNestEnableFundingLink=false` and `BuildFeatureFlags` behavior rather than removing the release variant.

## Android SDK/JDK errors

Use Android SDK/JDK versions supported by the installed .NET MAUI workload and verify IDE/environment paths.

Run:

```bash
sh scripts/verify-android.sh
```

Current app minimum is Android API 26. Optional biometric convenience unlock uses an API-28 `BiometricPrompt` path and must fall back safely below that capability boundary.

## Android biometric feature does not appear

Check:

- device/OS supports the current biometric implementation;
- API level is compatible with the implementation path;
- biometric hardware/enrollment is available;
- the vault has been unlocked with the master passphrase in the current fresh process as required;
- biometric unlock has actually been configured in Settings;
- periodic master-passphrase interval has not expired;
- platform secure storage has not been reset/cleared.

If configuration is missing/corrupted, use the master passphrase. Do not attempt to reconstruct the secondary secret manually.

## Apple targets fail on Windows/Linux

Build iOS/Mac Catalyst on a supported Apple build host with Xcode.

Run on macOS:

```bash
sh scripts/verify-apple.sh
```

A compile on macOS still does not prove signing/provisioning/notarization/Face ID/Touch ID device behavior.

## Apple biometric prompt is unavailable/cancelled

Use the master passphrase fallback. Check device enrollment/permissions/secure-storage state and test the exact target OS/device.

Request cancellation intentionally invalidates the native authentication context; cancellation is not treated as successful authentication.

## Vault does not unlock

Confirm the exact master passphrase or configured recovery material.

Current credential bounds are 12–4,096 characters. Invalid-length guesses are mapped to normal authentication failure rather than raw cryptographic errors.

Do not repeatedly guess if you have valid recovery material available. The application cannot reset/recover a forgotten master passphrase from a CipherNest server because the current release has no server-held key/password-reset service.

Repeated interactive failures also trigger bounded client-side delay; this is expected behavior.

## Recovery material does not authorize a sensitive action

This can be expected. Recovery material can unlock the vault but does not substitute for current-master re-authentication in flows that explicitly require the current master passphrase, such as plaintext export, biometric configuration, destructive actions, or protected-item re-authentication.

Use the current master passphrase for those actions.

## Biometric unlock stopped after backup restore

This is expected. Successful restore clears/disables the local biometric pairing because the restored vault's secondary-wrapper metadata may not correspond to the current installation's secure-storage secret.

Unlock the restored vault with its master passphrase/recovery material and deliberately configure biometrics again.

## Vault locks while an operation is running

Key-using operations are linked to the active unlock session. Locking cancels session-linked key leases, so attachment export/import or other cancellable work can stop with cancellation rather than continue under stale authorization.

Retry only after unlocking and confirming the intended operation.

## Vault search rejects a very large query

Search input is intentionally bounded. Trim/reduce the query. The current service rejects queries longer than 4,096 characters rather than scanning every decrypted field with an unbounded query.

## Item cannot be saved

Check validation limits in `LIMITS_AND_DEFAULTS.md`, especially:

- required title;
- field lengths;
- note 200,000-character / 5,000-line bound;
- tag/custom-field counts;
- attachment count/metadata;
- 2,000,000-character aggregate item-text budget.

Do not bypass validation by writing directly to SQLite; that can create authenticated data that future reads intentionally reject.

## Secure note preview rejects content

The secure-note parser uses a bounded safe Markdown-like subset. Raw HTML is neutralized, and oversized content is rejected.

See `security/SECURE_NOTES.md`.

## Attachment cannot be added

Check:

- source file is readable;
- plaintext size does not exceed 100 MiB;
- item has fewer than 25 attachments;
- vault-wide referenced attachments remain below the 10,000 resource ceiling;
- display filename/media type pass metadata bounds;
- the item is saved/reopened before adding an attachment;
- the vault remains unlocked through the operation.

CipherNest encrypts to a canonical GUID `.cna` name; do not manually rename encrypted attachment files.

## Attachment preview unavailable

In-app preview is intentionally limited to small supported UTF-8 text-family files (TXT/Markdown/CSV/JSON/LOG) up to 512 KiB and displays at most 20,000 characters.

Binary/unsupported/large content must remain encrypted until the user explicitly chooses plaintext export to an external app.

## Attachment export fails or cleanup warning appears

A decrypted attachment export creates a unique temporary plaintext app-cache file for OS sharing.

If export fails, the encrypted source remains the authoritative data. If the app says it cannot confirm cleanup, clear the app cache where safe/possible and avoid assuming the temporary plaintext was physically erased.

Destination apps/OS share providers can retain copies outside CipherNest control.

## Encrypted backup creation fails

Check:

- backup passphrase is 12–4,096 characters;
- destination storage is writable/has space;
- destination is not the active DB/WAL/SHM/recovery path or inside the attachment store;
- vault can lock normally;
- local database/attachments are readable;
- resource ceilings are not exceeded.

Do not manually choose the active SQLite database as a backup destination.

## Backup restore fails

Do not modify the backup in place.

Expected rejection reasons include:

- wrong backup passphrase;
- invalid/corrupt/truncated `CNBK0002` container;
- unsupported/hostile header metadata;
- duplicate/unexpected archive entries;
- oversized archive/resources;
- invalid `.cna` entries;
- staged database failing SQLite/schema/header/item resource validation.

A failed restore before active replacement should not intentionally replace the current vault. Failures after active mutation trigger rollback attempts.

See `operations/BACKUP_RECOVERY_RUNBOOK.md` before doing manual filesystem recovery.

## Backup restore was interrupted

Do not immediately delete files named `.previous.*` or `attachments.previous.*`. They can be recovery artifacts.

If this is important real data, minimize further writes and use a previously verified `.cnbak` if available. Maintainers should reproduce with synthetic data and inspect component-aware DB/WAL/SHM/attachment recovery semantics before advising manual changes.

Never publish a real vault/recovery artifact with its credentials.

## Plaintext CSV import fails

Check:

- file is valid/readable UTF-8 CSV;
- headers are non-empty and case-insensitively unique;
- mapped columns actually exist;
- title mapping is present;
- parser bounds are not exceeded: 256 columns, 100,000 rows, 1,000,000 chars/field, 2,000,000 chars/row;
- vault is unlocked;
- mapped item content passes `VaultItemValidator`.

Import is row-by-row, not an all-or-nothing whole-file transaction; valid earlier rows may remain imported when a later error stops the operation.

## Plaintext CSV export is disabled/rejected

The App requires:

- exact phrase `EXPORT PLAINTEXT`;
- current master-passphrase re-authentication;
- explicit final warning/confirmation.

Recovery material is not accepted for current-master export authorization.

Attachments/custom fields are not included in the current CSV export; use encrypted backup for CipherNest-fidelity recovery.

## Plaintext CSV cleanup warning appears

The app attempts to delete its staging CSV after the share request returns. A warning means CipherNest could not confirm removal of its cache copy.

Use the Settings/manual plaintext-cache cleanup action where appropriate. Copies retained by the OS/share target/provider remain outside CipherNest control.

## Settings reset to defaults

The settings JSON is non-secret. If it is malformed, oversized, or unreadable, CipherNest can fall back to normalized defaults rather than weakening security expectations through invalid values.

Reconfigure preferences deliberately. The encrypted vault itself is separate from `settings.json`.

## Settings value changes after restart

Out-of-range persisted settings are intentionally normalized, for example lock timeout, clipboard delay, trash retention, reminder intervals, and generator defaults. Password mode with every character group disabled is repaired to a valid lowercase group.

See `LIMITS_AND_DEFAULTS.md`.

## Storage usage cannot be measured or cache cannot be cleared

Filesystem permissions, inaccessible directories, or platform cache state can prevent enumeration/deletion.

The storage-maintenance code is intentionally fail-soft and skips reparse-point directories. Do not grant excessive filesystem permissions solely to make the estimate work.

## Clipboard did not clear

CipherNest clears only when the current clipboard still matches the fingerprint of what CipherNest copied. If you copied unrelated content afterward, preserving that newer content is intentional.

The OS/platform can also restrict clipboard access/clearing. Clipboard history/sync/third-party managers can retain older copies independently.

## Clipboard cleared later than expected

The configured delay is normalized to 5–300 seconds. Scheduling/timer/platform activity can affect exact wall-clock delivery.

If the vault locks, CipherNest may request the same conditional cleanup earlier.

## Screenshot protection seems unavailable

Not every target has reliable app-level screenshot blocking through the current implementation. The UI should state honest fallback behavior.

Secret masking still applies, but cameras, desktop capture paths, compromised OS sessions, or unsupported platform surfaces remain outside the guarantee.

## App immediately locks on background/resume

If `LockOnBackground` is enabled, this is expected. Inactivity rules can also lock after the configured timeout or on fail-closed clock rollback.

Review Settings rather than disabling lifecycle security code.

## Startup falls back to default theme/language/accessibility

If persisted preference application fails, startup reports the primary error and independently contains fallback errors while applying safe/default theme/language/accessibility state where possible.

This behavior is reliability hardening; investigate the underlying settings/platform error with privacy-safe diagnostics rather than exposing raw paths/messages to users.

## `DocumentationCoverageSourceTests` fails

Read `verification/DOCUMENTATION_SUITE_2026_08_12.md`.

Common causes:

- required canonical document was removed/renamed;
- root README lost a primary documentation link;
- docs hub lost a major area link;
- independent-audit disclaimer was accidentally weakened/removed.

Update the documentation/source test together. Do not delete the regression gate merely because a path moved.

## Documentation says something different from the source

Treat this as a release-relevant documentation bug, especially for security/recovery/format behavior.

Use `DOCUMENTATION_MAINTENANCE.md` and compare the source-of-truth implementation/contracts/constants. Update all affected canonical docs, changelog/status/test/release gates in the same change series.

## CI is configured but no status appears

Configured workflows are not a pass. Review the exact commit's GitHub Actions/checks. If no run/status exists, record that as missing evidence and execute the appropriate local/platform verification instead of claiming success.

## CodeQL/dependency review reports a problem

Investigate the exact finding/dependency/version. Do not disable the workflow to release.

For dependency exceptions, record owner/reason/severity/expiry and review `THIRD_PARTY_NOTICES.md`/resolved dependency graph.

## Packaging/signing fails

Compilation and signing are separate gates. Check protected signing/provisioning environment, certificate/profile validity, package identifiers, target store requirements, and exact platform tooling.

Never commit signing keys/passwords/store API tokens to fix a packaging error.

See `releases/PACKAGING.md` and `releases/RELEASE_PROCESS.md`.

## Funding CTA must be omitted for a distribution

Do not delete source. Build the target with:

```text
-p:CipherNestEnableFundingLink=false
```

Then verify the funding-disabled compile and record the flag in release provenance. Always check current exact store/region policy before packaging.

## Security issue discovered

Do not publish exploit details in a public issue. Follow root `SECURITY.md` and `operations/SECURITY_RESPONSE.md`.

Use synthetic reproduction and do not request/share real vault contents/credentials.

## Still unresolved

For general help use `SUPPORT.md`. Provide the smallest synthetic reproduction and environment versions possible. If the issue involves possible data loss, avoid repeated destructive/manual filesystem changes until a verified backup/recovery plan is known.
