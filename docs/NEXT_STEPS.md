# CipherNest Next Steps

This roadmap starts from the current local-first CipherNest implementation and complete documentation suite. It separates **release evidence still required** from **future product features** so deferred ideas are not confused with hidden bugs or completed work.

> The project has **not** completed an independent professional security audit. Release readiness requires more than green hosted compilation/tests.

## Priority 0 — freeze and verify the final documentation head

The immutable implementation baseline immediately before the complete-documentation expansion is:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

For that exact implementation SHA:

- CipherNest CI `31937127961`: success;
- CodeQL `31937127900`: success;
- 346 UnitTests passed;
- 98 IntegrationTests passed;
- 111 UI/source tests passed;
- **555 total passed, 0 failed, 0 skipped**;
- core analyzer builds completed with zero build warnings/errors;
- core formatting passed;
- Windows default Release passed;
- Windows funding-disabled Release passed;
- Android Release passed;
- iOS simulator Release passed;
- Mac Catalyst Release passed;
- CodeQL v4 passed after analyzable core and MAUI application builds.

The full documentation expansion creates later commits, so the immediate task is:

1. finish documentation/source-test synchronization;
2. stop changing the candidate;
3. run/observe exact-head core tests and formatting;
4. run/observe Windows default + funding-disabled builds;
5. run/observe Android build;
6. run/observe iOS simulator + Mac Catalyst builds;
7. run/observe CodeQL;
8. record the exact final documentation SHA and run IDs;
9. do not call that later SHA exact-head verified until all configured gates have finished successfully.

See `verification/COMPLETE_DOCUMENTATION_2026_08_16.md` and `verification/CI_GATES.md`.

## Priority 1 — physical-device and lifecycle security validation

### Session and lock lifecycle

Validate on representative target devices:

- manual lock immediately removes sensitive UI state;
- lock-on-background behavior;
- inactivity timeout at minimum, typical, and maximum configured values;
- suspend/resume and sleep/wake transitions;
- device clock changes do not improperly extend sessions;
- lifecycle fail-closed recovery contains secondary cleanup errors;
- lock cancels session-linked long-running vault work;
- concurrent search/save/attachment/recent-access activity does not intentionally continue under a stale session after lock.

### Clipboard

On Windows, Android, iOS, and Mac Catalyst where APIs permit:

- timed cleanup after CipherNest copy;
- preservation of unrelated newer clipboard content;
- cleanup behavior when lock occurs before the timer;
- scheduled cleanup independence from the initiating UI request cancellation;
- actual OS clipboard-history/synchronization behavior;
- username/secret/custom-secret/TOTP-code paths all use the documented conditional policy.

### Biometrics

Android:

- API 28+ devices/emulators;
- enrolled/not enrolled;
- denial/cancel;
- repeated failure/lockout;
- hardware unavailable;
- enrollment changes;
- secure-storage loss.

iOS/Mac Catalyst:

- Face ID/Touch ID availability;
- denial/cancel;
- enrollment changes;
- secure-storage lifecycle;
- request cancellation;
- process restart behavior.

Across platforms:

- fresh process requires master-auth state before convenience unlock;
- periodic master-passphrase interval is enforced;
- restore invalidates local biometric pairing;
- master-passphrase change requires the new master before convenience unlock resumes.

Keep Windows on master-passphrase fallback until a separately reviewed/tested Windows Hello design exists.

### Screenshots and task previews

- verify supported screenshot/privacy controls;
- inspect app switcher/task preview behavior;
- verify unsupported targets use honest fallback messaging.

## Priority 2 — destructive/recovery workflow validation

Using disposable synthetic vaults:

- exercise all item types including TOTP;
- exercise master unlock and recovery unlock separately;
- verify recovery material cannot authorize master-only sensitive actions;
- change master passphrase and confirm old master wrapper no longer works;
- verify biometric-session behavior after rotation;
- move items to Trash, restore, permanently delete, Empty Trash, and test retention expiry;
- verify current-master + confirmation requirements;
- verify full-vault deletion phrase/auth/final-confirmation flow;
- inspect logical cleanup of database/WAL/SHM/recovery/attachment artifacts;
- preserve explicit warning that physical remnants can remain outside application control.

