# CipherNest Documentation Maintenance

CipherNest documentation is part of the security/product surface. Stale documentation can be harmful when it misstates recovery, deletion, biometrics, plaintext export, supported formats, limits, localization status, or release evidence.

# 1. Canonical navigation

`docs/README.md` is the documentation navigation hub. A new canonical document must be linked there in the same change series. Public entry points should also be linked from the root `README.md` when appropriate.

# 2. Exhaustive file inventories

CipherNest maintains three complementary file-level references:

- `REPOSITORY_FILE_REFERENCE.md` — root files, GitHub/community automation, scripts, all documentation/evidence, legal/support/status/history artifacts;
- `SOURCE_CODE_REFERENCE.md` — every tracked production file below `src/`, including project files, C#, XAML, platform manifests/entry points and application resources;
- `TEST_SUITE_REFERENCE.md` — shared test configuration, all test project files, and every unit/integration/UI-source test file.

File-inventory maintenance is mandatory:

1. adding/removing/renaming/repurposing a production file requires `SOURCE_CODE_REFERENCE.md` in the same change series;
2. adding/removing/renaming/repurposing a test file requires `TEST_SUITE_REFERENCE.md`;
3. adding/removing/renaming/repurposing a root, `.github`, script or documentation file requires `REPOSITORY_FILE_REFERENCE.md`;
4. adding a new canonical file also requires the relevant hub/index link;
5. adding a verification record that should remain part of the current mandatory documentation contract requires an automated documentation-source guard update.

The inventories describe file responsibilities and boundaries; they should not duplicate source line-for-line. Executable source and focused tests remain authoritative.

# 3. Documentation classes

## User-facing

- root `README.md`
- `QUICK_START.md`
- `USER_GUIDE.md`
- `FAQ.md`
- `SUPPORT.md`
- `PRIVACY.md`
- `TERMS.md`
- in-app Security/About wording

These explain current behavior and limitations without unnecessary internal implementation assumptions.

## Developer/architecture

- `DEVELOPER_GUIDE.md`
- `MAINTAINER_GUIDE.md`
- `API_REFERENCE.md`
- `REPOSITORY_FILE_REFERENCE.md`
- `SOURCE_CODE_REFERENCE.md`
- `TEST_SUITE_REFERENCE.md`
- `architecture/*`
- `LIMITS_AND_DEFAULTS.md`
- `PROJECT_GLOSSARY.md`

These track actual contracts, ownership, data flow, formats, limits, dependencies and extension rules.

## Security/privacy

- root `SECURITY.md`
- root `PRIVACY.md`
- `security/*`
- `privacy/*`
- operations security response

These distinguish guarantees, partial mitigations, non-goals, plaintext boundaries, and platform/external limits.

## Verification/release

- `TESTING_GUIDE.md`
- `TEST_PLAN.md`
- `verification/CI_GATES.md`
- dated `verification/*` records
- `RELEASE_CHECKLIST.md`
- `releases/*`
- operations runbooks

These must never treat configured checks as passed evidence.

## Historical/status

- root `CHANGELOG.md`
- root `PROJECT_STATUS.md`
- root `what_changed.md`
- `history/*`
- `NEXT_STEPS.md`

These have different purposes and should not be collapsed into one undifferentiated summary.

# 4. Source-of-truth hierarchy

When documentation conflicts:

1. current executable source and focused tests define implemented behavior;
2. version/format/resource constants define current numeric versions and hard limits;
3. specialized security/architecture/format documentation explains intended invariants;
4. user/release documentation presents those implemented invariants appropriately;
5. historical verification records remain evidence for their recorded immutable candidates only.

A conflict triggers a source/documentation review, not silent source changes merely to match old prose.

# 5. Security wording rules

Never claim without evidence:

- unhackable;
- military-grade;
- 100% secure;
- independently audited;
- guaranteed managed-memory erasure;
- guaranteed physical-media erasure;
- guaranteed clipboard-history/synchronization erasure;
- server recovery/reset of a forgotten master passphrase;
- prevention of every screenshot/task-preview path;
- universal authenticator/provider compatibility;
- all CI/platform tests pass for a newer head because an older SHA passed.

Current independent professional security-audit status remains outstanding unless that actually changes.

# 6. Feature-status wording

Use explicit status language:

- **implemented in source**;
- **configured but awaiting exact-head execution evidence**;
- **platform/device validation required**;
- **external/manual review required**;
- **deferred/not implemented**.

Do not describe a contemplated abstraction or future feature as current behavior.

For TOTP, bounded local `otpauth://totp/...` text import/formatting and local code generation are implemented. QR/camera enrollment, HOTP/counter interoperability, provider enrollment/autofill, and universal compatibility remain separate/unclaimed unless later implemented and reviewed.

For localization, reviewed resource-backed neutral-English/Hindi surfaces must be described precisely; do not claim complete application translation while user-facing literals remain unmigrated.

# 7. Persisted/exported format changes

If a persisted/exported format changes, update together where relevant:

- implementation/version constants;
- compatibility/migration tests;
- the matching `formats/*` document;
- `CRYPTOGRAPHIC_DESIGN.md` when cryptographic;
- `DATABASE.md` when schema/persistence related;
- `THREAT_MODEL.md` when attack surface changes;
- `LIMITS_AND_DEFAULTS.md`;
- `TEST_PLAN.md`;
- `RELEASE_CHECKLIST.md`;
- `CHANGELOG.md`, `PROJECT_STATUS.md`, and `what_changed.md` where release/current-state semantics change;
- the affected file inventory.

