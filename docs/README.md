# CipherNest Documentation Hub

This directory is the canonical navigation point for CipherNest documentation. The suite describes current source behavior, separates implementation from external validation and deferred work, and preserves explicit security limitations.

> **Security status:** CipherNest has **not** completed an independent professional security audit. Managed strings cannot be deterministically erased, explicit plaintext export leaves the protected vault boundary, and platform controls such as screenshots, biometrics, clipboard access, secure storage, lifecycle behavior, file sharing, signing, packaging, and store behavior require target-specific validation.

## ☕ Support CipherNest development

<p align="center">
  <a href="https://buymeacoffee.com/sanskarIN" title="Support CipherNest on Buy Me a Coffee">
    <img src="../src/CipherNest.App/Resources/Images/bmc_support.svg" alt="BMC — Support CipherNest" width="520" />
  </a>
</p>

Financial support is voluntary. It does not change feature access, security/privacy treatment, support priority, licensing, recovery behavior, or open-source rights. Distribution builds can disable the in-app funding surface independently of repository funding metadata.

# Start here

For the fastest orientation, use these documents in order:

1. [`QUICK_START.md`](QUICK_START.md) — safe end-user setup plus contributor bootstrap.
2. [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md) — exhaustive implemented/platform-dependent/external/deferred feature status.
3. [`UI_REFERENCE.md`](UI_REFERENCE.md) — page-by-page routes, controls, security gates, and navigation behavior.
4. [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md) — product/build/settings/toolchain/resource configuration.
5. [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md) — 52-section end-to-end reference for the entire project.
6. [`USER_GUIDE.md`](USER_GUIDE.md) — detailed everyday user workflows.
7. [`FAQ.md`](FAQ.md) — common product/security/build/release questions.

The public project overview is [`../README.md`](../README.md).

# Complete reference set

## User and product documentation

- [`QUICK_START.md`](QUICK_START.md) — first launch, vault creation, items, TOTP including bounded setup-URI import/copy, attachments, backups, settings, contributor bootstrap.
- [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md) — current feature state and deliberately deferred functionality.
- [`UI_REFERENCE.md`](UI_REFERENCE.md) — Startup, Onboarding, Unlock, Vault, Item Editor, Generator, Audit, Trash, Settings, Security Info, Transfer, About, Developer, and route behavior.
- [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md) — application ID, target frameworks, build flags, packages, preferences, defaults, limits, toolchain, and verification scripts.
- [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md) — consolidated project architecture/security/storage/UI/build/test/release/operations reference.
- [`USER_GUIDE.md`](USER_GUIDE.md) — end-user workflows from first launch through recovery/deletion.
- [`FAQ.md`](FAQ.md) — user, contributor, security, platform, backup, CI, and release FAQ.
- [`PROJECT_GLOSSARY.md`](PROJECT_GLOSSARY.md) — project-specific terminology.

## Developer and maintainer documentation

- [`DEVELOPER_GUIDE.md`](DEVELOPER_GUIDE.md) — repository layout, dependency direction, DI, extension rules, tests, and review boundaries.
- [`MAINTAINER_GUIDE.md`](MAINTAINER_GUIDE.md) — repository/security/release/support ownership.
- [`API_REFERENCE.md`](API_REFERENCE.md) — Application contracts and Domain model reference; this is an internal source API, not a network API.
- [`LIMITS_AND_DEFAULTS.md`](LIMITS_AND_DEFAULTS.md) — authoritative current resource ceilings, defaults, versions, and timing bounds.
- [`DOCUMENTATION_MAINTENANCE.md`](DOCUMENTATION_MAINTENANCE.md) — documentation source-of-truth, synchronization, historical-evidence, and wording rules.
- [`../CONTRIBUTING.md`](../CONTRIBUTING.md) — contribution requirements.

# Architecture

- [`architecture/ARCHITECTURE.md`](architecture/ARCHITECTURE.md) — project/layer boundaries and dependency direction.
- [`architecture/DEPENDENCY_MAP.md`](architecture/DEPENDENCY_MAP.md) — solution/project/package/service ownership.
- [`architecture/DATA_FLOW.md`](architecture/DATA_FLOW.md) — sensitive data/key/attachment/backup/CSV/TOTP setup-URI/clipboard/share flows.
- [`architecture/SESSION_AND_CONCURRENCY.md`](architecture/SESSION_AND_CONCURRENCY.md) — session transition gate, key leases, cancellation, attachment serialization, destructive authorization, recovery ordering.
- [`architecture/DATABASE.md`](architecture/DATABASE.md) — SQLite schema, migrations, replacement, snapshot, validation, recovery.
- [`architecture/LOCALIZATION.md`](architecture/LOCALIZATION.md) — neutral-English/Hindi resource-backed localization architecture and extension rules.

# Security and privacy