## Priority 3 — backup, restore, database replacement, and transfer confidence

### Encrypted backup/restore

Test with synthetic datasets:

- no attachments;
- many attachments;
- large attachments;
- wrong backup passphrase;
- corrupt/truncated container;
- unsupported backup version;
- invalid salt/KDF/chunk metadata;
- exact max-header and over-limit header cases;
- duplicate/unknown/missing/wrong-type backup-header properties;
- duplicate normalized ZIP paths;
- unexpected/nested archive paths;
- excessive archive count/aggregate content;
- impossible attachment container sizes;
- declared-vs-actual extracted length mismatch;
- cancellation around active replacement;
- partial DB/WAL/SHM staging failures;
- stale recovery artifacts;
- restored biometric reset.

Verify failed restore preserves the active vault.

### Database replacement

Confirm staged candidates are rejected before active mutation when they have:

- failed SQLite `quick_check`;
- unsupported schema version;
- missing required tables/columns;
- malformed/unsupported/oversized vault header;
- non-canonical item IDs;
- over-budget encrypted records.

### CSV

Validate:

- quoted commas/newlines;
- Unicode;
- duplicate/empty headers;
- excessive columns including the final field at newline/EOF;
- malformed quoting;
- large valid rows;
- explicit mapping only;
- mapped tag bounds;
- plaintext export phrase + current-master auth + warning;
- temp-file cleanup behavior;
- fixed/redacted file-error messages.

### Attachment export/preview

Validate:

- text preview at empty/normal/max/invalid-UTF-8/unsupported media types;
- multi-megabyte streaming;
- staging collision behavior;
- exact GUID-N `.cna` storage-name policy;
- malformed UTF-16 and Unicode Control/Format metadata;
- plaintext export warning and unique staging;
- cleanup-failure messaging without path leakage;
- cancellation during export.

## Priority 4 — accessibility, localization, and responsive UI

Execute and record target checks for:

- Android TalkBack;
- iOS VoiceOver;
- Windows Narrator;
- Mac Catalyst/macOS VoiceOver where applicable;
- keyboard-only navigation;
- focus order/visible focus;
- OS large-text/scaling;
- CipherNest Larger Interface;
- Reduced Motion behavior;
- narrow phone layouts;
- portrait/landscape;
- tablet/large layouts;
- resizable desktop windows;
- touch targets;
- light/dark/system contrast/readability.

Localization work:

- verify the current reviewed `hi-IN` resource-backed catalog on devices;
- continue migrating remaining literals to resources;
- review security warnings in every new translation;
- do not claim full Hindi translation until every remaining user-facing literal is migrated/reviewed;
- add additional languages only with complete security-sensitive wording review.

## Priority 5 — performance and scale

Using synthetic disposable data:

- 1,000-item vault;
- 5,000-item vault;
- 10,000-item vault;
- representative larger attachments;
- representative backup archives;
- large valid CSV imports.

Measure:

- unlock latency;
- search/filter/sort latency;
- local audit latency;
- memory usage;
- 50-item incremental rendering responsiveness;
- attachment throughput;
- backup/restore duration;
- settings/storage enumeration behavior.

Do not introduce plaintext indexes merely for speed. Any new encrypted index/search design requires privacy/security review.

## Priority 6 — dependency, license, and supply-chain review

For the exact release candidate:

- run dependency review through the PR gate when applicable;
- inspect exact direct/transitive restored graph;
- review current advisories;
- confirm current `Microsoft.Data.Sqlite` / SQLitePCLRaw pins remain acceptable;
- reconcile exact licenses with `THIRD_PARTY_NOTICES.md`;
- document accepted vulnerability/license exceptions with owner and expiry;
- preserve CodeQL/application-build coverage.

## Priority 7 — release engineering

Follow `releases/RELEASE_PROCESS.md` and `RELEASE_CHECKLIST.md`.

