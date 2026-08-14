# CipherNest Documentation Hub

This directory is the canonical navigation point for CipherNest project documentation. CipherNest is a local-first .NET MAUI vault. Documentation must describe only behavior supported by the current source and must preserve the project's explicit security limitations: the project has not completed an independent professional security audit, managed strings cannot be deterministically erased, plaintext export leaves the protected vault boundary, and platform controls such as screenshots, biometrics, clipboard access, file sharing, secure storage, packaging, and lifecycle behavior require target-platform validation.

## Start here

- [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md) — consolidated end-to-end reference covering project identity, architecture, security model, storage, features, limits, build/test/release flow, support, and external validation gates.
- [`../README.md`](../README.md) — product overview, current capabilities, build entry points, repository/contact information, license, and high-level security status.
- [`USER_GUIDE.md`](USER_GUIDE.md) — end-user workflow from first launch through daily use, backup, restore, import/export, lock, trash, settings, and recovery.
- [`FAQ.md`](FAQ.md) — common user, security, platform, backup, build, CI, release, and support questions.
- [`DEVELOPER_GUIDE.md`](DEVELOPER_GUIDE.md) — repository layout, dependency direction, local development workflow, extension rules, review boundaries, and contributor workflow.
- [`MAINTAINER_GUIDE.md`](MAINTAINER_GUIDE.md) — day-to-day repository/security/release/support ownership rules.
- [`DOCUMENTATION_MAINTENANCE.md`](DOCUMENTATION_MAINTENANCE.md) — documentation governance, source-of-truth, synchronization, wording, and historical-preservation rules.
- [`API_REFERENCE.md`](API_REFERENCE.md) — application-layer public contracts and domain model reference.
- [`LIMITS_AND_DEFAULTS.md`](LIMITS_AND_DEFAULTS.md) — implemented safety ceilings, defaults, format versions, identifiers, and operational bounds.
- [`PROJECT_GLOSSARY.md`](PROJECT_GLOSSARY.md) — project-specific terminology used across code, UI, tests, and security documents.

## Architecture

- [`architecture/ARCHITECTURE.md`](architecture/ARCHITECTURE.md) — project/layer boundaries and dependency direction.
- [`architecture/DATABASE.md`](architecture/DATABASE.md) — SQLite schema, migration, replacement, snapshot, validation, and recovery boundaries.
- [`architecture/LOCALIZATION.md`](architecture/LOCALIZATION.md) — English-first localization architecture and extension rules.
- [`architecture/DATA_FLOW.md`](architecture/DATA_FLOW.md) — lifecycle of vault records, keys, attachments, backups, CSV data, clipboard data, and platform shares.
- [`architecture/SESSION_AND_CONCURRENCY.md`](architecture/SESSION_AND_CONCURRENCY.md) — session transition gate, key leases, cancellation, attachment mutation serialization, and destructive authorization.
- [`architecture/DEPENDENCY_MAP.md`](architecture/DEPENDENCY_MAP.md) — solution/project dependency map and ownership of major services.

## Security and privacy

- [`security/THREAT_MODEL.md`](security/THREAT_MODEL.md) — protected assets, attacker capabilities, partial mitigations, explicit non-goals, and platform limitations.
- [`security/CRYPTOGRAPHIC_DESIGN.md`](security/CRYPTOGRAPHIC_DESIGN.md) — implemented key hierarchy, Argon2id/AES-GCM design, record/attachment/backup formats, storage bounds, and audit status.
- [`security/BIOMETRIC_UNLOCK.md`](security/BIOMETRIC_UNLOCK.md) — secondary convenience-unlock design and platform behavior.
- [`security/SECURE_NOTES.md`](security/SECURE_NOTES.md) — safe note rendering subset and bounds.
- [`security/PASSPHRASE_GENERATOR.md`](security/PASSPHRASE_GENERATOR.md) — generator design and entropy guidance.
- [`security/SESSION_SECURITY.md`](security/SESSION_SECURITY.md) — master/recovery/secondary authorization roles, lock lifecycle, re-authentication, clipboard relationship, and sensitive-memory limits.
- [`security/DATA_LIFECYCLE.md`](security/DATA_LIFECYCLE.md) — where protected/plaintext data can exist and what CipherNest can or cannot erase.
- [`privacy/DIAGNOSTICS.md`](privacy/DIAGNOSTICS.md) — privacy-safe diagnostic policy.
- [`../PRIVACY.md`](../PRIVACY.md) — user-facing privacy notice.
- [`../SECURITY.md`](../SECURITY.md) — responsible disclosure/security contact policy.

## Formats and interoperability

- [`formats/VAULT_RECORDS.md`](formats/VAULT_RECORDS.md) — logical vault item model, encrypted record identity binding, validation, and storage limits.
- [`formats/ATTACHMENTS.md`](formats/ATTACHMENTS.md) — encrypted attachment naming/framing, chunk processing, metadata policy, preview, and export boundary.
- [`formats/ENCRYPTED_BACKUP.md`](formats/ENCRYPTED_BACKUP.md) — `.cnbak` container framing, KDF/header validation, encrypted chunks, bounded archive contents, restore validation, and rollback.
- [`formats/CSV_TRANSFER.md`](formats/CSV_TRANSFER.md) — explicit CSV mapping/import behavior and guarded plaintext export.

These format documents are implementation documentation, not promises of permanent compatibility beyond versions explicitly supported by the current code. Cryptographic or schema changes require versioning, migration/compatibility tests, threat-model updates, and review.

## Development, build, testing, and troubleshooting

