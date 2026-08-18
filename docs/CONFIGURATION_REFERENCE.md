# CipherNest Configuration Reference

This document centralizes the current configurable values, build properties, target frameworks, persisted non-secret settings, package/toolchain policy, file/format identifiers, and release-time switches used by CipherNest.

Configuration values are not all equivalent: some are **user preferences**, some are **build properties**, and others are **compatibility/security constants** that must not be edited casually.

## 1. Configuration categories

| Category | Examples | Change risk |
|---|---|---|
| User preferences | theme, lock timeout, reminders | Normal product configuration; still normalized/bounded. |
| Build properties | funding CTA, target-framework selection | Release/toolchain behavior; must be recorded in provenance. |
| Product metadata | version, application ID, contacts | Public identity/release behavior. |
| Storage/format constants | schema version, magic values, file names | Compatibility-sensitive. |
| Security/resource bounds | KDF limits, record sizes, parser ceilings | Security/performance-sensitive. |
| CI/toolchain pins | SDK/workload/Xcode/RID | Verification/reproducibility-sensitive. |

## 2. Product metadata

Current source values from `CipherNest.Shared.AppConstants` and the MAUI project:

| Setting | Value |
|---|---|
| Product name | `CipherNest` |
| Product/source version | `0.1.0` |
| MAUI display version | `0.1.0` |
| MAUI application version | `1` |
| Application ID | `in.sanskar.ciphernest` |
| Database filename | `ciphernest.db` |
| Attachment directory | `attachments` |
| Backup extension | `.cnbak` |
| Business email | `sanskarin@outlook.in` |
| Support email | `supportramsandesh@gmail.com` |
| Repository | `https://github.com/sanskarIN/CipherNest` |
| Creator profile | `https://www.github.com/sanskarIN` |
| Buy Me a Coffee | `https://buymeacoffee.com/sanskarIN` |
| Creator watermark | `Made by the Sanskar` |

When product metadata changes, update `AppConstants`, MAUI metadata where applicable, README/support/legal/store documentation, tests that guard centralized metadata, and release provenance.

## 3. Target frameworks and minimum OS versions

`CipherNest.App` is a .NET MAUI Single Project application with these target frameworks:

```text
net10.0-android
net10.0-ios
net10.0-maccatalyst
net10.0-windows10.0.19041.0
```

Minimum platform versions declared by the project:

| Target | Minimum |
|---|---:|
| Android | API 26 |
| iOS | 15.0 |
| Mac Catalyst | 15.0 |
| Windows | 10.0.19041.0 |

The Android biometric convenience-unlock implementation itself uses the API-28 `BiometricPrompt` baseline and falls back where the capability is not usable.

## 4. `CipherNestTargetFrameworks`

The App project uses a custom MSBuild property:

```xml
<CipherNestTargetFrameworks>...</CipherNestTargetFrameworks>
```

When not supplied, it contains all four MAUI targets. CI and verification scripts override it for platform-only builds so unsupported target graphs do not enter restore/build on the current host.

Examples:

### Android

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release \
  -p:CipherNestTargetFrameworks=net10.0-android \
  -f net10.0-android \
  -r android-arm64
```

### iOS simulator

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release \
  -p:CipherNestTargetFrameworks=net10.0-ios \
  -f net10.0-ios \
  -r iossimulator-arm64
```

### Mac Catalyst

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release \
  -p:CipherNestTargetFrameworks=net10.0-maccatalyst \
  -f net10.0-maccatalyst \
  -r maccatalyst-arm64
```

### Windows

```powershell
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release `
  -p:CipherNestTargetFrameworks=net10.0-windows10.0.19041.0 `
  -f net10.0-windows10.0.19041.0 `
  -r win-x64 `
  -p:WindowsPackageType=None
