# Support and Runtime Hardening Verification — 2026-08-13

This addendum records verification requirements for the BMC project-support presentation and the runtime hardening added after the complete documentation pass. It supplements, rather than replaces, `CI_GATES.md`, `SECURITY_HARDENING_2026_08_11.md`, and `DOCUMENTATION_SUITE_2026_08_12.md`.

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

`DecryptedRecordValidationIntegrationTests` now creates authenticated encrypted record payloads using the real crypto/store path and requires Infrastructure to reject:

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

`EncryptedBackupService.RestoreEncryptedAsync` now maps parser-level framing failures to the backup-facing `InvalidDataException` boundary:

- `EndOfStreamException` becomes an invalid-data error identifying a truncated/incomplete backup;
- `JsonException` becomes an invalid-data error identifying a malformed backup header;
- existing cryptographic authentication failures remain mapped to invalid backup data.

Integration coverage requires truncated header bytes and malformed header JSON to be normalized without reaching key derivation.

## Automated gates

The exact candidate must execute:

1. UnitTests.
2. IntegrationTests, including:
   - `DecryptedRecordValidationIntegrationTests`;
   - `VaultSessionTransitionIntegrationTests`;
   - `BackupHeaderValidationIntegrationTests`.
3. UiTests, including `BmcSupportSourceTests` and `DocumentationCoverageSourceTests`.
4. Core formatting/analyzer verification.
5. Windows default and funding-disabled MAUI Release builds.
6. Android MAUI Release build.
7. iOS and Mac Catalyst MAUI Release builds on the configured Apple runner.
8. CodeQL and dependency-review gates configured for the exact candidate.

A configured or queued workflow is not a pass. Record the exact candidate commit and completed workflow evidence before release.

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

Do not infer a security audit, physical secure deletion, universal platform protection, or guaranteed backup recovery from these tests. They verify specific source/runtime invariants only. Independent professional security review remains a separate release/marketing gate.
