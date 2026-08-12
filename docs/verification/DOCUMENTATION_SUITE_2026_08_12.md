# CipherNest documentation suite gates — 2026-08-12

This document records the documentation-completeness gates added during the complete project-documentation pass. It complements `CI_GATES.md`, `TESTING_GUIDE.md`, `TEST_PLAN.md`, `RELEASE_CHECKLIST.md`, and `DOCUMENTATION_MAINTENANCE.md`.

## Required documentation areas

The current repository must keep non-empty documentation for:

- canonical documentation hub;
- end-user guide;
- developer guide;
- maintainer guide;
- documentation governance;
- Application API/domain model reference;
- limits/defaults/version reference;
- glossary;
- accessibility;
- testing;
- architecture, dependency, data-flow, database, localization, and session/concurrency design;
- threat model, cryptographic design, biometrics, secure notes, passphrase generator, session security, and sensitive-data lifecycle;
- vault-record, attachment, encrypted-backup, and CSV formats;
- diagnostics/privacy;
- CI/verification;
- backup/recovery and security-response operations;
- build/troubleshooting;
- packaging/reproducibility/store listing/release process;
- release checklist, roadmap, project status, changelog, and chronological progress ledger;
- contribution, support, privacy, security, terms, third-party notices, and license entry points.

## Automated source regression gate

`tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs` requires the major documentation files to exist and be non-empty.

It also requires:

- root `README.md` to link the canonical documentation hub and primary user/developer/security/format/release entry points;
- `docs/README.md` to link every major documentation area introduced by the documentation pass;
- primary README/security/threat/cryptographic entry points to retain an explicit independent-audit disclaimer.

This test is a repository/source completeness guard. It does not verify that every statement is semantically correct at runtime.

## Content accuracy gates

Before release, reviewers must compare documentation against current source for:

- public interface signatures;
- domain-model fields/enums;
- app routes/platform targets/minimum versions;
- DI registrations and project ownership;
- cryptographic KDF/key/nonce/tag/passphrase bounds;
- vault/storage/item/attachment/CSV/backup resource limits;
- attachment and backup framing/AAD/endian details;
- database schema/migration/replacement behavior;
- session/key-lease/cancellation/destructive-authorization ordering;
- settings defaults/normalization;
- plaintext export and managed-memory limitations;
- biometric/platform support/fallback behavior;
- deferred features and audit status.

A source change that invalidates documentation must update the affected canonical documents in the same change series.

## Link integrity gate

Review all relative links from:

- root `README.md`;
- `docs/README.md`;
- `CONTRIBUTING.md`;
- `SECURITY.md`;
- `SUPPORT.md`;
- `PRIVACY.md`;
- architecture/build/release entry documents.

The automated source test verifies presence of major files but does not parse every Markdown link. Broken links remain a release-documentation defect.

## Historical preservation gate

`what_changed.md` is a chronological implementation ledger. New continuation sections must be appended without shortening/removing prior history.

`CHANGELOG.md` must retain released history while describing current Unreleased changes.

`PROJECT_STATUS.md` remains current-state oriented and must continue distinguishing source implementation from external execution/hardware/audit gates.

## Security wording gate

Documentation must not claim as a supported fact that CipherNest is:

- independently audited;
- unhackable;
- military-grade;
- 100% secure;
- capable of guaranteed physical data erasure;
- able to recover a forgotten master passphrase from a server.

It is acceptable—and required in several documents—to mention these phrases while explicitly rejecting/limiting those claims.

## Test/evidence wording gate

Documentation must distinguish:

- tests/workflows/scripts present in source;
- checks configured in CI;
- checks actually executed/passing for an exact candidate;
- device/emulator/manual checks;
- independent professional security review.

A configured workflow with no result is not a pass.

## Deferred-feature gate

Current documentation must keep these out of the completed-feature set until separately implemented/reviewed:

- cloud sync/accounts/collaboration/server vault storage;
- browser/app autofill;
- TOTP seed storage/generation;
- Windows Hello convenience unlock;
- rich binary/PDF preview/document scanning beyond bounded text preview;
- pronounceable-password generation;
- destructive automatic wipe after failed attempts;
- complete Hindi/additional translation catalogs.

## Synthetic-data gate

Documentation examples, screenshots, fixtures, and reproduction steps must use synthetic/demo data only. Do not commit real vaults, passphrases, recovery keys, payment credentials, private documents, signing secrets, or store/API tokens.

## Release evidence

For an exact release candidate, documentation completion should be evidenced by:

1. `CipherNest.UiTests` including `DocumentationCoverageSourceTests` passing;
2. review of `docs/README.md` link coverage;
3. source-to-doc review of changed contracts/formats/limits/security assumptions;
4. changelog/project-status/release-checklist synchronization;
5. verification that audit/platform/store-policy wording is current;
6. confirmation that no private vulnerability details or real user secrets entered docs/history.

The connected GitHub editing environment can commit these gates but cannot itself prove the current candidate's .NET/MAUI test execution, device behavior, store compliance, or independent security review.