```

## 5. Funding/BMC build switch

The optional in-app development-support surface is controlled by:

```text
CipherNestEnableFundingLink
```

Default:

```text
true
```

Disable example:

```bash
dotnet build src/CipherNest.App/CipherNest.App.csproj -c Release -p:CipherNestEnableFundingLink=false
```

When explicitly false, the project adds:

```text
CIPHERNEST_DISABLE_FUNDING_LINK
```

Application funding surfaces guarded by `BuildFeatureFlags.IsFundingLinkEnabled` are hidden/removed accordingly.

This setting does **not** remove `.github/FUNDING.yml` because repository funding metadata is separate from the application binary.

Release rule: determine the value after checking the exact target store, region, and distribution policy, and record the chosen value in release provenance.

## 6. SDK selection

`global.json` currently requests:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

Meaning:

- baseline SDK family: .NET 10;
- feature-band roll-forward is allowed;
- prerelease SDK selection is not allowed by this file.

Use:

```bash
dotnet --info
dotnet workload list
```

to record the actual SDK/workload resolved on a build host.

## 7. Shared build-quality policy

`Directory.Build.props` applies:

```text
LangVersion = latest
Nullable = enable
ImplicitUsings = enable
TreatWarningsAsErrors = true
AnalysisLevel = latest
EnforceCodeStyleInBuild = true
Deterministic = true
ContinuousIntegrationBuild = true when CI=true
```

The MAUI App project deliberately overrides:

```text
LangVersion = preview
```

That override exists because the current verified CommunityToolkit MVVM partial observable-property syntax used for WinRT/AOT-safe ViewModels requires the preview language feature in the App toolchain.

Do not globally disable warnings-as-errors, analyzers, nullable analysis, deterministic builds, or the CommunityToolkit WinRT/AOT diagnostics as a shortcut around a build failure.

## 8. Central package versions

`Directory.Packages.props` centrally manages package versions.

Current direct version pins include:

| Package | Version |
|---|---:|
| CommunityToolkit.Mvvm | `8.4.0` |
| Konscious.Security.Cryptography.Argon2 | `1.3.1` |
| Microsoft.Data.Sqlite | `10.0.10` |
| SQLitePCLRaw.bundle_e_sqlite3 | `2.1.12` |
| Microsoft.Extensions.Logging.Debug | `10.0.0` |
| Microsoft.Maui.Controls | `10.0.0` |
| Microsoft.NET.Test.Sdk | `18.0.0` |
| xunit | `2.9.3` |
| xunit.runner.visualstudio | `3.1.4` |

TOTP setup-URI text interoperability does not add a third-party QR, URI-parser, camera, network, or authenticator dependency; it uses the platform/BCL URI facilities plus existing CipherNest validation.

Dependency changes require restore/build/test review, vulnerability/advisory review, third-party-license review, and release-documentation updates.

## 9. Persisted user preferences

`AppPreferences` is non-secret local configuration. It must never become a secret store.

### Theme

Property:

```text
Theme
```

Values:

```text
System
Light
Dark
```

Default: `System`

Undefined persisted enum values normalize back to `System`.

### Language

Property:

```text
Language
```

Values:

```text
System
English
Hindi
```

Default: `System`

Neutral English is the fallback. The reviewed Hindi catalog covers the resource-backed interface, but every remaining UI literal is not claimed translated.

### Lock timeout

Property:

```text
LockTimeoutSeconds
```

Default: `60`

Normalized range:

```text
5..3600 seconds
```

### Lock on background

Property:

```text
LockOnBackground
```

Default: `true`

Actual lifecycle enforcement is platform/runtime-dependent and remains a device validation gate.

### Clipboard clear interval

Property:

```text
ClipboardClearSeconds
```

Default: `30`

Normalized range:

```text
5..300 seconds
```

Clipboard cleanup is best-effort and does not guarantee deletion from OS history/synchronization. This same configured interval is used when a TOTP setup URI is explicitly copied because the URI contains the long-lived seed and follows the secret-clipboard path.

### Screenshot protection

Property:

```text
ScreenshotProtection
```

Default: `true`

This is a preference for supported platform controls, not a guarantee that every capture path is blocked.

### Biometric unlock enabled

Property:

```text
BiometricUnlockEnabled
```

Default: `false`

The preference alone is not sufficient authorization; configured secondary wrapper, secure storage, master-auth interval, and platform capability also matter.

### Reduced motion

Property:

```text
ReducedMotion
```

Default: `false`

### Larger interface

Property:

```text
LargerInterface
```

Default: `false`

### Trash retention

Property:

```text
TrashRetentionDays
```

Default: `30`

Normalized range:

```text
1..365 days
```

### Master-passphrase re-auth interval

Property:

```text
RequireMasterPassphraseAfterHours
```

Default: `24`

Normalized range:

```text
1..168 hours
```

A fresh process still begins by requiring master-auth state before biometric convenience unlock can later be used.

### Backup reminder

Property:

```text
BackupReminderDays
```

Default: `7`

Normalized range:

```text
1..365 days
```

### Review reminders enabled

Property:

```text
ReviewRemindersEnabled
```

Default: `true`

### Review reminder lead

Property:

```text
ReviewReminderLeadDays
```

Default: `7`

Normalized range:

```text
0..365 days
```

### Generator mode

Property:

```text
GeneratorPassphraseMode
```

Default: `false` (password mode)

### Generator password length

Property:

```text
GeneratorPasswordLength
```

Default: `20`

Normalized range:

```text
8..256 characters
```

### Generator passphrase word count

Property:

```text
GeneratorPassphraseWordCount
```

Default: `8`

Normalized range:

```text
6..16 words
```

### Generator character groups

Properties:

```text
GeneratorUppercase
GeneratorLowercase
GeneratorDigits
GeneratorSymbols
GeneratorExcludeAmbiguous
```

Defaults:

```text
Uppercase = true
Lowercase = true
Digits = true
Symbols = true
ExcludeAmbiguous = true
```

If password mode is active and every character group is false, normalization enables lowercase so an unusable empty character set is not retained.

### Last successful backup timestamp

Property:

```text
LastSuccessfulBackupUtc
```

Default: `null`

This is non-secret local reminder metadata.

## 10. Settings-file safety policy

Current settings JSON policy:

| Rule | Value |
|---|---:|
| Maximum file size | 64 KiB |
| Actual read buffer | 64 KiB + 1 sentinel byte |
| Maximum JSON nesting depth | 16 |
| Invalid UTF-8 | fall back to normalized defaults |
| Malformed JSON | fall back to normalized defaults |
| Over-depth JSON | fall back to normalized defaults |
| Oversized JSON | fall back to normalized defaults |
| Cancellation | propagates; not converted into fallback |

The loader performs both a file-length check and an independent bounded actual read so a file that changes between those moments cannot cause an unbounded parser read.

## 11. Product/format versions

Current compatibility identifiers:

| Surface | Version/value |
|---|---|
| Product version | `0.1.0` |
| Database schema | `1` |
| Core encrypted envelope | `1` |
| Minimum supported vault-header document | `1` |
| Current vault-header document | `2` |
| Encrypted backup format | `2` |
| Backup magic | `CNBK0002` |
| Attachment magic | `CNAT0001` |

The TOTP setup-URI codec is transient text interoperability and does not introduce a new persisted format/schema version. The setup URI is parsed into or formatted from existing TOTP item fields.

Never silently reuse an old format/schema version for an incompatible structure.

## 12. Cryptographic defaults

Current new-wrapper values:

| Setting | Value |
|---|---:|
| Vault/AES key | 32 bytes / 256 bits |
| AES-GCM nonce | 12 bytes |
| AES-GCM tag | 16 bytes |
| Argon2id memory | 65,536 KiB / 64 MiB |
| Argon2id iterations | 3 |
| Argon2id parallelism | 1 |
| New-wrapper salt | 16 bytes |
| Derived key | 32 bytes |

Accepted untrusted KDF metadata is separately bounded before expensive work:

| Parameter | Minimum | Maximum |
|---|---:|---:|
| Salt | 16 bytes | 64 bytes |
| Argon2id memory | 16 MiB | 512 MiB |
| Iterations | 1 | 10 |
| Parallelism | 1 | 16 |

Accepted lower/higher compatibility values are not the defaults used for new wrappers.

## 13. Credential input bounds

Crypto-bound passphrase/recovery/secondary/backup inputs:

```text
12..4096 characters
```

The upper limit is a resource-safety bound, not a recommendation to use extremely long secrets.

## 14. Vault storage limits

| Resource | Maximum |
|---|---:|
| Vault-header UTF-8 | 64 KiB |
| Vault-header JSON depth | 16 |
| Decrypted/serialized item JSON | 16 MiB |
| Stored encrypted envelope/row | 24 MiB |
| Item rows | 100,000 |
| Aggregate encrypted envelopes | 256 MiB |
| Referenced attachments | 10,000 |

See [`LIMITS_AND_DEFAULTS.md`](LIMITS_AND_DEFAULTS.md) for every current parser/item/attachment/backup ceiling.

## 15. Vault item limits

| Resource | Maximum/rule |
|---|---|
| Item ID | non-empty GUID |
| Title | required; 256 chars |
| Username | 2,048 chars |
| General secret | 100,000 chars |
| URL | 4,096 chars |
| Secure note | 200,000 chars / 5,000 lines |
| Collection | 128 chars |
| Tags | 100 × 128 chars |
| Custom fields | 100 |
| Custom field name | 128 chars |
| Custom field value | 100,000 chars |
| Attachments/item | 25 |
| Combined item text/metadata | 2,000,000 chars |

## 16. TOTP configuration

| Setting | Default | Supported |
|---|---|---|
| Algorithm | SHA-1 | SHA-1 / SHA-256 / SHA-512 |
| Digits | 6 | 6 or 8 |
| Period | 30 s | 15–120 s |
| Formatted seed | — | up to 4,096 chars |
| Normalized Base32 seed | — | 16–1,024 chars |

Whitespace and `-` grouping are removed during normalization. Base32 input is case-insensitive. Generated codes are not persisted.

### TOTP setup-URI parser/formatter

| Setting/resource | Current rule |
|---|---|
| Scheme/type | absolute `otpauth://totp/...` only |
| URI text | maximum 8,192 characters |
| Query pairs | maximum 16 |
| Query parameter name | maximum 64 characters; ASCII letters/digits/`-`/`_` |
| Account name | maximum 512 characters |
| Issuer | maximum 256 characters |
| Label | maximum 769 characters before splitting |
| Duplicate query keys | rejected case-insensitively |
| User-info/custom port/fragment | rejected |
| HOTP/`counter` | rejected |
| Unicode Control/Format in display metadata | rejected |
| Label/query issuer disagreement | rejected |
| Secret/settings validation | delegated to the same `TotpPolicy` used by code generation |