- [`setup/BUILD.md`](setup/BUILD.md) — prerequisites, target-specific build commands, verification scripts, CI coverage, and funding-CTA build property.
- [`verification/CI_GATES.md`](verification/CI_GATES.md) — configured CI/local verification gates and evidence requirements.
- [`verification/SECURITY_HARDENING_2026_08_11.md`](verification/SECURITY_HARDENING_2026_08_11.md) — framing/resource/session/platform hardening verification addendum.
- [`verification/DOCUMENTATION_SUITE_2026_08_12.md`](verification/DOCUMENTATION_SUITE_2026_08_12.md) — required documentation/source-link/audit-wording completeness gates.
- [`verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md`](verification/SUPPORT_AND_RUNTIME_HARDENING_2026_08_13.md) — highlighted BMC support surface plus authenticated-record, session-race, hostile-backup-header, malformed-framing, Windows WinRT/AOT, SQLite dependency, and platform-toolchain gates.
- [`verification/HOSTED_CI_EVIDENCE_2026_08_13.md`](verification/HOSTED_CI_EVIDENCE_2026_08_13.md) — exact candidate/run evidence for 240 passing tests, formatting, Windows default/funding-disabled, Android, iOS simulator, Mac Catalyst, and CodeQL, with remaining device/store/audit limitations.
- [`verification/CURRENT_HEAD_2026_08_13.md`](verification/CURRENT_HEAD_2026_08_13.md) — explicitly marks the hosted baseline as historical after later source/test commits.
- [`verification/POST_BASELINE_CHECKLIST_2026_08_13.md`](verification/POST_BASELINE_CHECKLIST_2026_08_13.md) — rerun checklist for every post-baseline release candidate.
- [`verification/DOCUMENTATION_CONSOLIDATION_2026_08_14.md`](verification/DOCUMENTATION_CONSOLIDATION_2026_08_14.md) — repository-side scope and gate for the complete-documentation consolidation, including the required current-head rerun and external validation limits.
- [`TEST_PLAN.md`](TEST_PLAN.md) — complete automated/manual release test matrix.
- [`TESTING_GUIDE.md`](TESTING_GUIDE.md) — how tests are organized, how to add tests, what source tests prove, and what still requires devices.
- [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md) — build/runtime troubleshooting.
- [`../CONTRIBUTING.md`](../CONTRIBUTING.md) — contribution requirements.

## User experience, accessibility, and branding

- [`ACCESSIBILITY.md`](ACCESSIBILITY.md) — accessibility expectations, semantic metadata, larger-interface/reduced-motion behavior, and target-device checks.
- [`branding/ASSETS.md`](branding/ASSETS.md) — original vector source assets, generation rules, safe usage, and creator-credit guidance.
- [`releases/STORE_LISTING_GUIDE.md`](releases/STORE_LISTING_GUIDE.md) — accurate positioning, store disclosures, and synthetic-data screenshot rules.

## Release and operations

- [`RELEASE_CHECKLIST.md`](RELEASE_CHECKLIST.md) — release-blocking checklist.
- [`releases/PACKAGING.md`](releases/PACKAGING.md) — target-specific packaging/signing guidance.
- [`releases/REPRODUCIBLE_BUILDS.md`](releases/REPRODUCIBLE_BUILDS.md) — reproducibility expectations and environment capture.
- [`releases/RELEASE_PROCESS.md`](releases/RELEASE_PROCESS.md) — end-to-end candidate, evidence, signing, provenance, store-policy, and publication process.
- [`operations/BACKUP_RECOVERY_RUNBOOK.md`](operations/BACKUP_RECOVERY_RUNBOOK.md) — safe backup/restore verification and failure handling.
- [`operations/SECURITY_RESPONSE.md`](operations/SECURITY_RESPONSE.md) — maintainer response procedure for reported security issues without requesting user secrets.
- [`NEXT_STEPS.md`](NEXT_STEPS.md) — ordered post-source verification and future-version roadmap.
- [`../PROJECT_STATUS.md`](../PROJECT_STATUS.md) — implemented source scope, observed hosted verification, remaining external/hardware gates, and deferred features.
- [`../CHANGELOG.md`](../CHANGELOG.md) — release/unreleased change history.
- [`../what_changed.md`](../what_changed.md) — chronological implementation ledger.

## Legal, support, and third-party material

- [`../LICENSE`](../LICENSE) — GPL-3.0-or-later project license.
- [`../TERMS.md`](../TERMS.md) — current project terms/disclaimers.
- [`../SUPPORT.md`](../SUPPORT.md) — support channels and optional development-support link.
- [`../THIRD_PARTY_NOTICES.md`](../THIRD_PARTY_NOTICES.md) — dependency notice families/current central pins; exact resolved dependency/license review remains a release gate.

## Documentation maintenance rules

1. Documentation must track the current source rather than desired future features.
2. Never describe configured CI as passing until the exact commit has executed successfully; preserve the commit/run identifier with any hosted evidence.
3. Never describe CipherNest as independently audited, unhackable, military-grade, 100% secure, physically erasable, or able to recover a lost master passphrase from a server.
4. Mark cloud sync, accounts, collaboration, autofill, TOTP, Windows Hello, rich binary/PDF preview/scanning, pronounceable passwords, destructive wipe-on-failure, and complete additional language catalogs as deferred until implemented and reviewed.
5. Update `THREAT_MODEL.md`, `CRYPTOGRAPHIC_DESIGN.md`, format docs, tests, release gates, changelog, project status, and this index whenever a security-sensitive persistence/format/session behavior changes.
6. Use synthetic/demo data only in documentation examples and screenshots.
7. Keep credentials, passphrases, recovery keys, signing files, store tokens, private keys, crash-service tokens, and real vault contents out of documentation and Git history.
8. Keep public contact/project metadata centralized with `CipherNest.Shared.AppConstants` where application code needs it.
