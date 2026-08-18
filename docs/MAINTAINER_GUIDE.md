# CipherNest Maintainer Guide

This guide collects day-to-day repository maintenance expectations that span development, security, documentation, releases, dependencies, and user support.

## 1. Maintainer priorities

For CipherNest, maintain these priorities in order:

1. avoid loss/corruption of user vault data;
2. preserve confidentiality/integrity boundaries;
3. keep authentication/session/destructive authorization correct;
4. keep backup/recovery compatibility testable;
5. keep platform limitations honest;
6. keep source/build/test/release evidence reproducible;
7. keep documentation synchronized with current behavior;
8. add features only after their attack/privacy surface is understood.

## 2. Never use real user secrets for development

Development/test/reproduction data must be synthetic.

Do not request or commit:

- real vaults;
- master/backup passphrases;
- recovery values;
- secondary secure-storage secrets;
- private documents/attachments;
- payment credentials;
- signing private keys/passwords;
- store/API tokens;
- real TOTP seeds, setup URIs, or generated one-time codes.

## 3. Small reviewable commits

Prefer one logical change per commit.

A good security-sensitive sequence is:

```text
policy/contract
implementation
unit/integration regression test
source/platform regression test if needed
documentation/release gate
progress ledger
```

The requested project commit identity is:

```text
Sanskar <sanskarin@outlook.in>
```

When commits are created through a connector that does not expose an author-email override, include the requested identity in a `Signed-off-by` trailer where the connector accepts commit-message text, and verify actual Git author/committer metadata independently before release. Do not claim the connector changed author metadata when it did not.

## 4. Main-branch discipline

Direct main changes should remain small, buildable/reviewable, and evidence-aware. Do not use main as scratch storage for secrets/generated signing artifacts/real vaults.

Before a release, select an exact candidate SHA and stop silently moving the candidate while collecting evidence.

## 5. Source quality policy

Shared build policy enables:

- nullable analysis;
- warnings as errors;
- latest analyzers;
- code-style enforcement;
- deterministic compilation.

Do not disable these globally to hide a new warning/failure.

## 6. Public contract changes

When changing Application interfaces or Domain models:

- preserve dependency direction;
- review serialized compatibility;
- update `API_REFERENCE.md`;
- update implementation/tests;
- update user docs if observable behavior changes;
- update format/security docs if persisted semantics change.

## 7. Security-sensitive source areas

Changes in these areas require extra review/testing:

```text
Infrastructure/Crypto
VaultService/session/key leases
SQLite/migrations/replacement/snapshot/deletion
EncryptedAttachmentStore/attachment policies
EncryptedBackupService/backup policies
CSV parser/transfer
TOTP generation/setup-URI parser/formatter
biometrics/secure storage
clipboard/screenshot/lifecycle
plaintext export/preview
privacy-safe diagnostics
```

Use `THREAT_MODEL.md`, `CRYPTOGRAPHIC_DESIGN.md`, `SESSION_SECURITY.md`, `DATA_LIFECYCLE.md`, and `TOTP.md` as review checklists where applicable.

## 8. Versioned format discipline

Current independent versions include:

- product version;
- database schema version;
- crypto envelope version;
- vault-header document version;
- attachment container magic/version;
- encrypted backup format version.

Do not conflate them. A schema change does not automatically mean a crypto-version change, and vice versa.

If interpretation changes incompatibly, introduce/review the proper version/migration path.

TOTP setup-URI text interoperability is not a new persisted vault format: the current implementation parses or formats transient text around the existing TOTP item fields. Do not introduce a duplicate persisted URI field without explicit compatibility/security review.

## 9. Migration discipline

Released migrations are compatibility history.

- append migrations rather than editing old released meaning;
- reject future schema versions;
- validate required schema shape after version resolution;
- test supported prior states;
- preserve original errors when rollback fails;
- do not manually copy a live SQLite DB as a supported backup procedure.

## 10. Resource-limit discipline

Limits exist to bound malicious/corrupted local input and application memory work.