# 8. Contract/model changes

When Application interfaces or Domain records change:

- update `API_REFERENCE.md`;
- update data-flow/dependency docs if ownership changes;
- update `USER_GUIDE.md` for visible behavior;
- add/adjust tests;
- review serialization compatibility;
- update `SOURCE_CODE_REFERENCE.md` if responsibility/path changed.

# 9. Limit/default changes

When a resource/default setting changes:

- update implementation normalization/validation;
- update unit/integration boundary tests;
- update `LIMITS_AND_DEFAULTS.md`;
- update user/developer docs where visible;
- update security/format docs when security relevant;
- update configuration docs when user/build configurable.

# 10. Platform-support changes

If a platform API, target framework or minimum target changes, reconcile:

- app project target/minimum versions;
- build guide;
- dependency map;
- biometric/screenshot/clipboard docs;
- testing/accessibility matrix;
- packaging/release process;
- store listing guidance;
- `SOURCE_CODE_REFERENCE.md` for added/removed platform files.

# 11. Dependency updates

After package changes update, where needed:

- `Directory.Packages.props`;
- `architecture/DEPENDENCY_MAP.md` current dependency ownership;
- `THIRD_PARTY_NOTICES.md`;
- release provenance;
- build/toolchain guidance when workloads/APIs change.

Do not manually duplicate package versions across many documents when one canonical table/file link is sufficient.

# 12. CI/workflow changes

If workflows/scripts change:

- update `verification/CI_GATES.md`;
- update `TESTING_GUIDE.md`;
- update `setup/BUILD.md`;
- update `RELEASE_PROCESS.md`/checklist if release evidence changes;
- update `REPOSITORY_FILE_REFERENCE.md` for added/removed/renamed workflow/script files.

A deleted workflow must not leave prose claiming the gate still exists.

# 13. User-flow changes

For onboarding, unlock, items, settings, backup, transfer, trash, generator, audit or About/security changes:

- update `USER_GUIDE.md` and/or `UI_REFERENCE.md`;
- update accessibility/security docs when secrets/auth/warnings are affected;
- update localization docs when resource-backed coverage changes;
- update store/screenshots guidance when release positioning changes.

# 14. Error-message changes

Sensitive fixed error wording should remain privacy-safe. Documentation examples must not encourage rendering/logging raw `Exception.Message`, paths, stack traces, vault contents, credentials, recovery material, TOTP seeds/codes/setup URIs, or other sensitive state from filesystem/crypto/persistence/platform failures.

# 15. Examples and screenshots

Use only synthetic data. Never place real:

- passwords/passphrases;
- recovery keys;
- TOTP seeds/codes/setup URIs;
- email or service credentials;
- tokens/API keys;
- payment data;
- Wi-Fi passwords;
- server/private keys;
- private documents;
- signing/store credentials

in documentation, screenshots, test fixtures, or Git history.

# 16. Link maintenance

After adding/moving documentation:

- update `docs/README.md`;
- update relevant root/public entry points;
- update `REPOSITORY_FILE_REFERENCE.md`;
- verify relative paths during repository review;
- avoid parallel competing canonical documents unless one is intentionally a distinct user/developer/security view.

# 17. Historical preservation

`what_changed.md` is the live chronological ledger. Append continuations while it remains manageable; when an intentional archive rollover is performed, preserve the complete prior ledger under `docs/history/` and make the live file link the archive rather than deleting history.

`CHANGELOG.md` may be curated by release semantics but should preserve released history.

`PROJECT_STATUS.md` remains current-state oriented rather than becoming an unbounded implementation diary.

Dated verification records remain immutable historical/evidence records except for narrowly justified factual corrections that preserve original candidate context.

# 18. Date/time/version accuracy

Use exact version numbers/current format constants from source and specific dates for dated evidence records. Do not copy a historical “current” platform/store policy assertion forward without re-verification; platform/store rules are time-sensitive.

# 19. Documentation review checklist

Before committing documentation, check:

- Does every current-behavior statement match executable source/tests?
- Are deferred/platform/external features clearly separated?
- Are security/plaintext/erasure limitations explicit?
- Are numeric limits/versions copied correctly?
- Are format byte order/framing/AAD/version details correct where discussed?
- Are platform claims limited to implemented/verified scope?
- Are all examples synthetic?
- Are links valid?
- Is independent audit status accurate?
- Is exact-head evidence distinguished from historical evidence?
- Does the change require TEST_PLAN/RELEASE_CHECKLIST updates?
- Does `docs/README.md` need a link?
- Does one of the three exhaustive file references need an update?
- Does an automated documentation guard need an update?

# 20. Release documentation freeze

Before release-candidate tagging, verify:

- README/user docs match shipped UI;
- security docs match shipped formats/session/plaintext boundaries;
- build/testing docs match actual scripts/workflows;
- packaging/store guide matches current target policy decision;
- changelog/status/ledger are current;
- third-party notices match resolved dependencies;
- independent audit status is accurate;
- file inventories match the candidate tree;
- no document includes real secrets/private vulnerability details;
- exact candidate SHA and observable gate evidence are recorded without inheritance from older SHAs.

# 21. Maintainer ownership

A code, test, workflow, resource, platform, or documentation change that makes canonical documentation incorrect is not complete until the documentation is repaired. Documentation debt affecting security, recovery, plaintext boundaries, persisted formats, release evidence, or repository-file coverage is release-relevant rather than cosmetic.
