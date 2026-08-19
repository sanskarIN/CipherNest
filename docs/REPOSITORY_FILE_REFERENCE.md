# CipherNest Complete Repository File Reference

This document is the repository-level map for **every tracked file class** in CipherNest. Together with [`SOURCE_CODE_REFERENCE.md`](SOURCE_CODE_REFERENCE.md) and [`TEST_SUITE_REFERENCE.md`](TEST_SUITE_REFERENCE.md), it forms the exhaustive file-by-file documentation set.

Inventory baseline: `7d046ab5c6dc15eecf06599ed68317aa88d8967` on 2026-08-19. The two new reference files themselves are part of the documentation suite and are listed below. Any later added/renamed tracked file must be documented in the same change series.

> Historical verification records describe their own immutable candidates. Their old pass counts and run IDs must not be copied to a newer head without new evidence.

# 1. Exhaustive inventory boundaries

The repository is split into three non-overlapping documentation inventories:

1. **This file** — root metadata, GitHub automation/community files, build/verification scripts, all documentation, legal/support/status/history files.
2. **`SOURCE_CODE_REFERENCE.md`** — every tracked production file under `src/`, including project files, C#, XAML, manifests, localization catalogs, styles, icons, images, splash and raw resources.
3. **`TEST_SUITE_REFERENCE.md`** — every tracked file under `tests/`, including shared test configuration, test project files, unit tests, integration tests and UI/source-contract tests.

Directories themselves are organizational and are not runtime artifacts. Every tracked file inside them belongs to one of the three inventories above.

# 2. Root repository files

- `.editorconfig` — repository-wide editor/text formatting rules.
- `.gitignore` — generated build/IDE/platform artifacts excluded from version control.
- `CipherNest.slnx` — solution definition joining production and test projects.
- `Directory.Build.props` — repository-wide .NET compiler/analyzer/build properties.
- `Directory.Packages.props` — centrally managed NuGet dependency versions; dependency documentation must reconcile to this file.
- `global.json` — .NET SDK selection/roll-forward contract for local and hosted builds.
- `README.md` — public project overview, feature/security scope, documentation entry points, build/evidence summary, and optional funding information.
- `LICENSE` — GPL-3.0-or-later license text.
- `CODE_OF_CONDUCT.md` — contributor/community conduct expectations.
- `CONTRIBUTING.md` — contribution workflow, security-sensitive change expectations, testing/documentation requirements.
- `SECURITY.md` — vulnerability reporting and security-claim boundaries.
- `PRIVACY.md` — user-facing local-first/privacy/plaintext-boundary disclosures.
- `TERMS.md` — project terms/disclaimers.
- `SUPPORT.md` — user/contributor support channels and optional support link.
- `THIRD_PARTY_NOTICES.md` — dependency/license notice families; exact release graph review remains a release gate.
- `CHANGELOG.md` — curated release/unreleased feature and hardening history.
- `PROJECT_STATUS.md` — current implementation/evidence/deferred-state snapshot rather than an implementation diary.
- `DECISIONS.md` — architectural/product/security decisions and rationale retained for maintainers.
- `what_changed.md` — current chronological implementation/documentation ledger; older large ledgers are archived under `docs/history/`.

# 3. GitHub repository automation and community configuration

## Funding and contribution templates

- `.github/FUNDING.yml` — repository-level optional funding metadata.
- `.github/PULL_REQUEST_TEMPLATE.md` — pull-request review checklist and contribution prompts.
- `.github/ISSUE_TEMPLATE/bug_report.yml` — structured bug-report form; reporters should not be asked to disclose vault secrets.
- `.github/ISSUE_TEMPLATE/feature_request.yml` — structured feature-request form.
- `.github/ISSUE_TEMPLATE/config.yml` — issue-template chooser/contact configuration.
- `.github/dependabot.yml` — automated dependency-update configuration.

## Workflows

- `.github/workflows/dotnet-desktop.yml` — primary CipherNest CI workflow covering core restore/build/test/format and configured Windows/Android/Apple build gates.
- `.github/workflows/codeql.yml` — CodeQL source analysis workflow, including analyzable core/application build setup.
- `.github/workflows/dependency-review.yml` — pull-request dependency/security review gate where applicable.

