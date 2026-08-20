# CipherNest Complete Repository File Reference

This is the canonical repository-level file map for CipherNest. Together with [`SOURCE_CODE_REFERENCE.md`](SOURCE_CODE_REFERENCE.md) and [`TEST_SUITE_REFERENCE.md`](TEST_SUITE_REFERENCE.md), it documents every tracked file class without forcing production implementation details, test intent, and repository operations into one unreadable artifact.

Inventory baseline: Trash localization continuation on 2026-08-20, synchronized through the August 19 ledger archive. Later files must be added to the appropriate inventory in the same change series.

> Historical verification records belong to their recorded immutable candidates. Their pass counts/run IDs never transfer automatically to a newer head.

# 1. Inventory ownership

Every tracked file belongs to exactly one of these documentation inventories:

1. **This file** — root files, GitHub/community automation, verification scripts, documentation, legal/support/status/history material.
2. **`SOURCE_CODE_REFERENCE.md`** — every tracked production file below `src/`, including project files, C#, XAML, manifests, localization, styles, icons/images/splash/raw resources.
3. **`TEST_SUITE_REFERENCE.md`** — `tests/Directory.Build.props`, all test project files, and every unit/integration/UI-source test file, including `RepositoryDocumentationInventorySourceTests.cs`.

Directories are organizational containers, not omitted files.

# 2. Root files

- `.editorconfig` — repository editor/text-formatting rules.
- `.gitignore` — generated build/IDE/platform artifact exclusions.
- `CipherNest.slnx` — solution definition for production and test projects.
- `Directory.Build.props` — repository-wide .NET compiler/analyzer/build properties.
- `Directory.Packages.props` — central NuGet version management and dependency source of truth.
- `global.json` — .NET SDK selection/roll-forward contract.
- `README.md` — public project overview, current feature/security scope, build/evidence summary, docs entry points, optional support.
- `LICENSE` — GPL-3.0-or-later license.
- `CODE_OF_CONDUCT.md` — community/contributor conduct expectations.
- `CONTRIBUTING.md` — contribution, review, security-sensitive change, test and documentation requirements.
- `SECURITY.md` — vulnerability-reporting and security-claim boundaries.
- `PRIVACY.md` — user-facing local-first/privacy/plaintext-boundary disclosures.
- `TERMS.md` — project terms/disclaimers.
- `SUPPORT.md` — support channels and optional development-support path.
- `THIRD_PARTY_NOTICES.md` — dependency/license notice families; exact release graph review remains a release gate.
- `CHANGELOG.md` — curated release/unreleased feature and hardening history.
- `PROJECT_STATUS.md` — current implementation/evidence/deferred-state snapshot.
- `DECISIONS.md` — retained architecture/product/security decisions and rationale.
- `what_changed.md` — live chronological implementation/documentation ledger; archived ledgers remain under `docs/history/`.

# 3. GitHub/community automation

## Repository/community configuration

- `.github/FUNDING.yml` — optional repository funding metadata.
- `.github/PULL_REQUEST_TEMPLATE.md` — pull-request review prompts/checklist.
- `.github/dependabot.yml` — automated dependency-update configuration.
- `.github/ISSUE_TEMPLATE/bug_report.yml` — structured bug report without requiring vault secrets.
- `.github/ISSUE_TEMPLATE/feature_request.yml` — structured feature request.
- `.github/ISSUE_TEMPLATE/config.yml` — issue-template chooser/contact configuration.

## Workflows

- `.github/workflows/dotnet-desktop.yml` — primary CI for configured core and target build gates.
- `.github/workflows/codeql.yml` — CodeQL analysis including analyzable application build setup.
- `.github/workflows/dependency-review.yml` — pull-request dependency/security review where applicable.

Workflow presence means **configured**, not **passed**. See `docs/verification/CI_GATES.md`.

# 4. Local verification/build scripts