- [`security/THREAT_MODEL.md`](security/THREAT_MODEL.md) — assets, attacker capabilities, mitigations, partial mitigations, non-goals, scenarios, and platform limitations.
- [`security/CRYPTOGRAPHIC_DESIGN.md`](security/CRYPTOGRAPHIC_DESIGN.md) — key hierarchy, Argon2id/AES-GCM design, KDF bounds, formats, nonce/AAD assumptions, audit status.
- [`security/SESSION_SECURITY.md`](security/SESSION_SECURITY.md) — master/recovery/secondary authorization, lock lifecycle, re-authentication, clipboard relationship, sensitive-memory limits.
- [`security/DATA_LIFECYCLE.md`](security/DATA_LIFECYCLE.md) — where plaintext/protected data can exist and what CipherNest can/cannot erase.
- [`security/BIOMETRIC_UNLOCK.md`](security/BIOMETRIC_UNLOCK.md) — secondary convenience-unlock design and platform limitations.
- [`security/TOTP.md`](security/TOTP.md) — encrypted Base32 seed/settings, RFC-compatible code generation, bounded `otpauth://totp/...` text import/formatting, input bounds, clipboard behavior, and factor-separation limitations.
- [`security/SECURE_NOTES.md`](security/SECURE_NOTES.md) — bounded Markdown-like safe subset and HTML-neutralization policy.
- [`security/PASSPHRASE_GENERATOR.md`](security/PASSPHRASE_GENERATOR.md) — password/passphrase generation and entropy guidance.
- [`privacy/DIAGNOSTICS.md`](privacy/DIAGNOSTICS.md) — privacy-safe diagnostic policy.
- [`../SECURITY.md`](../SECURITY.md) — responsible disclosure/security contact policy.
- [`../PRIVACY.md`](../PRIVACY.md) — user-facing privacy notice.

# Formats and interoperability

- [`formats/VAULT_HEADER.md`](formats/VAULT_HEADER.md) — exact supported local vault-header schemas, parser bounds, compatibility, pre-unwrap/pre-replacement validation.
- [`formats/VAULT_RECORDS.md`](formats/VAULT_RECORDS.md) — logical encrypted item model, identity binding, TOTP parameters, validation/resource rules.
- [`formats/ATTACHMENTS.md`](formats/ATTACHMENTS.md) — `.cna` naming/framing, chunk processing, metadata validation, preview/export boundary.
- [`formats/ENCRYPTED_BACKUP.md`](formats/ENCRYPTED_BACKUP.md) — `.cnbak` framing, KDF/header checks, chunks, bounded ZIP/archive, restore validation/rollback.
- [`formats/CSV_TRANSFER.md`](formats/CSV_TRANSFER.md) — explicit CSV mapping/import and guarded plaintext export.
- [`security/TOTP.md`](security/TOTP.md) — dedicated bounded single-item TOTP `otpauth://totp/...` interoperability. This is intentionally separate from generic CSV transfer.

These are implementation documents, not promises of permanent compatibility beyond versions explicitly supported by current source. Incompatible schema/format changes require explicit versioning, migration/compatibility tests, threat-model review, and release documentation.

# Build, test, verification, and troubleshooting

- [`setup/BUILD.md`](setup/BUILD.md) — prerequisites and platform-specific build commands.
- [`TEST_PLAN.md`](TEST_PLAN.md) — automated/manual release test matrix.
- [`TESTING_GUIDE.md`](TESTING_GUIDE.md) — how tests are organized and when source/device testing is required.
- [`verification/CI_GATES.md`](verification/CI_GATES.md) — configured CI/local gates and evidence rules.
- [`verification/COMPLETE_DOCUMENTATION_2026_08_16.md`](verification/COMPLETE_DOCUMENTATION_2026_08_16.md) — source-to-document scope and gate for the full documentation expansion.
- [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md) — build/runtime troubleshooting.

## Current immutable pre-documentation implementation baseline

The complete-documentation expansion is grounded in exact source baseline:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

For that exact commit:

- **346 UnitTests passed**;
- **98 IntegrationTests passed**;
- **111 UI/source tests passed**;
- **555 total passed, 0 failed, 0 skipped**;
- analyzer-enabled core test builds completed without build warnings/errors;
- core formatting passed;
- Windows default Release passed;
- Windows `CipherNestEnableFundingLink=false` Release passed;
- Android Release passed;
- iOS simulator Release passed;
- Mac Catalyst Release passed;
- CodeQL v4 passed after analyzable core and MAUI application builds.

Recorded run IDs:

- CipherNest CI: `31937127961`
- CodeQL: `31937127900`

This evidence belongs only to that immutable implementation SHA. The August 18 TOTP setup-URI implementation creates a newer exact head and requires its own configured CI/CodeQL evidence before being described as exact-head release-candidate verified.

## Historical verification records

Historical records remain intentionally preserved with their original commit/run context:

- [`verification/SECURITY_HARDENING_2026_08_11.md`](verification/SECURITY_HARDENING_2026_08_11.md)
- [`verification/DOCUMENTATION_SUITE_2026_08_12.md`](verification/DOCUMENTATION_SUITE_2026_08_12.md)
- [`verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md`](verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md)
- [`verification/HOSTED_CI_EVIDENCE_2026_08_13.md`](verification/HOSTED_CI_EVIDENCE_2026_08_13.md) — historical 240-test exact candidate.
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
- [`verification/VERIFIED_MAIN_BASELINE_2026_08_15.md`](verification/VERIFIED_MAIN_BASELINE_2026_08_15.md) — historical 554-test exact baseline.
- [`verification/REPOSITORY_AUDIT_2026_08_16.md`](verification/REPOSITORY_AUDIT_2026_08_16.md)

# User experience, accessibility, localization, and branding

- [`ACCESSIBILITY.md`](ACCESSIBILITY.md) — semantic metadata, dynamic typography, reduced motion, responsive UI, assistive-technology release checks.
- [`UI_REFERENCE.md`](UI_REFERENCE.md) — current page-by-page UI reference.
- [`architecture/LOCALIZATION.md`](architecture/LOCALIZATION.md) — System/English/Hindi preference and fallback architecture.
- [`branding/ASSETS.md`](branding/ASSETS.md) — original vector branding, BMC badge, generation/usage/creator-credit rules.
- [`releases/STORE_LISTING_GUIDE.md`](releases/STORE_LISTING_GUIDE.md) — accurate positioning, disclosures, screenshots, feature graphic, funding-policy guidance.

# Release and operations

- [`RELEASE_CHECKLIST.md`](RELEASE_CHECKLIST.md) — release-blocking checklist.
- [`releases/RELEASE_PROCESS.md`](releases/RELEASE_PROCESS.md) — candidate freeze, evidence, signing, provenance, policy, publication.
- [`releases/PACKAGING.md`](releases/PACKAGING.md) — target-specific packaging/signing.
- [`releases/REPRODUCIBLE_BUILDS.md`](releases/REPRODUCIBLE_BUILDS.md) — environment capture/reproducibility expectations.
- [`releases/STORE_LISTING_GUIDE.md`](releases/STORE_LISTING_GUIDE.md) — store positioning/policy/disclosure guidance.
- [`operations/BACKUP_RECOVERY_RUNBOOK.md`](operations/BACKUP_RECOVERY_RUNBOOK.md) — safe backup/restore verification and failure handling.
- [`operations/SECURITY_RESPONSE.md`](operations/SECURITY_RESPONSE.md) — maintainer security-report handling without requesting user secrets.
- [`NEXT_STEPS.md`](NEXT_STEPS.md) — ordered external release gates and future-version roadmap.
- [`../PROJECT_STATUS.md`](../PROJECT_STATUS.md) — current source/evidence/deferred status.
- [`../CHANGELOG.md`](../CHANGELOG.md) — release/unreleased history.
- [`../what_changed.md`](../what_changed.md) — chronological implementation/documentation ledger.

# Legal, support, and third-party material

- [`../LICENSE`](../LICENSE) — GPL-3.0-or-later.
- [`../TERMS.md`](../TERMS.md) — project terms/disclaimers.
- [`../SUPPORT.md`](../SUPPORT.md) — support contacts and optional BMC link.
- [`../THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md) — dependency notice families; exact release graph/license review remains a release gate.

# Documentation maintenance rules

1. Documentation tracks current source, not desired features.
2. Never describe configured CI as passing without exact immutable candidate/run evidence.
3. Never describe CipherNest as independently audited, unhackable, military-grade, 100% secure, physically erasable, or capable of server-reset recovery when source does not provide it.
4. Keep cloud sync/accounts/collaboration/autofill/Windows Hello/rich binary-PDF preview/scanning/pronounceable passwords/wipe-on-failure and complete-unmigrated language surfaces deferred until implemented and reviewed. TOTP local generation, bounded TOTP `otpauth://totp/...` text import/formatting, and the reviewed Hindi resource-backed catalog are implemented; TOTP QR/camera enrollment, HOTP interoperability, provider/autofill integration, and complete UI translation remain deferred.
5. Update affected threat/crypto/session/format/API/limits/tests/release/status/docs when security-sensitive behavior changes.
6. Use synthetic/demo data only in examples/screenshots.
7. Never place credentials, passphrases, recovery keys, TOTP seeds/codes/setup URIs, signing files, store tokens, private keys, crash-service tokens, or real vault contents in documentation/Git history.
8. Keep application-consumed public project/contact metadata centralized with `CipherNest.Shared.AppConstants`.
9. Preserve historical verification records as historical records rather than rewriting them for later commits.
10. Add/update documentation regression tests when canonical files or entry points change.