Workflow existence means a gate is **configured**, not that any later commit passed it. Evidence rules live in `docs/verification/CI_GATES.md`.

# 4. Local verification/build scripts

- `build/scripts/verify.ps1` — PowerShell aggregate verification entry point under the build tooling path.
- `build/scripts/verify.sh` — POSIX shell aggregate verification entry point.
- `scripts/verify-core.ps1` — Windows/PowerShell core restore/build/test/format verification.
- `scripts/verify-core.sh` — POSIX core restore/build/test/format verification.
- `scripts/verify-windows.ps1` — Windows Release build verification, including the separate funding-disabled build path where configured.
- `scripts/verify-android.sh` — Android Release build verification.
- `scripts/verify-apple.sh` — iOS simulator and Mac Catalyst build verification.

These scripts are documented by `docs/setup/BUILD.md`, `docs/TESTING_GUIDE.md`, and `docs/verification/CI_GATES.md`.

# 5. Production and test source inventories

Every tracked file under `src/` is documented individually in [`SOURCE_CODE_REFERENCE.md`](SOURCE_CODE_REFERENCE.md). That includes:

- all four production project files and all C# source;
- MAUI `App`, Shell, pages, code-behind, ViewModels, services, converters and localization extension;
- Android, iOS, Mac Catalyst and Windows entry points/manifests;
- application icons, logos, BMC artwork, splash, styles, neutral/Hindi resource catalogs and raw resources;
- Application abstractions/models/exceptions/policies/validators;
- Domain models/enums;
- Infrastructure cryptography, SQLite, migration, backup, attachment, CSV, settings, generator, audit, TOTP, header and vault-session implementations;
- Shared application constants and storage limits.

Every tracked file under `tests/` is documented individually in [`TEST_SUITE_REFERENCE.md`](TEST_SUITE_REFERENCE.md), including `tests/Directory.Build.props`, all three test project files, and every unit/integration/UI-source test.

# 6. Top-level documentation files

- `docs/README.md` — canonical documentation hub and navigation entry point.
- `docs/QUICK_START.md` — safe first-use and contributor bootstrap path.
- `docs/USER_GUIDE.md` — detailed end-user workflows.
- `docs/FAQ.md` — product/security/build/release questions and answers.
- `docs/FEATURE_MATRIX.md` — implemented/platform-dependent/external/deferred feature status.
- `docs/UI_REFERENCE.md` — page/route/control/security-gate reference.
- `docs/CONFIGURATION_REFERENCE.md` — app ID, targets, packages, flags, settings/defaults/resources/toolchain configuration.
- `docs/COMPLETE_PROJECT_DOCUMENTATION.md` — consolidated end-to-end project reference.
- `docs/DEVELOPER_GUIDE.md` — development architecture, DI, security boundaries and extension rules.
- `docs/MAINTAINER_GUIDE.md` — repository/security/release/support ownership.
- `docs/API_REFERENCE.md` — internal Application contracts and Domain model API reference; CipherNest does not expose a server/network API.
- `docs/LIMITS_AND_DEFAULTS.md` — canonical human-readable safety ceilings, versions, defaults and timing bounds.
- `docs/PROJECT_GLOSSARY.md` — CipherNest terminology.
- `docs/ACCESSIBILITY.md` — semantic metadata, interface scaling/reduced-motion intent and manual assistive-technology gates.
- `docs/TESTING_GUIDE.md` — suite organization, local/hosted/device test responsibilities.
- `docs/TEST_PLAN.md` — release-oriented automated/manual test matrix.
- `docs/RELEASE_CHECKLIST.md` — evidence-backed release blocking checklist.
- `docs/TROUBLESHOOTING.md` — build/runtime troubleshooting without unsafe secret collection.
- `docs/NEXT_STEPS.md` — ordered remaining external evidence gates and later-version feature roadmap.
- `docs/DOCUMENTATION_MAINTENANCE.md` — documentation source-of-truth, wording, synchronization and historical-evidence rules.
- `docs/SOURCE_CODE_REFERENCE.md` — exhaustive production-source/asset file map added by the 2026-08-19 repository-wide documentation pass.
- `docs/TEST_SUITE_REFERENCE.md` — exhaustive automated-test file map added by the same pass.
- `docs/REPOSITORY_FILE_REFERENCE.md` — this exhaustive repository-level/root/automation/documentation map.