When changing a limit, update all mirrored boundaries, arithmetic-policy tests, integration behavior, test/release gates, `LIMITS_AND_DEFAULTS.md`, and affected format/security docs.

Avoid increasing a ceiling merely to make a pathological test/file pass without reviewing memory/CPU/disk consequences.

For TOTP setup URIs, preserve the current independent URI/query/account/issuer ceilings, duplicate-key rejection, and downstream `TotpPolicy` validation unless a reviewed replacement is introduced.

## 11. Dependency updates

For every dependency change:

- identify why it is needed;
- review exact direct/transitive versions;
- review license;
- review vulnerability/dependency-review output;
- confirm correct project layer;
- update `THIRD_PARTY_NOTICES.md` if required;
- run relevant core/platform gates;
- record release provenance.

Do not add a remote analytics/security service that receives vault context without separate privacy/threat design.

## 12. Platform/API updates

MAUI/Android/iOS/Windows/platform SDK upgrades can change:

- biometric APIs;
- secure storage;
- lifecycle behavior;
- file picker/share behavior;
- clipboard behavior;
- screenshot protections;
- packaging/signing;
- accessibility semantics.

A successful compile is not enough. Re-run the target device matrix. TOTP setup-URI parsing is platform-independent, but copied setup URIs still cross the platform clipboard boundary and need representative runtime validation.

## 13. Backup/recovery maintenance

Any persistence/format change must answer:

- Can current builds restore old supported backups?
- Can new backups be restored by intended target platforms?
- Does rollback still preserve active data after cancellation/failure?
- Are DB/WAL/SHM/attachments handled consistently?
- Are resource/path/duplicate bounds still symmetric?

Use `operations/BACKUP_RECOVERY_RUNBOOK.md`.

## 14. User support

Support should begin with documentation and synthetic reproduction, not data collection.

Useful requests:

- app version/commit;
- platform/OS;
- fixed error text;
- reproduction using fake data;
- whether a separate verified backup exists;
- approximate non-sensitive file size/count.

Do not ask for passphrases/recovery material/real vault contents/TOTP seeds/setup URIs/current codes.

## 15. Security reports

Follow:

- root `SECURITY.md` for public reporter instructions;
- `operations/SECURITY_RESPONSE.md` for private maintainer response.

Keep exploit details private until coordinated disclosure timing permits publication when appropriate.

## 16. Documentation ownership

Treat docs as part of the feature.

A user-visible/security-sensitive change is incomplete until applicable docs are synchronized.

Canonical navigation is `docs/README.md`.

See `DOCUMENTATION_MAINTENANCE.md`.

## 17. Changelog/status ownership

`CHANGELOG.md` answers “what changed for releases/users/developers.”

`PROJECT_STATUS.md` answers “what is implemented in source versus still externally unverified/deferred.”

`what_changed.md` is the chronological implementation ledger and should preserve older history rather than rewriting it into a shorter retrospective.

## 18. CI evidence

Configured workflows are useful only when they run.

For a candidate review:

- inspect exact commit checks;
- record missing/no-status as missing evidence, not pass;
- inspect failing logs rather than rerunning until green without understanding failures;
- keep timeouts/cancel-in-progress behavior;
- do not disable CodeQL/dependency checks to ship faster;
- because the main workflow uses cancel-in-progress concurrency, collect final evidence only after the intended exact head stops moving.

## 19. Store policy maintenance

Store rules change. Before every package release, verify current exact policy for:

- permissions/privacy declarations;
- external funding CTA;
- platform signing/notarization;
- biometric usage descriptions;
- data collection claims.

The current funding CTA can be disabled per build with `CipherNestEnableFundingLink=false`.

## 20. Branding maintenance

Edit source vectors, not generated raster artifacts where MAUI generation applies.

Keep:

- original CipherNest mark;
- creator credit only on appropriate branding/About surfaces;
- no real user data in screenshots;
- no unsupported security claims.

See `branding/ASSETS.md`.