`TotpUriCodec` is registered as the singleton implementation of Application `ITotpUriCodec`. Setup URI import/export is local only; no camera, QR, network/provider, or cloud service is configured. The imported URI field is transient sensitive UI state and is not persisted as a separate setting or vault property.

## 17. Attachment configuration/limits

| Setting | Value |
|---|---:|
| Plaintext chunk size | 256 KiB |
| Maximum plaintext file | 100 MiB |
| Maximum encrypted chunk count | 16,384 |
| Display name | max 240 UTF-16 code units |
| Media type | max 256 UTF-16 code units |
| Missing media type | `application/octet-stream` |
| Opaque storage filename | 32-char GUID-N + `.cna` |
| Decrypted text-preview bytes | 512 KiB |
| Displayed preview chars | 20,000 |

## 18. Backup configuration/limits

| Setting | Value |
|---|---:|
| Format version | 2 |
| Header JSON | 16–16,384 bytes |
| Header JSON depth | 16 |
| Accepted chunk size | 64 KiB–4 MiB |
| Current export chunk size | 1 MiB |
| Maximum encrypted chunk indexes | 65,536 |
| Maximum archive entries | 10,001 |
| Maximum aggregate plaintext archive content | 1 GiB |

Backup restore applies both authenticated-container checks and staged SQLite/resource validation before replacement.

