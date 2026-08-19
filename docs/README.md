# CipherNest Documentation Hub

This directory is the canonical navigation point for CipherNest documentation. The suite describes current source behavior, preserves historical evidence without rewriting it, and separates implementation from physical-device, store, signing, accessibility, interoperability, and independent-review gates.

> **Security status:** CipherNest has **not** completed an independent professional security audit. Managed strings cannot be deterministically erased; explicit plaintext copy/export/share leaves the protected vault boundary; platform controls such as screenshots, biometrics, clipboard history/synchronization, secure storage, lifecycle behavior, signing, packaging and store behavior require target-specific validation.

## ☕ Optional project support

Repository and in-app funding support is optional. It does not change feature access, security/privacy treatment, licensing, recovery behavior, support priority or open-source rights. Distribution builds can disable the in-app funding surface independently of repository funding metadata.

# Start here

1. [`QUICK_START.md`](QUICK_START.md) — safe first-use and contributor bootstrap.
2. [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md) — implemented, platform-dependent, external-validation and deferred status.
3. [`UI_REFERENCE.md`](UI_REFERENCE.md) — every current page/route and major security-sensitive control.
4. [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md) — targets, build flags, packages, preferences, limits and toolchain configuration.
5. [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md) — consolidated end-to-end architecture/security/storage/UI/build/test/release reference.
6. [`REPOSITORY_FILE_REFERENCE.md`](REPOSITORY_FILE_REFERENCE.md) — complete root/automation/documentation file inventory.
7. [`SOURCE_CODE_REFERENCE.md`](SOURCE_CODE_REFERENCE.md) — every production source/platform/resource file and its responsibility.
8. [`TEST_SUITE_REFERENCE.md`](TEST_SUITE_REFERENCE.md) — every automated test file and the behavior it protects.
9. [`USER_GUIDE.md`](USER_GUIDE.md) — detailed user workflows.
10. [`FAQ.md`](FAQ.md) — common product/security/build/release questions.

The public project overview remains [`../README.md`](../README.md).

# Complete file-level documentation

The repository-wide file documentation contract is deliberately split into three references so nothing is hidden in an unreadable single table:

- [`REPOSITORY_FILE_REFERENCE.md`](REPOSITORY_FILE_REFERENCE.md) covers root metadata, `.github`, scripts, legal/support/status files and every documentation/evidence file.
- [`SOURCE_CODE_REFERENCE.md`](SOURCE_CODE_REFERENCE.md) covers every tracked file below `src/`, including `.csproj`, C#, XAML, code-behind, platform manifests/entry points, localization resources, styles, icons, images, splash and raw resources.
- [`TEST_SUITE_REFERENCE.md`](TEST_SUITE_REFERENCE.md) covers `tests/Directory.Build.props`, all three test projects and every unit/integration/UI-source test file.

A new/renamed tracked file must update its appropriate inventory in the same change series. See [`DOCUMENTATION_MAINTENANCE.md`](DOCUMENTATION_MAINTENANCE.md).

# User and product documentation

- [`QUICK_START.md`](QUICK_START.md) — first launch, vault creation, items, TOTP setup-URI import/copy, attachments, backup/restore, settings and contributor bootstrap.
- [`USER_GUIDE.md`](USER_GUIDE.md) — complete everyday workflow reference.
- [`FAQ.md`](FAQ.md) — product/security/platform/backup/build/release FAQ.
- [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md) — exhaustive feature state and explicit non-goals/deferred scope.
- [`UI_REFERENCE.md`](UI_REFERENCE.md) — Startup, Onboarding, Unlock, Vault, Item Editor, Generator, Audit, Trash, Settings, Security Info, Transfer, About and Developer surfaces.
- [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md) — application ID, target frameworks, build flags, package pins, settings/defaults/bounds, resources and toolchain.
- [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md) — consolidated project reference.
- [`PROJECT_GLOSSARY.md`](PROJECT_GLOSSARY.md) — project-specific terminology.
- [`ACCESSIBILITY.md`](ACCESSIBILITY.md) — semantics, larger-interface/reduced-motion intent, responsive behavior and manual assistive-technology gates.

# Developer and maintainer documentation

