# CipherNest Security Response Runbook

This maintainer runbook complements the public `SECURITY.md`. It describes how to receive, investigate, fix, validate, disclose, and release a security issue without asking reporters/users for real secrets.

## 1. Core response principles

- Protect users/reporter privacy first.
- Do not request or collect real vault contents/credentials when synthetic reproduction can work.
- Preserve evidence without spreading exploit details unnecessarily.
- Reproduce with synthetic/disposable data.
- Fix the underlying security boundary, not only the visible symptom.
- Add regression coverage.
- Update threat/design/format/release documentation when the issue changes assumptions.
- Avoid stronger security claims until independent evidence supports them.

## 2. Public reporting channel

Current public policy asks reporters **not** to publish exploitable details in a public GitHub issue and to email:

```text
sanskarin@outlook.in
```

Useful non-secret report information:

- affected CipherNest version/commit;
- platform/OS;
- synthetic reproduction steps;
- expected versus actual behavior;
- security impact;
- suggested mitigation if known.

## 3. Never request these from a reporter

Do not ask for:

- real master passphrase;
- recovery material;
- backup passphrase;
- biometric secondary secret;
- DEK/KEK/private cryptographic keys;
- real `ciphernest.db` unless there is an exceptional private process and no safer alternative—and default is still not to request it;
- real `.cnbak` plus its credential;
- decrypted attachments/private documents;
- plaintext CSV containing actual secrets;
- signing keys/store tokens/certificates;
- screenshots that expose credentials.

Prefer a synthetic fixture generated solely to reproduce the bug.

## 4. Initial triage

Record privately:

```text
report identifier
date received
reporter contact (minimum necessary)
affected version/commit
platform
suspected component
reproducibility status
potential confidentiality/integrity/availability impact
whether exploit details are public already
whether current release users are exposed
```

Do not copy sensitive report content into public issue trackers before coordinated disclosure is appropriate.

## 5. Classify affected boundary

Common categories:

### Cryptography

- KDF resource validation;
- key wrapping/unwrapping;
- nonce/AAD/version handling;
- authenticated record/attachment/backup framing;
- key/session memory lifetime.

### Authentication/session

- master/recovery/secondary role confusion;
- stale authorization;
- lock/unlock race;
- protected-item bypass;
- destructive authorization/cancellation.

### Persistence/restore

- SQLite migration/replacement;
- DB/WAL/SHM recovery;
- malformed authenticated metadata;
- path clobbering/traversal;
- resource exhaustion.

### Attachment/preview/export

- chunk tampering;
- storage-name validation;
- plaintext staging/remnants;
- unsafe preview parsing/rendering.

### CSV/parser

- parser resource exhaustion;
- malformed input bypass;
- secret leakage in warnings/logging;
- plaintext-export authorization.

### Platform privacy

- clipboard;
- screenshot protection;
- secure storage/biometrics;
- file picker/share/lifecycle behavior.

### Diagnostics/supply chain

- sensitive logging;
- dependency compromise/vulnerability;
- CI/release/signing provenance.

## 6. Reproduce safely

Build a minimal synthetic reproduction:

- use a disposable temp app-data directory/profile/device;
- generate fake credentials/items/documents;
- avoid network/upload unless the vulnerability itself concerns a reviewed network boundary;
- preserve the exact candidate/release environment details;
- keep exploit proof private until disclosure timing is decided.

If the issue depends on malformed bytes, create a minimal synthetic malformed container rather than modifying a user's real vault.

## 7. Determine affected versions

Identify:

- first known vulnerable version/commit if possible;
- current main status;
- released versions affected;
- whether data-format compatibility affects patch feasibility;
- whether a mitigation exists without changing the persisted format.

Do not guess ranges publicly before confirming them.

## 8. Severity considerations

Consider separately:

- confidentiality impact on locked/unlocked vaults;
- integrity/tamper impact;
- availability/data-loss impact;
- need for local access versus privileged/root access;
- user interaction required;
- whether attacker controls a file/backup/CSV/attachment;
- whether attack works on copied encrypted data offline;
- whether exploit bypasses documented limitations or only demonstrates one;
- scale/reliability of exploitation.

A documented limitation may still be serious if UI/documentation falsely promises protection beyond that limitation.

## 9. Immediate mitigation decisions

Possible actions, depending on evidence:

- prepare patched source/release;
- disable/remove a vulnerable optional feature in the next build;
- strengthen validation/resource bounds;
- reject a vulnerable format/version where safe;
- require fresh master authorization;
- improve failure cleanup/rollback;
- update user warning/documentation;
- advise temporary avoidance of a workflow.