For each target distribution:

- lock exact SDK/workload/package versions;
- build from immutable candidate;
- preserve CI/CodeQL/device-test evidence;
- sign/package only in protected environments;
- keep private keys/certificates/passwords/store tokens outside Git;
- verify application ID, display version/build, permissions/capabilities, icons, splash, privacy declarations;
- validate adaptive/monochrome/dark-surface branding;
- use synthetic/demo vault content in screenshots;
- verify store copy contains no unsupported security claims;
- verify current target-store/region policy for the BMC CTA;
- if necessary, package with `CipherNestEnableFundingLink=false` and record it in provenance;
- freeze the complete documentation suite against the exact shipped artifact.

## Priority 8 — independent professional security review

Before broader security claims or high-risk positioning, obtain independent review of:

- cryptographic key hierarchy;
- Argon2id defaults/bounds;
- nonce/AAD strategy;
- vault-header parsing/versioning;
- record identity binding;
- session/key-lease/cancellation design;
- biometric secondary wrapper/secure-storage assumptions;
- attachment container/framing/metadata;
- backup format/archive/rollback;
- SQLite migration/replacement/recovery;
- TOTP implementation/same-vault factor tradeoffs;
- clipboard fingerprint design;
- plaintext export/data lifecycle;
- resource ceilings/parser hostile-input handling;
- dependency/supply-chain posture;
- documentation/security claims.

Track findings, remediation, retesting, and audit scope/version accurately. An external review of one version does not automatically audit later source changes.

## Priority 9 — launch preparation

After technical/review gates are complete:

- freeze release notes/changelog/status;
- create release tag;
- preserve build provenance/checksums where practical;
- publish exact supported platform/version matrix;
- publish accurate privacy/recovery/plaintext-export limitations;
- publish support/security-report channels;
- verify BMC/store policy choice per package;
- preserve historical verification evidence.

## Later-version feature roadmap

These are future design projects, not release blockers for the current local-first scope:

### Optional sync/account architecture

Requires separate design for:

- zero-knowledge/server metadata model;
- device identity/trust;
- end-to-end encryption;
- conflict resolution;
- revocation/recovery;
- server compromise;
- privacy/legal/account deletion.

### Collaboration/shared vaults

Requires protocol and key-sharing design, revocation, membership lifecycle, conflict semantics, metadata privacy, and threat-model expansion.

### Autofill/browser integration

Requires platform-specific security/privacy/accessibility review and strict session/clipboard boundaries.

### Windows Hello

Requires a separately reviewed native convenience-unlock implementation and target testing; current release intentionally uses master-passphrase fallback.

### TOTP QR and `otpauth://` interoperability

Local TOTP generation is already implemented. Future interoperability requires bounded URI/QR parsing, camera lifecycle/privacy review, secret-import UX, provider quirks, tests, and documentation.

### Rich document preview/scanning

Current bounded UTF-8 text preview remains the safe baseline. Rich PDF/binary rendering/scanning adds parser/rendering/camera/privacy attack surface and needs dedicated review.

### Pronounceable-password mode

Only add after a reviewed random-generation/entropy model exists. Do not weaken the current cryptographic RNG path for aesthetics.

### Destructive wipe-on-failure

Do not implement casually. It can cause irreversible data loss and does not guarantee physical erasure.

## Definition of done for a CipherNest release

A release is not “done” merely because the repository compiles. A release candidate is evidence-backed only when:

1. exact-head automated tests/format pass;
2. exact-head Windows/Android/Apple builds pass;
3. exact-head CodeQL/dependency/security review is complete;
4. documentation matches the exact candidate;
5. target-device security/lifecycle/clipboard/screenshot tests are recorded;
6. accessibility/localization/responsive validation is recorded;
7. backup/restore/recovery/compatibility validation is recorded;
8. dependencies/licenses/advisories are reviewed;
9. package signing/notarization/store validation is complete;
10. target store/region BMC policy has been resolved;
11. independent professional security review status is represented truthfully;
12. release provenance/tag/artifacts are preserved.