# 7. Architecture documentation

- `docs/architecture/ARCHITECTURE.md` — layer boundaries and dependency direction.
- `docs/architecture/DEPENDENCY_MAP.md` — solution/project/package/service ownership map.
- `docs/architecture/DATA_FLOW.md` — sensitive data, key, attachment, backup, CSV, TOTP URI, clipboard/share flows.
- `docs/architecture/SESSION_AND_CONCURRENCY.md` — transition gate, key leases, cancellation, destructive authorization and ordering.
- `docs/architecture/DATABASE.md` — SQLite schema, migrations, replacement, validation and recovery.
- `docs/architecture/LOCALIZATION.md` — neutral/Hindi resources, System/English/Hindi preference, reusable translation path and migration rules.

# 8. Security/privacy documentation

- `docs/security/THREAT_MODEL.md` — assets, attacker capabilities, mitigations, partial mitigations, non-goals and target-platform assumptions.
- `docs/security/CRYPTOGRAPHIC_DESIGN.md` — key hierarchy, Argon2id/AES-GCM design, wrappers, nonce/AAD/version assumptions and audit limitations.
- `docs/security/CRYPTOGRAPHY.md` — additional cryptography-focused implementation/usage reference retained by the repository.
- `docs/security/SESSION_SECURITY.md` — master/recovery/secondary authorization and lifecycle/clipboard relationship.
- `docs/security/DATA_LIFECYCLE.md` — protected/plaintext locations and what the application can/cannot erase.
- `docs/security/BIOMETRIC_UNLOCK.md` — secondary convenience-unlock design and platform limitations.
- `docs/security/TOTP.md` — TOTP seed/settings/code generation plus bounded TOTP-only setup-URI interoperability.
- `docs/security/SECURE_NOTES.md` — safe bounded note-markup subset.
- `docs/security/PASSPHRASE_GENERATOR.md` — generator security/entropy guidance.
- `docs/privacy/DIAGNOSTICS.md` — privacy-safe diagnostics policy and prohibited raw sensitive diagnostics.

# 9. Persisted/exported format documentation

- `docs/formats/VAULT_HEADER.md` — supported vault-header schemas, JSON bounds/versioning and pre-unwrap/pre-replacement validation.
- `docs/formats/VAULT_RECORDS.md` — encrypted item envelope/logical record model, identity binding and validation.
- `docs/formats/ATTACHMENTS.md` — `.cna` name/framing/chunks/metadata/preview/export boundaries.
- `docs/formats/ENCRYPTED_BACKUP.md` — `.cnbak` header/KDF/chunks/archive/restore/rollback contract.
- `docs/formats/CSV_TRANSFER.md` — generic CSV parsing/mapping/import and guarded plaintext export contract.

# 10. Build, release and operations documentation

## Setup

- `docs/setup/BUILD.md` — SDK/workload/toolchain prerequisites and target-specific commands.

## Releases

- `docs/releases/RELEASE_PROCESS.md` — candidate freeze, exact-head evidence, packaging/signing/policy/provenance/publication process.
- `docs/releases/PACKAGING.md` — target packaging/signing considerations.
- `docs/releases/REPRODUCIBLE_BUILDS.md` — build environment/provenance/reproducibility expectations.
- `docs/releases/STORE_LISTING_GUIDE.md` — accurate store positioning, screenshots, disclosures and funding-policy guidance.
- `docs/releases/UNRELEASED_HARDENING_2026_08_11.md` — preserved unreleased hardening notes from the August 11 source period.

## Operations

- `docs/operations/BACKUP_RECOVERY_RUNBOOK.md` — synthetic-data backup/restore verification and safe failure handling.
- `docs/operations/SECURITY_RESPONSE.md` — maintainer vulnerability-response process without requesting user secrets.

# 11. Branding documentation

- `docs/branding/ASSETS.md` — tracked logo/icon/BMC/splash asset inventory and usage rules.
- `docs/branding/BRANDING.md` — product naming, creator credit, visual identity and branding guidance.

# 12. Changelog/history documentation

- `docs/changelog/2026-08-13-post-baseline.md` — preserved post-baseline change record from the August 13 period.
- `docs/history/what_changed_through_2026_08_15.md` — archived full chronological ledger through August 15.
- `docs/history/what_changed_through_2026_08_18.md` — archived live ledger through the August 18 continuation.