- [`DEVELOPER_GUIDE.md`](DEVELOPER_GUIDE.md) — solution layout, dependency direction, DI, security boundaries and extension rules.
- [`MAINTAINER_GUIDE.md`](MAINTAINER_GUIDE.md) — repository/security/release/support ownership.
- [`API_REFERENCE.md`](API_REFERENCE.md) — internal Application contracts and Domain models; this is not a network API.
- [`LIMITS_AND_DEFAULTS.md`](LIMITS_AND_DEFAULTS.md) — authoritative human-readable ceilings, versions, defaults and timing bounds.
- [`DOCUMENTATION_MAINTENANCE.md`](DOCUMENTATION_MAINTENANCE.md) — documentation governance/source-of-truth rules.
- [`REPOSITORY_FILE_REFERENCE.md`](REPOSITORY_FILE_REFERENCE.md) — exhaustive repository file map outside production/test trees.
- [`SOURCE_CODE_REFERENCE.md`](SOURCE_CODE_REFERENCE.md) — production file-by-file source reference.
- [`TEST_SUITE_REFERENCE.md`](TEST_SUITE_REFERENCE.md) — automated test file-by-file reference.
- [`../CONTRIBUTING.md`](../CONTRIBUTING.md) — contribution requirements.
- [`../DECISIONS.md`](../DECISIONS.md) — preserved project/architecture decisions.

# Architecture

- [`architecture/ARCHITECTURE.md`](architecture/ARCHITECTURE.md) — project/layer boundaries and dependency direction.
- [`architecture/DEPENDENCY_MAP.md`](architecture/DEPENDENCY_MAP.md) — projects, packages and service ownership.
- [`architecture/DATA_FLOW.md`](architecture/DATA_FLOW.md) — key/data/attachment/backup/CSV/TOTP setup-URI/clipboard/share flows.
- [`architecture/SESSION_AND_CONCURRENCY.md`](architecture/SESSION_AND_CONCURRENCY.md) — session transition gate, key leases, cancellation, destructive authorization and ordering.
- [`architecture/DATABASE.md`](architecture/DATABASE.md) — SQLite schema, migration, replacement, snapshot, validation and recovery.
- [`architecture/LOCALIZATION.md`](architecture/LOCALIZATION.md) — System/English/Hindi resources, reusable translation path and migration rules.

# Security and privacy

- [`security/THREAT_MODEL.md`](security/THREAT_MODEL.md) — assets, attackers, mitigations, partial mitigations and non-goals.
- [`security/CRYPTOGRAPHIC_DESIGN.md`](security/CRYPTOGRAPHIC_DESIGN.md) — key hierarchy, Argon2id/AES-GCM, wrappers, nonce/AAD and version assumptions.
- [`security/CRYPTOGRAPHY.md`](security/CRYPTOGRAPHY.md) — additional cryptography implementation/usage reference.
- [`security/SESSION_SECURITY.md`](security/SESSION_SECURITY.md) — master/recovery/secondary authorization and lock lifecycle.
- [`security/DATA_LIFECYCLE.md`](security/DATA_LIFECYCLE.md) — protected/plaintext locations and deletion limitations.
- [`security/BIOMETRIC_UNLOCK.md`](security/BIOMETRIC_UNLOCK.md) — optional secondary convenience unlock and platform limits.
- [`security/TOTP.md`](security/TOTP.md) — encrypted TOTP seed/settings, local generation, bounded `otpauth://totp/...` text import/formatting and factor-separation limitations.
- [`security/SECURE_NOTES.md`](security/SECURE_NOTES.md) — bounded Markdown-like safe subset and HTML neutralization.
- [`security/PASSPHRASE_GENERATOR.md`](security/PASSPHRASE_GENERATOR.md) — password/passphrase generation and entropy guidance.
- [`privacy/DIAGNOSTICS.md`](privacy/DIAGNOSTICS.md) — privacy-safe diagnostics policy.
- [`../SECURITY.md`](../SECURITY.md) — responsible disclosure/security contact policy.
- [`../PRIVACY.md`](../PRIVACY.md) — public privacy notice.

# Formats and interoperability

- [`formats/VAULT_HEADER.md`](formats/VAULT_HEADER.md) — exact supported local header schemas, bounds and compatibility validation.
- [`formats/VAULT_RECORDS.md`](formats/VAULT_RECORDS.md) — authenticated encrypted item record model and validation/resource rules.
- [`formats/ATTACHMENTS.md`](formats/ATTACHMENTS.md) — `.cna` naming/framing/chunk/metadata/preview/export contract.
- [`formats/ENCRYPTED_BACKUP.md`](formats/ENCRYPTED_BACKUP.md) — `.cnbak` framing/KDF/header/chunk/archive/restore/rollback contract.
- [`formats/CSV_TRANSFER.md`](formats/CSV_TRANSFER.md) — explicit CSV mapping/import and guarded plaintext export.
- [`security/TOTP.md`](security/TOTP.md) — dedicated bounded single-item `otpauth://totp/...` interoperability, intentionally separate from generic CSV.