## 19. CSV parser limits

| Resource | Maximum |
|---|---:|
| Columns | 256 |
| Data rows | 100,000 |
| Header-name characters | 256 |
| Field characters | 1,000,000 |
| Aggregate row characters | 2,000,000 |
| Retained user-visible warnings | 20 |

CSV mapped Tags additionally enforce the canonical 100-tag / 128-character item policy before item construction. TOTP setup-URI text interoperability is a separate Item Editor path rather than CSV configuration.

## 20. Verification scripts

Canonical scripts:

```text
scripts/verify-core.ps1
scripts/verify-core.sh
scripts/verify-windows.ps1
scripts/verify-android.sh
scripts/verify-apple.sh
```

Prefer these scripts over inventing ad hoc commands when validating the repository.

## 21. Hosted Apple toolchain record

The recorded compatible hosted Apple pairing for the verified line uses:

```text
runner: macos-26
.NET SDK: 10.0.302
Xcode: 26.5
.NET workload set: 10.0.300.3
iOS RID: iossimulator-arm64
Mac Catalyst RID: maccatalyst-arm64
```

These values are evidence for the recorded CI pairing, not a permanent requirement for every future developer machine. The actual local .NET Apple workloads and Xcode must be mutually compatible.

## 22. Pre-documentation verified baseline

