# Support and Runtime Hardening Verification — 2026-08-13

This addendum records verification requirements for the BMC project-support presentation and the runtime hardening added after the complete documentation pass. It supplements, rather than replaces, `CI_GATES.md`, `SECURITY_HARDENING_2026_08_11.md`, `DOCUMENTATION_SUITE_2026_08_12.md`, and the exact hosted-run record in `HOSTED_CI_EVIDENCE_2026_08_13.md`.

## Source changes covered

### BMC project-support surface

- `src/CipherNest.App/Resources/Images/bmc_support.svg` is an original CipherNest support badge with a coffee-cup/BMC motif and `Support CipherNest` wording.
- The asset is intentionally not represented as the official Buy Me a Coffee brand logo.
- `README.md` displays the badge prominently and links it directly to `https://buymeacoffee.com/sanskarIN`.
- `SUPPORT.md` displays the same badge and canonical project-support URL.
- About uses the compiled MAUI image inside a highlighted project-support card. Both the badge tap target and the explicit button route through `OnBuyMeACoffeeClicked` and `AppConstants.BuyMeACoffeeUrl`.
- `BuildFeatureFlags.IsFundingLinkEnabled` continues to control the in-app surface. A funding-disabled store/distribution build must not expose the in-app badge/card/link.
- Financial support remains voluntary and must not change feature access, security/privacy treatment, support priority, licensing, recovery, or open-source rights.

### Authenticated decrypted-record validation

`DecryptedRecordValidationIntegrationTests` creates authenticated encrypted record payloads using the real crypto/store path and requires Infrastructure to reject:

- a payload whose internal `VaultItem.Id` differs from the authenticated SQLite row ID;
- a payload with an invalid `VaultItemType` even though its AES-GCM authentication is otherwise valid.

These are runtime integration gates for the existing `VaultService.DecryptItem` identity/metadata boundary. Source-string tests remain supplementary only.

### Serialized lock/unlock transition behavior

`VaultSessionTransitionIntegrationTests` blocks a real master-key unwrap, starts `LockAsync` while unlock owns the transition gate, then releases unwrap. The test requires:

- lock to remain pending while unlock holds the transition gate;
- both operations to complete within bounded test time;
- the final vault state to be locked.

The waits use explicit timeouts so a regression fails instead of hanging CI indefinitely.

### Hostile backup headers before KDF work

`BackupHeaderValidationIntegrationTests` constructs synthetic `.cnbak` framing and requires invalid headers to fail before `ICryptoService.DeriveKey` is called, including:

- unsupported backup format version;
- KDF memory metadata far above the supported resource ceiling.

A derivation-guard crypto implementation records whether key derivation was attempted.

### Malformed/truncated backup framing normalization

`EncryptedBackupService.RestoreEncryptedAsync` maps parser-level framing failures to the backup-facing `InvalidDataException` boundary:

- `EndOfStreamException` becomes an invalid-data error identifying a truncated/incomplete backup;
- `JsonException` becomes an invalid-data error identifying a malformed backup header;
- existing cryptographic authentication failures remain mapped to invalid backup data.

Integration coverage requires truncated header bytes and malformed header JSON to be normalized without reaching key derivation.

### SQLite dependency remediation