Persisted/exported format changes require explicit versioning/compatibility review and synchronized tests/docs.

# Build, testing and troubleshooting

- [`setup/BUILD.md`](setup/BUILD.md) — prerequisites and target-specific commands.
- [`TESTING_GUIDE.md`](TESTING_GUIDE.md) — automated/source/device test responsibilities.
- [`TEST_PLAN.md`](TEST_PLAN.md) — release-oriented automated/manual matrix.
- [`TEST_SUITE_REFERENCE.md`](TEST_SUITE_REFERENCE.md) — exact test-source inventory.
- [`verification/CI_GATES.md`](verification/CI_GATES.md) — configured CI/local gates and immutable evidence rules.
- [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md) — build/runtime troubleshooting.

## Immutable historical verified baseline

The complete-documentation work retains the historical exact implementation baseline `8566980ff981b8b4072f9010ec7b7ba54aba051e`. For that SHA only, repository records document 346 UnitTests, 98 IntegrationTests and 111 UI/source tests: **555 total passed, 0 failed, 0 skipped**, plus configured Windows default/funding-disabled, Android, iOS simulator, Mac Catalyst and CodeQL success.

Those results are historical evidence for that SHA, not automatic evidence for the later August 18/19 heads.

# Verification records

## Standing contract

- [`verification/CI_GATES.md`](verification/CI_GATES.md) — configured gate/evidence semantics.

## Historical records

- [`verification/SECURITY_HARDENING_2026_08_11.md`](verification/SECURITY_HARDENING_2026_08_11.md)
- [`verification/DOCUMENTATION_SUITE_2026_08_12.md`](verification/DOCUMENTATION_SUITE_2026_08_12.md)
- [`verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md`](verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md)
- [`verification/HOSTED_CI_EVIDENCE_2026_08_13.md`](verification/HOSTED_CI_EVIDENCE_2026_08_13.md)
- [`verification/CURRENT_HEAD_2026_08_13.md`](verification/CURRENT_HEAD_2026_08_13.md)
- [`verification/POST_BASELINE_CHECKLIST_2026_08_13.md`](verification/POST_BASELINE_CHECKLIST_2026_08_13.md)
- [`verification/DOCUMENTATION_CONSOLIDATION_2026_08_14.md`](verification/DOCUMENTATION_CONSOLIDATION_2026_08_14.md)
- [`verification/TOTP_AND_HINDI_LOCALIZATION_2026_08_14.md`](verification/TOTP_AND_HINDI_LOCALIZATION_2026_08_14.md)
- [`verification/CSV_IMPORT_HARDENING_2026_08_15.md`](verification/CSV_IMPORT_HARDENING_2026_08_15.md)
- [`verification/SETTINGS_JSON_HARDENING_2026_08_15.md`](verification/SETTINGS_JSON_HARDENING_2026_08_15.md)
- [`verification/BACKUP_HEADER_HARDENING_2026_08_15.md`](verification/BACKUP_HEADER_HARDENING_2026_08_15.md)
- [`verification/VAULT_HEADER_HARDENING_2026_08_15.md`](verification/VAULT_HEADER_HARDENING_2026_08_15.md)
- [`verification/ATTACHMENT_METADATA_HARDENING_2026_08_15.md`](verification/ATTACHMENT_METADATA_HARDENING_2026_08_15.md)
- [`verification/FINAL_REPOSITORY_HARDENING_2026_08_15.md`](verification/FINAL_REPOSITORY_HARDENING_2026_08_15.md)
- [`verification/VERIFIED_MAIN_BASELINE_2026_08_15.md`](verification/VERIFIED_MAIN_BASELINE_2026_08_15.md)
- [`verification/REPOSITORY_AUDIT_2026_08_16.md`](verification/REPOSITORY_AUDIT_2026_08_16.md)
- [`verification/COMPLETE_DOCUMENTATION_2026_08_16.md`](verification/COMPLETE_DOCUMENTATION_2026_08_16.md)

## Current continuation records