- `build/scripts/verify.ps1` — PowerShell aggregate verification entry point under build tooling.
- `build/scripts/verify.sh` — POSIX aggregate verification entry point.
- `scripts/verify-core.ps1` — PowerShell core restore/build/test/format verification.
- `scripts/verify-core.sh` — POSIX core restore/build/test/format verification.
- `scripts/verify-windows.ps1` — Windows Release verification, including the configured funding-disabled variant.
- `scripts/verify-android.sh` — Android Release verification.
- `scripts/verify-apple.sh` — iOS simulator and Mac Catalyst verification.

Canonical command/evidence explanations live in `docs/setup/BUILD.md`, `docs/TESTING_GUIDE.md`, and `docs/verification/CI_GATES.md`.

# 5. Production and test source inventories

Every tracked `src/` file is individually documented in [`SOURCE_CODE_REFERENCE.md`](SOURCE_CODE_REFERENCE.md). It covers application composition, pages/ViewModels/services, all Application/Domain/Infrastructure/Shared files, platform manifests/entry points, localization catalogs, styles, logos/icons/splash and raw resources.

Every tracked `tests/` file is individually documented in [`TEST_SUITE_REFERENCE.md`](TEST_SUITE_REFERENCE.md). It covers shared test configuration, all three test projects, all unit/integration/UI-source tests, and the repository-documentation inventory guard.

# 6. Top-level documentation

- `docs/README.md` — canonical documentation hub.
- `docs/QUICK_START.md` — safe first-use and contributor bootstrap.
- `docs/USER_GUIDE.md` — detailed end-user workflows.
- `docs/FAQ.md` — product/security/build/release FAQ.
- `docs/FEATURE_MATRIX.md` — implemented/platform-dependent/external/deferred status.
- `docs/UI_REFERENCE.md` — current page/route/control/security-gate reference.
- `docs/CONFIGURATION_REFERENCE.md` — app ID, targets, packages, flags, settings/defaults/resources/toolchain.
- `docs/COMPLETE_PROJECT_DOCUMENTATION.md` — consolidated end-to-end project reference.
- `docs/DEVELOPER_GUIDE.md` — development architecture, DI, security boundaries, extension rules.
- `docs/MAINTAINER_GUIDE.md` — repository/security/release/support ownership.
- `docs/API_REFERENCE.md` — internal Application contracts/Domain models, not a network API.
- `docs/LIMITS_AND_DEFAULTS.md` — canonical human-readable safety ceilings, versions/defaults/timing bounds.
- `docs/PROJECT_GLOSSARY.md` — project terminology.
- `docs/ACCESSIBILITY.md` — semantic/accessibility intent and manual target gates.
- `docs/TESTING_GUIDE.md` — test organization and local/hosted/device responsibilities.
- `docs/TEST_PLAN.md` — release-oriented automated/manual matrix.
- `docs/RELEASE_CHECKLIST.md` — release-blocking checklist.
- `docs/TROUBLESHOOTING.md` — build/runtime troubleshooting without unsafe secret collection.
- `docs/NEXT_STEPS.md` — remaining release evidence gates and later-version roadmap.
- `docs/DOCUMENTATION_MAINTENANCE.md` — documentation source-of-truth/synchronization/evidence rules.
- `docs/SOURCE_CODE_REFERENCE.md` — exhaustive production source/platform/resource file map.
- `docs/TEST_SUITE_REFERENCE.md` — exhaustive automated-test file map.
- `docs/REPOSITORY_FILE_REFERENCE.md` — this exhaustive root/automation/documentation file map.

# 7. Architecture documentation