Immediately before this documentation expansion:

```text
commit: 8566980ff981b8b4072f9010ec7b7ba54aba051e
CipherNest CI: 31937127961 — success
CodeQL: 31937127900 — success
Unit: 346 passed
Integration: 98 passed
UI/source: 111 passed
Total: 555 passed, 0 failed, 0 skipped
Windows default: passed
Windows funding-disabled: passed
Android: passed
iOS simulator: passed
Mac Catalyst: passed
CodeQL v4: passed
```

Later commits, including the August 18 TOTP setup-URI continuation, must rerun exact-head gates before being described as release-candidate verified.

## 23. Configuration changes requiring documentation/review

Update the relevant docs/tests whenever changing:

- application ID/version;
- public contacts/BMC URL;
- target frameworks/minimum OS versions;
- SDK/workload/toolchain pins;
- package versions;
- build properties/symbols;
- database schema version;
- cryptographic/backup/attachment format versions;
- KDF defaults/bounds;
- vault item/resource ceilings;
- settings defaults/bounds;
- TOTP algorithms/digits/period/seed rules;
- TOTP setup-URI syntax/resource/metadata/clipboard boundaries;
- backup/archive ceilings;
- CSV limits;
- platform capability claims;
- localization options;
- accessibility preferences;
- funding/store behavior.

## 24. Related references

- [`QUICK_START.md`](QUICK_START.md)
- [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md)
- [`UI_REFERENCE.md`](UI_REFERENCE.md)
- [`LIMITS_AND_DEFAULTS.md`](LIMITS_AND_DEFAULTS.md)
- [`API_REFERENCE.md`](API_REFERENCE.md)
- [`security/TOTP.md`](security/TOTP.md)
- [`setup/BUILD.md`](setup/BUILD.md)
- [`verification/CI_GATES.md`](verification/CI_GATES.md)
- [`releases/REPRODUCIBLE_BUILDS.md`](releases/REPRODUCIBLE_BUILDS.md)
- [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md)