## 21. Localization maintenance

Neutral English remains the fallback and a reviewed Hindi resource-backed catalog is implemented; remaining literal UI text can still appear in English.

When adding a language:

- translate every supported user/security/error string;
- review security warning meaning;
- test long text/responsive layout;
- test fallback;
- update store metadata/documentation;
- do not claim partial catalog as complete language support.

## 22. Accessibility maintenance

Every new UI surface should pass the checklist in `ACCESSIBILITY.md` and receive target-device screen-reader/keyboard/large-text validation before release.

Do not put a secret in semantic metadata merely because it is not visually displayed. In particular, TOTP setup-URI fields/descriptions must not echo the actual URI/seed into accessibility labels.

## 23. Performance maintenance

Measure before changing privacy architecture for speed.

Do not introduce a plaintext search index/cache to improve search without a specific privacy review.

If large-vault performance becomes inadequate, profile decrypted in-memory search/rendering and consider privacy-preserving alternatives deliberately.

## 24. Deferred features

Do not quietly implement one piece of these and advertise the whole feature:

- cloud sync/accounts/collaboration;
- autofill;
- TOTP QR scanning/rendering/camera enrollment;
- HOTP interoperability;
- TOTP provider/autofill enrollment;
- Windows Hello;
- rich binary/PDF preview/scanning;
- pronounceable passwords;
- automatic destructive wipe;
- complete migration/review of remaining UI literals into additional-language catalogs.

Bounded `otpauth://totp/...` text import/formatting is implemented and is no longer part of the deferred list. Each remaining item requires its own design/test/privacy/migration/release plan.

## 25. Release maintenance

Follow `releases/RELEASE_PROCESS.md` and `RELEASE_CHECKLIST.md`.

Do not tag/publish a release until applicable evidence is complete and documentation/audit status is accurate.

## 26. Post-release maintenance

After a release:

- monitor security/dependency advisories;
- triage regressions;
- verify support docs against shipped version;
- keep Unreleased changelog current;
- preserve release provenance/checksums;
- plan format migrations before removing old compatibility.

## 27. Maintainer handoff

A new maintainer should first read:

1. `docs/README.md`;
2. `DEVELOPER_GUIDE.md`;
3. `architecture/ARCHITECTURE.md`;
4. `architecture/DATA_FLOW.md`;
5. `architecture/SESSION_AND_CONCURRENCY.md`;
6. `security/THREAT_MODEL.md`;
7. `security/CRYPTOGRAPHIC_DESIGN.md`;
8. `security/DATA_LIFECYCLE.md`;
9. `security/TOTP.md`;
10. `TESTING_GUIDE.md`;
11. `releases/RELEASE_PROCESS.md`.

Then run the core verification scripts on a clean checkout before making security-sensitive changes.

## TOTP maintenance rules

Treat TOTP seed/settings/setup URIs as security-sensitive semantics. Preserve the explicit persisted `VaultItemType` numeric values, RFC 6238 known-answer vectors, Base32/resource bounds, no-generated-code-persistence rule, audit exclusions, and explicit clipboard behavior.

For the implemented setup-URI surface, preserve:

- TOTP-only `otpauth://totp/...` acceptance;
- HOTP and `counter` rejection;
- URI/query/display metadata bounds;
- duplicate-query rejection;
- issuer consistency validation;
- downstream `TotpPolicy` validation;
- masked/transient import UI state;
- no setup-URI persistence as a separate item field;
- no secret-bearing diagnostics;
- setup-URI copy through secret clipboard handling.

Any QR/camera parser/rendering, background refresh, HOTP, autofill/provider integration, or broader interoperability change requires dedicated threat, parser, lifecycle, accessibility, format, and release review rather than being folded into an unrelated UI patch.

Never request a user's real TOTP seed, setup URI, or current code in support/security triage. Synthetic seeds only belong in tests/documentation.

For localization, maintain neutral-English/satellite key parity and do not describe the complete UI as Hindi-translated until remaining literals are migrated and reviewed.