Historical files are intentionally not rewritten to make them sound current.

# 13. Verification/evidence records

## Standing verification contract

- `docs/verification/CI_GATES.md` — canonical configured gate definitions and exact-head evidence rules.

## Historical and continuation-specific records

- `docs/verification/SECURITY_HARDENING_2026_08_11.md` — August 11 security-hardening verification record.
- `docs/verification/DOCUMENTATION_SUITE_2026_08_12.md` — August 12 documentation-suite verification record.
- `docs/verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md` — August 13 support/runtime hardening record.
- `docs/verification/HOSTED_CI_EVIDENCE_2026_08_13.md` — immutable historical hosted-CI evidence record.
- `docs/verification/CURRENT_HEAD_2026_08_13.md` — August 13 current-head verification snapshot.
- `docs/verification/POST_BASELINE_CHECKLIST_2026_08_13.md` — post-baseline validation checklist.
- `docs/verification/DOCUMENTATION_CONSOLIDATION_2026_08_14.md` — August 14 consolidation verification.
- `docs/verification/TOTP_AND_HINDI_LOCALIZATION_2026_08_14.md` — initial TOTP/Hindi localization verification record.
- `docs/verification/CSV_IMPORT_HARDENING_2026_08_15.md` — CSV parser/import hardening record.
- `docs/verification/SETTINGS_JSON_HARDENING_2026_08_15.md` — bounded settings JSON hardening record.
- `docs/verification/BACKUP_HEADER_HARDENING_2026_08_15.md` — strict backup-header hardening record.
- `docs/verification/VAULT_HEADER_HARDENING_2026_08_15.md` — strict vault-header hardening record.
- `docs/verification/ATTACHMENT_METADATA_HARDENING_2026_08_15.md` — Unicode/metadata attachment hardening record.
- `docs/verification/FINAL_REPOSITORY_HARDENING_2026_08_15.md` — August 15 repository hardening verification snapshot.
- `docs/verification/VERIFIED_MAIN_BASELINE_2026_08_15.md` — historical immutable verified-main baseline record.
- `docs/verification/REPOSITORY_AUDIT_2026_08_16.md` — repository-wide audit record after the August 15 baseline.
- `docs/verification/COMPLETE_DOCUMENTATION_2026_08_16.md` — source-to-document scope/evidence contract for the first complete-documentation expansion.
- `docs/verification/TOTP_URI_INTEROPERABILITY_2026_08_18.md` — bounded text-only TOTP setup-URI implementation/hardening verification contract.
- `docs/verification/TOTP_LOCALIZATION_2026_08_19.md` — TOTP fixed/dynamic/status localization verification contract.
- `docs/verification/AUTHENTICATION_LOCALIZATION_2026_08_19.md` — Unlock/onboarding/recovery authentication localization verification contract.
- `docs/verification/ABOUT_SECURITY_LOCALIZATION_2026_08_19.md` — About/security/privacy localization verification contract.
- `docs/verification/SETTINGS_SURFACE_LOCALIZATION_2026_08_19.md` — Settings fixed-surface localization verification contract.

A new repository-wide documentation verification record is added separately by this continuation rather than rewriting any of the historical files above.

# 14. Completeness maintenance contract

A change is documentation-complete only when all affected inventory layers remain synchronized:

- adding/removing/renaming a production file -> update `SOURCE_CODE_REFERENCE.md`;
- adding/removing/renaming a test file -> update `TEST_SUITE_REFERENCE.md`;
- adding/removing/renaming root/GitHub/script/documentation files -> update this file;
- changing a file's responsibility -> update its inventory description and the specialized canonical document;
- adding a canonical document -> link it from `docs/README.md` and, where useful, root `README.md`;
- adding a verification record that should remain mandatory -> update `DocumentationCoverageSourceTests.cs`;
- changing a persisted/security boundary -> synchronize security/format/limits/test/release documentation;
- changing CI -> synchronize `CI_GATES.md`, build/test docs and release evidence expectations.

Do not mark a release candidate documentation-complete merely because every path is listed. Statements must also match current executable behavior, security limitations, current versions/limits, and observable evidence.