A hosted restore surfaced `NU1903` for the older `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 native dependency. The repository now:

- pins `Microsoft.Data.Sqlite` to `10.0.10`;
- explicitly pins and references `SQLitePCLRaw.bundle_e_sqlite3` `2.1.12`.

The later exact hosted candidate restored/built without the earlier blocker. This is not a permanent vulnerability exemption: pull-request dependency review and release-time direct/transitive advisory inspection remain required.

### Windows WinRT/AOT observable-property migration

A hosted Windows Release build surfaced CommunityToolkit `MVVMTK0045` for field-based `[ObservableProperty]` generation on the Windows/WinRT target.

CipherNest corrected the source rather than suppressing the analyzer:

- affected MAUI ViewModels use public partial observable properties;
- `CipherNest.App.csproj` explicitly sets `<LangVersion>preview</LangVersion>` because the current CommunityToolkit partial-property syntax requires that feature in this toolchain;
- `ViewModelAotSourceTests` rejects field-based observable-property generation in the ViewModel directory and requires the app-level language setting.

Both the normal and funding-disabled Windows Release builds subsequently passed on the exact hosted candidate.

### Cross-platform CI target/toolchain alignment

- App-only `CipherNestTargetFrameworks` selection prevents unrelated MAUI targets from entering a platform-specific restore/build graph.
- Android CI uses `net10.0-android` with `android-arm64`.
- Windows CI uses `net10.0-windows10.0.19041.0` with `win-x64`.
- Apple CI uses the supported `macos-26` runner label, .NET SDK `10.0.302`, Xcode `26.5`, workload set `10.0.300.3`, `iossimulator-arm64`, and `maccatalyst-arm64`.
- CodeQL uses `github/codeql-action` v4 and builds the Android MAUI application path before analysis.

No compatibility validation is disabled to obtain these builds.

## Automated gates

The exact candidate must execute:

1. UnitTests.
2. IntegrationTests, including:
   - `DecryptedRecordValidationIntegrationTests`;
   - `VaultSessionTransitionIntegrationTests`;
   - `BackupHeaderValidationIntegrationTests`.
3. UiTests, including `BmcSupportSourceTests`, `DocumentationCoverageSourceTests`, and `ViewModelAotSourceTests`.
4. Core formatting/analyzer verification.
5. Windows default and funding-disabled MAUI Release builds.
6. Android MAUI Release build.
7. iOS and Mac Catalyst MAUI Release builds on the configured Apple runner/toolchain pair.
8. CodeQL for the exact candidate.
9. Pull-request dependency review where the candidate is exercised through the PR gate, plus release-time advisory review of the exact restored graph.

A configured or queued workflow is not a pass. Record the exact candidate commit and completed workflow evidence before release.

## Hosted evidence captured for the current hardening baseline

For candidate `2327abba1646082a4d94a689d452b1116701cc0b`:

- UnitTests: **106 passed**.
- IntegrationTests: **60 passed**.
- UiTests/source tests: **74 passed**.
- Total runtime/source tests: **240 passed, 0 failed, 0 skipped**.
- Core formatting: passed.
- Windows default Release: passed.
- Windows funding-disabled Release: passed.
- Android Release: passed.
- iOS simulator Release: passed.
- Mac Catalyst Release: passed.
- CodeQL v4: passed after analyzable core and Android MAUI builds.

The exact GitHub run identifiers and limitations are recorded in `HOSTED_CI_EVIDENCE_2026_08_13.md`. A later commit does not inherit these results automatically.

## Manual BMC visual/accessibility gates

On every supported target used for release:

- confirm the BMC card remains readable in light and dark themes;
- confirm the badge scales without clipping on narrow phones and resizable desktop windows;
- confirm the badge has useful semantic description and its tap target is operable with screen-reader/touch/keyboard behavior supported by the platform;
- confirm the explicit BMC button has sufficient size/contrast and opens the expected HTTPS URL;
- confirm launcher failure remains privacy-safe and does not expose exception/path detail;
- build once with `CipherNestEnableFundingLink=false` and confirm the in-app support card/metadata are hidden;
- verify the current store/distribution policy for the exact target/region before deciding whether to ship the in-app funding surface.

## Security/data rules for these tests

Use synthetic/disposable vaults, passphrases, backup files, and record data only. Never use a real user vault, production credentials, real recovery material, signing secrets, or private documents as test fixtures.

## Release wording

Do not infer a security audit, physical secure deletion, universal platform protection, guaranteed backup recovery, store acceptance, signing correctness, or physical-device biometric correctness from these tests/builds. They verify specific source/runtime/compile/static-analysis invariants only. Independent professional security review remains a separate release/marketing gate.
