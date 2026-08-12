# CipherNest Documentation Maintenance

CipherNest documentation is part of the security/product surface. Stale documentation can be harmful when it misstates recovery, deletion, biometrics, plaintext export, supported formats, or release evidence.

## 1. Canonical index

`docs/README.md` is the documentation navigation hub. New major documentation should be linked there in the same change series.

## 2. Documentation classes

### User-facing

- root `README.md`
- `USER_GUIDE.md`
- `SUPPORT.md`
- `PRIVACY.md`
- `TERMS.md`
- in-app Security/About wording

These must explain current behavior/limitations without internal implementation assumptions users do not need.

### Developer/architecture

- `DEVELOPER_GUIDE.md`
- `API_REFERENCE.md`
- `architecture/*`
- `LIMITS_AND_DEFAULTS.md`
- `PROJECT_GLOSSARY.md`

These should track actual contracts, dependencies, data flow, formats, limits, and extension rules.

### Security/privacy

- `SECURITY.md`
- `security/*`
- `privacy/*`
- operations security response

These must distinguish guarantees, partial mitigations, non-goals, and external/platform limits.

### Verification/release

- `TESTING_GUIDE.md`
- `TEST_PLAN.md`
- `verification/CI_GATES.md`
- `RELEASE_CHECKLIST.md`
- `releases/*`
- operations runbooks

These must never treat configured checks as passed evidence.

### Historical/status

- `CHANGELOG.md`
- `PROJECT_STATUS.md`
- `what_changed.md`
- `NEXT_STEPS.md`

These documents have different purposes and should not be collapsed into one summary.

## 3. Source-of-truth hierarchy

When documentation conflicts:

1. current executable source/tests define implemented behavior;
2. version/format constants define current numeric versions/limits where applicable;
3. security/architecture docs explain intended invariants;
4. user/release docs describe those implemented invariants appropriately.

A conflict should trigger a source/doc review, not silent rewriting of source behavior merely to match old prose.

## 4. Security wording rules

Never claim without evidence:

- unhackable;
- military-grade;
- 100% secure;
- independently audited;
- guaranteed physical erasure;
- server recovery of a forgotten master passphrase;
- all screenshots/clipboard copies can be prevented/removed;
- all CI/platform tests pass.

Current independent professional security audit status remains outstanding unless that actually changes.

## 5. Feature-status wording

Use one of:

- **implemented in source**;
- **configured but awaiting execution evidence**;
- **platform/device validation required**;
- **deferred/not implemented**.

Do not describe a future/deferred feature as current merely because an abstraction/placeholder is contemplated.

## 6. Format changes

If any persisted/exported format changes, update together:

- format implementation/version constants;
- compatibility/migration tests;
- `formats/*` document;
- `CRYPTOGRAPHIC_DESIGN.md` when cryptographic;
- `DATABASE.md` when schema/persistence;
- `THREAT_MODEL.md` if attack surface changes;
- `LIMITS_AND_DEFAULTS.md`;
- `TEST_PLAN.md`;
- `RELEASE_CHECKLIST.md`;
- `CHANGELOG.md`/status/ledger.

## 7. Contract/model changes

When changing Application interfaces or Domain records:

- update `API_REFERENCE.md`;
- update data-flow/dependency docs if ownership changes;
- update `USER_GUIDE.md` for visible behavior;
- add tests;
- review serialization compatibility.

## 8. Limit/default changes

When changing any resource/default setting:

- update implementation normalization/validation;
- update unit/integration boundary tests;
- update `LIMITS_AND_DEFAULTS.md`;
- update user/developer docs where visible;
- update security/format docs when the limit is security-relevant.

## 9. Platform-support changes

If a platform API or minimum target changes, reconcile:

- app project target/minimum versions;
- build guide;
- dependency map;
- biometric/screenshot/clipboard docs;
- testing/accessibility matrix;
- packaging/release process;
- store listing guidance.

## 10. Dependency updates

After package changes update, where needed:

- `DEPENDENCY_MAP.md` current central package table;
- `THIRD_PARTY_NOTICES.md`;
- release provenance;
- build/toolchain guidance when workloads/APIs change.

Do not manually duplicate dependency versions across many docs if one reference table plus package file link is sufficient.

## 11. CI/workflow changes

If workflows/scripts change:

- update `verification/CI_GATES.md`;
- update `TESTING_GUIDE.md`;
- update `setup/BUILD.md`;
- update `RELEASE_PROCESS.md`/checklist if release evidence changes.

A workflow deletion should not leave docs claiming the gate still exists.

## 12. User-flow changes

For onboarding/unlock/items/settings/backup/transfer/trash changes:

- update `USER_GUIDE.md`;
- update accessibility/security docs if the change affects secrets/auth/warnings;
- update screenshots/store guide if user-facing release positioning changes.

## 13. Error-message changes

Sensitive fixed error wording should remain privacy-safe.

Documentation examples must not encourage rendering/logging raw `Exception.Message` from filesystem/crypto/persistence/platform calls.

## 14. Examples and screenshots

Use only synthetic data.

Never place real:

- email credentials;
- tokens/passwords;
- recovery keys;
- card data;
- Wi-Fi passwords;
- server secrets;
- private documents;
- store/signing credentials

in documentation or images.

## 15. Link maintenance

After adding/moving documentation:

- update `docs/README.md`;
- update relevant root entry points;
- verify relative paths by fetching/opening them in repository review;
- avoid duplicate docs for the same canonical subject unless one is intentionally a user/developer/security view.

## 16. Historical preservation

`what_changed.md` is a chronological ledger. Append new continuations; do not shorten/remove previous historical sections merely to make the file easier to edit.

`CHANGELOG.md` may be curated by release semantics, but should preserve released history.

`PROJECT_STATUS.md` should remain current-state oriented rather than becoming an unbounded implementation diary.

## 17. Date/time/version accuracy

Use exact version numbers/current format constants from source. Use specific dates where a release/documentation record needs them.

Do not copy a historical “current” platform/store policy assertion forward without re-verification; store/platform rules are time-sensitive.

## 18. Documentation review checklist

Before committing docs:

- Does every statement match current source?
- Are deferred features clearly deferred?
- Are security limitations explicit?
- Are numeric limits/versions copied correctly?
- Are format byte orders/framing/AAD details correct?
- Are platform claims limited to implemented/verified scope?
- Are all examples synthetic?
- Are links valid?
- Is audit status accurate?
- Does the change need TEST_PLAN/RELEASE_CHECKLIST updates?
- Does `docs/README.md` need a link?

## 19. Release documentation freeze

Before release candidate tagging, verify:

- README/user docs match shipped UI;
- security docs match shipped formats/session model;
- build/testing docs match scripts/workflows;
- packaging/store guide matches target policy decision;
- changelog/status are current;
- third-party notices match resolved dependencies;
- audit status is current;
- no doc includes real secrets/private vulnerability details.

## 20. Maintainer ownership

A code change that makes documentation incorrect is not complete until the documentation is repaired. Documentation debt affecting security/recovery behavior should be treated as release-relevant, not cosmetic.