- [`verification/TOTP_URI_INTEROPERABILITY_2026_08_18.md`](verification/TOTP_URI_INTEROPERABILITY_2026_08_18.md) — bounded text-only TOTP setup-URI implementation/hardening.
- [`verification/TOTP_LOCALIZATION_2026_08_19.md`](verification/TOTP_LOCALIZATION_2026_08_19.md) — TOTP fixed/dynamic/status localization.
- [`verification/AUTHENTICATION_LOCALIZATION_2026_08_19.md`](verification/AUTHENTICATION_LOCALIZATION_2026_08_19.md) — Unlock/onboarding/recovery authentication localization.
- [`verification/ABOUT_SECURITY_LOCALIZATION_2026_08_19.md`](verification/ABOUT_SECURITY_LOCALIZATION_2026_08_19.md) — About/security/privacy localization.
- [`verification/SETTINGS_SURFACE_LOCALIZATION_2026_08_19.md`](verification/SETTINGS_SURFACE_LOCALIZATION_2026_08_19.md) — remaining fixed Settings surface localization.
- `verification/REPOSITORY_WIDE_DOCUMENTATION_2026_08_19.md` — repository-wide file inventory/documentation verification record added by the current continuation.

# Release and operations

- [`RELEASE_CHECKLIST.md`](RELEASE_CHECKLIST.md) — release blocking checklist.
- [`releases/RELEASE_PROCESS.md`](releases/RELEASE_PROCESS.md) — candidate freeze, evidence, signing, provenance, policy and publication.
- [`releases/PACKAGING.md`](releases/PACKAGING.md) — target-specific packaging/signing.
- [`releases/REPRODUCIBLE_BUILDS.md`](releases/REPRODUCIBLE_BUILDS.md) — environment/provenance/reproducibility expectations.
- [`releases/STORE_LISTING_GUIDE.md`](releases/STORE_LISTING_GUIDE.md) — accurate store positioning, screenshots, disclosures and funding-policy guidance.
- [`releases/UNRELEASED_HARDENING_2026_08_11.md`](releases/UNRELEASED_HARDENING_2026_08_11.md) — preserved unreleased hardening notes.
- [`operations/BACKUP_RECOVERY_RUNBOOK.md`](operations/BACKUP_RECOVERY_RUNBOOK.md) — safe synthetic-data backup/restore verification.
- [`operations/SECURITY_RESPONSE.md`](operations/SECURITY_RESPONSE.md) — vulnerability handling without requesting user secrets.
- [`NEXT_STEPS.md`](NEXT_STEPS.md) — remaining external gates and later-version roadmap.

# Branding, history, legal and status

- [`branding/ASSETS.md`](branding/ASSETS.md) — tracked visual asset inventory/usage rules.
- [`branding/BRANDING.md`](branding/BRANDING.md) — naming/creator/visual identity rules.
- [`changelog/2026-08-13-post-baseline.md`](changelog/2026-08-13-post-baseline.md) — preserved historical change record.
- [`history/what_changed_through_2026_08_15.md`](history/what_changed_through_2026_08_15.md) — archived ledger through August 15.
- [`history/what_changed_through_2026_08_18.md`](history/what_changed_through_2026_08_18.md) — archived ledger through August 18.
- [`../PROJECT_STATUS.md`](../PROJECT_STATUS.md) — current-state snapshot.
- [`../CHANGELOG.md`](../CHANGELOG.md) — release/unreleased history.
- [`../what_changed.md`](../what_changed.md) — live chronological ledger.
- [`../LICENSE`](../LICENSE) — GPL-3.0-or-later.
- [`../TERMS.md`](../TERMS.md) — project terms/disclaimers.
- [`../SUPPORT.md`](../SUPPORT.md) — support channels.
- [`../THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md) — dependency notice families.

# Documentation governance

1. Current executable source/tests define implemented behavior; prose must follow them.
2. A configured workflow is not passing evidence until the exact immutable candidate run is observed.
3. Never claim CipherNest is unhackable, military-grade, 100% secure, independently audited, capable of guaranteed managed-memory/physical-media erasure, or capable of server-reset recovery when source/evidence does not support it.
4. TOTP local generation and bounded TOTP-only `otpauth://totp/...` text import/formatting are implemented; QR/camera enrollment, HOTP, provider/autofill integration and universal compatibility are not claimed.
5. Reviewed Hindi resources exist for migrated surfaces; do not claim complete app translation while unmigrated literals remain.
6. Use only synthetic data in tests/examples/screenshots; never commit real credentials, passphrases, recovery keys, TOTP seeds/codes/setup URIs, private documents, signing material or store tokens.
7. Preserve historical verification files with their original SHA/run context.
8. Update the three file-level inventories whenever files are added, removed, renamed or materially repurposed.