- `docs/architecture/ARCHITECTURE.md` — layer boundaries/dependency direction.
- `docs/architecture/DEPENDENCY_MAP.md` — projects/packages/service ownership.
- `docs/architecture/DATA_FLOW.md` — sensitive data/key/attachment/backup/CSV/TOTP URI/clipboard/share flows.
- `docs/architecture/SESSION_AND_CONCURRENCY.md` — transition gate, key leases, cancellation, destructive authorization/order.
- `docs/architecture/DATABASE.md` — SQLite schema/migration/replacement/validation/recovery.
- `docs/architecture/LOCALIZATION.md` — System/English/Hindi primary-plus-feature resources and localization migration rules.

# 8. Security/privacy documentation

- `docs/security/THREAT_MODEL.md` — assets, attackers, mitigations, partial mitigations, non-goals.
- `docs/security/CRYPTOGRAPHIC_DESIGN.md` — key hierarchy, Argon2id/AES-GCM, wrappers, nonce/AAD/version assumptions and audit status.
- `docs/security/CRYPTOGRAPHY.md` — additional cryptographic implementation/usage reference.
- `docs/security/SESSION_SECURITY.md` — master/recovery/secondary authorization and lock lifecycle.
- `docs/security/DATA_LIFECYCLE.md` — protected/plaintext locations and erasure limitations.
- `docs/security/BIOMETRIC_UNLOCK.md` — secondary convenience-unlock design/platform limits.
- `docs/security/TOTP.md` — encrypted TOTP seed/settings, generation, bounded TOTP-only setup-URI interoperability.
- `docs/security/SECURE_NOTES.md` — bounded safe-note markup subset.
- `docs/security/PASSPHRASE_GENERATOR.md` — generator security/entropy guidance.
- `docs/privacy/DIAGNOSTICS.md` — privacy-safe diagnostic policy.

# 9. Persisted/exported format documentation

- `docs/formats/VAULT_HEADER.md` — supported header schemas/version/bounds/compatibility checks.
- `docs/formats/VAULT_RECORDS.md` — authenticated encrypted item model/identity binding/validation.
- `docs/formats/ATTACHMENTS.md` — `.cna` naming/framing/chunks/metadata/preview/export boundaries.
- `docs/formats/ENCRYPTED_BACKUP.md` — `.cnbak` header/KDF/chunks/archive/restore/rollback contract.
- `docs/formats/CSV_TRANSFER.md` — generic CSV mapping/import and guarded plaintext export.

# 10. Setup, releases and operations

- `docs/setup/BUILD.md` — SDK/workload/toolchain prerequisites and target commands.
- `docs/releases/RELEASE_PROCESS.md` — candidate freeze, evidence, signing, provenance, policy, publication.
- `docs/releases/PACKAGING.md` — target packaging/signing considerations.
- `docs/releases/REPRODUCIBLE_BUILDS.md` — environment/provenance/reproducibility expectations.
- `docs/releases/STORE_LISTING_GUIDE.md` — accurate store copy/screenshots/disclosures/funding-policy guidance.
- `docs/releases/UNRELEASED_HARDENING_2026_08_11.md` — preserved August 11 unreleased hardening notes.
- `docs/operations/BACKUP_RECOVERY_RUNBOOK.md` — synthetic-data backup/restore verification and safe failure handling.
- `docs/operations/SECURITY_RESPONSE.md` — vulnerability-response process without requesting user secrets.

# 11. Branding, changelog and history

- `docs/branding/ASSETS.md` — tracked icon/logo/BMC/splash inventory/usage rules.
- `docs/branding/BRANDING.md` — product naming, creator credit and visual identity.
- `docs/changelog/2026-08-13-post-baseline.md` — preserved August 13 post-baseline change record.
- `docs/history/what_changed_through_2026_08_15.md` — archived chronological ledger through August 15.
- `docs/history/what_changed_through_2026_08_18.md` — archived live ledger through August 18.
- `docs/history/what_changed_through_2026_08_19.md` — byte-identical archive of the live ledger through the August 19 tracked-file documentation hardening continuation, preserved before the August 20 ledger rollover.

Historical files remain historical; they are not rewritten to impersonate current evidence.

# 12. Verification/evidence records