Do not introduce destructive behavior such as automatic vault wiping as a rushed mitigation without dedicated review.

## 10. Fix design

Before coding, state the invariant being restored.

Examples:

- “Unauthenticated KDF metadata must be bounded before Argon2.”
- “Lock must cancel operations authorized by the ended session.”
- “A replacement database must be validated before active DB mutation.”
- “User-controlled attachment names must not become filesystem paths.”
- “Raw exception/path text must not reach sensitive UI/logs.”

Fix the invariant at the lowest stable boundary so alternate callers cannot bypass it.

## 11. Regression tests

A security fix should add the narrowest reliable failing-before/passing-after test.

Depending on the issue, add:

- unit policy test;
- crypto known-answer/tamper/resource test;
- integration persistence/backup/attachment/session test;
- deterministic concurrency test with barriers;
- source regression test for ordering/platform/error-handling shape;
- real-device validation step in `TEST_PLAN.md`/`RELEASE_CHECKLIST.md`.

Do not rely only on source string matching for a runtime cryptographic/persistence bug when an integration test is practical.

## 12. Documentation updates

Security fixes can require updates to:

- `SECURITY.md`;
- `docs/security/THREAT_MODEL.md`;
- `docs/security/CRYPTOGRAPHIC_DESIGN.md`;
- `docs/security/SESSION_SECURITY.md`;
- `docs/security/DATA_LIFECYCLE.md`;
- affected `docs/formats/*`;
- `docs/TEST_PLAN.md`;
- `docs/RELEASE_CHECKLIST.md`;
- `PROJECT_STATUS.md`;
- `CHANGELOG.md`;
- release notes/advisory;
- `what_changed.md` implementation ledger.

Do not silently change a security claim without reconciling all public surfaces.

## 13. Format/version changes

If the fix changes cryptographic/backup/attachment/schema interpretation:

- decide whether a new version is required;
- preserve/read/migrate supported old data where safe;
- reject unsafe/unsupported old data explicitly when necessary;
- add compatibility tests;
- document upgrade/backup guidance;
- review rollback/downgrade implications.

Never reinterpret an existing version incompatibly merely to avoid a migration.

## 14. Release verification for a security fix

At minimum run all affected gates plus normal candidate gates:

- core build/tests/format/analyzers;
- affected platform compile;
- integration regression test;
- CodeQL/dependency/security checks;
- target-device validation when platform behavior is involved;
- backup/restore/migration compatibility when formats/persistence changed;
- accessibility/UI warning review when user flows changed.

Urgency can compress scheduling, not eliminate essential validation.

## 15. Disclosure timing

If exploitation is not already public, avoid publishing detailed weaponizable reproduction before users have a reasonable patch/upgrade path when coordinated disclosure is appropriate.

A public advisory/release note should contain enough information for users to understand:

- affected versions;
- impact;
- fixed version;
- recommended action;
- meaningful limitations;
- credit if reporter wants it.

Avoid exposing reporter/private user data.

## 16. User communication

Use accurate wording.

Do not say:

- “no data could ever be affected” without evidence;
- “military-grade/unhackable”;
- “fully audited”;
- “physical deletion is guaranteed.”

If scope is uncertain, say what is confirmed and what remains under investigation.

## 17. Release artifact/provenance

For the fixed release retain:

- fixed source commit/tag;
- build/toolchain versions;
- test/CI evidence;
- dependency review;
- artifact checksums;
- signing identity reference without secret material;
- store build flag choices;
- advisory/release-note linkage.

Follow `../releases/RELEASE_PROCESS.md`.

## 18. Post-fix review

After release:

- verify the public fix artifact actually corresponds to the intended commit;
- re-run synthetic reproduction against the shipped artifact where feasible;
- monitor for bypass reports;
- search adjacent code for the same bug class;
- consider fuzz/property/concurrency tests to prevent recurrence;
- update roadmap/security-review priorities.

## 19. Example adjacent-code questions

If a path-validation bug is found, inspect:

- backup paths;
- attachment paths;
- SQLite snapshot/recovery paths;
- temp/cache cleanup;
- diagnostics/export paths.

If an unbounded-resource bug is found, inspect:

- KDF metadata;
- JSON/header sizes;
- record envelopes/counts;
- archive entries;
- CSV fields/rows;
- attachment chunks;
- UI search/input lengths.

If a cancellation bug is found, inspect:

- session leases;
- mutation gates;
- restore rollback;
- destructive commit points;
- cleanup masking.

## 20. Security audit relationship

This runbook does not make CipherNest independently audited. A maintainer's internal security response and automated tests are not substitutes for independent professional review.

Keep audit status explicit until a real audit is completed and its scope/report is known.