## Standing contract

- `docs/verification/CI_GATES.md` — configured gate definitions and exact-head evidence rules.

## Historical/continuation records

- `docs/verification/SECURITY_HARDENING_2026_08_11.md` — August 11 security hardening.
- `docs/verification/DOCUMENTATION_SUITE_2026_08_12.md` — August 12 documentation-suite verification.
- `docs/verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md` — August 13 support/runtime hardening.
- `docs/verification/HOSTED_CI_EVIDENCE_2026_08_13.md` — immutable historical hosted-CI evidence.
- `docs/verification/CURRENT_HEAD_2026_08_13.md` — August 13 current-head snapshot.
- `docs/verification/POST_BASELINE_CHECKLIST_2026_08_13.md` — post-baseline validation checklist.
- `docs/verification/DOCUMENTATION_CONSOLIDATION_2026_08_14.md` — August 14 consolidation verification.
- `docs/verification/TOTP_AND_HINDI_LOCALIZATION_2026_08_14.md` — initial TOTP/Hindi localization verification.
- `docs/verification/CSV_IMPORT_HARDENING_2026_08_15.md` — CSV import/parser hardening.
- `docs/verification/SETTINGS_JSON_HARDENING_2026_08_15.md` — bounded settings JSON hardening.
- `docs/verification/BACKUP_HEADER_HARDENING_2026_08_15.md` — strict backup-header hardening.
- `docs/verification/VAULT_HEADER_HARDENING_2026_08_15.md` — strict vault-header hardening.
- `docs/verification/ATTACHMENT_METADATA_HARDENING_2026_08_15.md` — attachment Unicode/metadata hardening.
- `docs/verification/FINAL_REPOSITORY_HARDENING_2026_08_15.md` — August 15 repository hardening snapshot.
- `docs/verification/VERIFIED_MAIN_BASELINE_2026_08_15.md` — historical immutable verified-main baseline.
- `docs/verification/REPOSITORY_AUDIT_2026_08_16.md` — repository-wide audit after the August 15 baseline.
- `docs/verification/COMPLETE_DOCUMENTATION_2026_08_16.md` — first complete-documentation source/evidence contract.
- `docs/verification/TOTP_URI_INTEROPERABILITY_2026_08_18.md` — bounded text-only TOTP setup-URI implementation/hardening.
- `docs/verification/TOTP_LOCALIZATION_2026_08_19.md` — TOTP fixed/dynamic/status localization.
- `docs/verification/AUTHENTICATION_LOCALIZATION_2026_08_19.md` — Unlock/onboarding/recovery authentication localization.
- `docs/verification/ABOUT_SECURITY_LOCALIZATION_2026_08_19.md` — About/security/privacy localization.
- `docs/verification/SETTINGS_SURFACE_LOCALIZATION_2026_08_19.md` — remaining fixed Settings localization.
- `docs/verification/REPOSITORY_WIDE_DOCUMENTATION_2026_08_19.md` — exhaustive file-inventory/documentation verification contract.

# 13. Completeness maintenance contract

A change is documentation-complete only when affected inventories and canonical behavior documents remain synchronized:

- production file added/removed/renamed/repurposed -> update `SOURCE_CODE_REFERENCE.md`;
- test file added/removed/renamed/repurposed -> update `TEST_SUITE_REFERENCE.md`;
- root/GitHub/script/documentation file added/removed/renamed/repurposed -> update this file;
- canonical document added -> link it from `docs/README.md`, and root `README.md` when it is a public entry point;
- verification record intended as mandatory current evidence -> update the documentation source guard;
- persisted/security boundary changed -> synchronize threat/crypto/format/limits/test/release documentation;
- CI changed -> synchronize `CI_GATES.md`, build/test docs, and release evidence expectations.

File-path coverage alone does not make a release verified. Documentation must match executable behavior, security limitations, versions/limits and observable exact-head evidence.
