# What Changed

The detailed implementation ledger through the final pre-documentation hardening period is preserved unchanged at:

[`docs/history/what_changed_through_2026_08_15.md`](docs/history/what_changed_through_2026_08_15.md)

This live ledger continues from the 2026-08-16 complete documentation and repository presentation work. Git history remains the authoritative commit-by-commit record.

## 2026-08-16 — Complete project documentation expansion

### Goal

Create a truly complete, navigable, source-grounded documentation suite for CipherNest rather than relying on one increasingly large overview file. The work also reconciles stale current guidance with the latest verified implementation baseline while preserving historical verification records for their original exact commits.

### Immutable source baseline used while authoring

The documentation expansion was grounded in implementation commit:

`8566980ff981b8b4072f9010ec7b7ba54aba051e`

Observed exact-candidate evidence before documentation commits:

- CipherNest CI run `31937127961`: success;
- CodeQL run `31937127900`: success;
- 346 UnitTests passed;
- 98 IntegrationTests passed;
- 111 UI/source tests passed;
- **555 total passed, 0 failed, 0 skipped**;
- analyzer-enabled core test builds completed with zero build warnings/errors;
- configured core formatting passed;
- Windows default Release passed;
- Windows `CipherNestEnableFundingLink=false` Release passed;
- Android Release passed;
- iOS simulator Release passed;
- Mac Catalyst Release passed;
- CodeQL v4 passed after analyzable core and MAUI application builds.

That baseline is immutable historical evidence for its exact SHA. The documentation commits below create a new exact head and therefore require their own configured CI/CodeQL runs before the later documentation head can be described as exact-head verified.

### New canonical documentation

Added `docs/QUICK_START.md`:

- safe first-launch/end-user bootstrap;
- master-passphrase and recovery guidance;
- first item/TOTP/attachment workflows;
- search/generator/secure-note/clipboard/trash/backup/restore/CSV/settings summaries;
- BMC build-policy note;
- contributor clone/verification bootstrap;
- exact pre-documentation verification baseline.

Added `docs/FEATURE_MATRIX.md`:

- explicit status legend;
- core storage/cryptography/session matrix;
- biometric platform matrix;
- every persisted item type;
- search/sort/reminder/audit matrix;
- TOTP matrix;
- generator/secure-note/attachment/backup/CSV/clipboard/settings/privacy/accessibility/localization matrix;
- UI/branding/BMC/build/CI matrix;
- external release gates;
- deliberately deferred future features.

Added `docs/UI_REFERENCE.md`:

- complete Shell route map;
- Startup, Onboarding, Unlock, Vault, Item Editor, Generator, Generator Defaults, Audit, Trash, Settings, Security Info, Transfer, About, and Developer page responsibilities;
- current major controls/actions;
- re-authentication gates;
- TOTP panel behavior;
- custom-field/secure-note/attachment UI behavior;
- BMC Settings/Vault surfaces;
- sensitive page lifecycle rules;
- accessibility/localization/responsive/funding UI rules.

Added `docs/CONFIGURATION_REFERENCE.md`:

- product/application metadata;
- target frameworks/minimum OS versions;
- `CipherNestTargetFrameworks` behavior;
- `CipherNestEnableFundingLink` behavior;
- `global.json` SDK policy;
- shared build-quality policy;
- centrally managed package versions;
- every persisted `AppPreferences` default and normalization bound;
- settings JSON safety policy;
- product/database/crypto/header/backup/attachment versions;
- cryptographic defaults/KDF bounds;
- vault/TOTP/attachment/backup/CSV limits;
- verification scripts;
- recorded Apple CI toolchain pairing;
- exact implementation baseline.

### Rebuilt consolidated project reference

Replaced `docs/COMPLETE_PROJECT_DOCUMENTATION.md` with a full 52-section end-to-end project reference covering:

1. project identity;
2. executive summary;
3. product goals;
4. non-goals/deferred scope;
5. supported targets;
6. technology stack;
7. repository layout;
8. dependency architecture;
9. runtime composition/services;
10. navigation/UI surfaces;
11. first launch/vault creation;
12. master/recovery model;
13. biometric convenience unlock;
14. cryptographic design;
15. vault-header compatibility;
16. item model/types;
17. validation/resource ceilings;
18. SQLite schema/migrations/replacement;
19. session/concurrency;
20. search/filters/sorting/reminders;
21. local security audit;
22. TOTP;
23. password/passphrase generation;
24. secure notes;
25. encrypted attachments;
26. encrypted backup/restore;
27. CSV transfer;
28. clipboard/plaintext lifecycle;
29. trash/permanent/full-vault deletion;
30. settings/configuration;
31. accessibility;
32. localization;
33. privacy-safe diagnostics;
34. branding/BMC;
35. build prerequisites/commands;
36. package/dependency management;
37. automated tests;
38. hosted CI/CodeQL baseline;
39. threat-model summary;
40. data lifecycle;
41. format/version compatibility;
42. release/packaging;
43. store/distribution policy;
44. security-response/recovery operations;
45. support/troubleshooting;
46. contribution/review rules;
47. documentation governance;
48. known limitations/external gates;
49. future roadmap;
50. user/developer/release checklists;
51. canonical documentation map;
52. glossary.

### Documentation navigation rebuilt

Rebuilt `docs/README.md` as the canonical documentation hub:

- prominent Quick Start / Feature Matrix / UI / Configuration / Complete Documentation entry order;
- grouped user/developer/architecture/security/format/build/release/operations references;
- highlighted BMC support badge while retaining voluntary-support wording;
- current 555-test implementation baseline;
- historical verification records clearly separated from the current baseline;
- documentation maintenance rules synchronized with current TOTP/Hindi/deferred scope.

Rebuilt the root `README.md` as a complete public landing page:

- kept CipherNest logo and prominent BMC badge;
- added all new canonical documentation entry points;
- summarized current encrypted-vault/auth/session/item/TOTP/generator/note/attachment/backup/CSV/clipboard/deletion/settings/accessibility/localization/diagnostic capabilities;
- added current resource/format highlights;
- added architecture/build/CI summary;
- replaced the older 554-test public baseline with the verified 555-test implementation baseline;
- preserved external release/audit limitations and deferred-feature wording.

### Current guidance corrected

Rebuilt `docs/FAQ.md`:

- now correctly states System/English/Hindi preferences are implemented;
- now correctly states the reviewed `hi-IN` resource-backed catalog is implemented;
- explicitly states complete translation of remaining literals is not claimed;
- now correctly treats local TOTP seed storage/generation as implemented;
- preserves TOTP QR/`otpauth://`/autofill integration as deferred;
- updates platform/build/CI questions to the 555-test implementation baseline;
- expands BMC, privacy, deletion, backup, CSV, clipboard, accessibility, and support answers.

Rebuilt `docs/DEVELOPER_GUIDE.md`:

- aligns architecture/DI/session/crypto/header/persistence/TOTP/attachment/backup/CSV/settings/localization/accessibility/test rules with current source;
- removes the obsolete claim that TOTP seed storage/generation itself is deferred;
- keeps QR/`otpauth://`/provider/autofill TOTP work deferred;
- replaces the old 240-test hosted baseline with the 555-test implementation baseline;
- records the active repository commit identity observed by GitHub as `Sanskar <sanskarin@outlook.in>`.

Rebuilt `docs/setup/BUILD.md`:

- current .NET/MAUI target requirements;
- platform-specific verification/build commands;
- Windows normal/funding-disabled variants;
- Android/iOS/Mac Catalyst direct target shapes;
- recorded Apple toolchain pairing;
- build-quality policy;
- current 555-test exact implementation baseline;
- explicit distinction between compile evidence and device/store evidence.

Rebuilt `docs/verification/CI_GATES.md`:

- current exact implementation baseline/run IDs;
- historical evidence preservation policy;
- core/documentation/Windows/Android/Apple/CodeQL/dependency/BMC gates;
- release evidence checklist;
- explicit list of what automation cannot prove.

Rebuilt `PROJECT_STATUS.md`:

- concise current implemented architecture/security/auth/items/TOTP/generator/note/attachment/backup/CSV/database/settings/privacy/accessibility/BMC status;
- current 555-test implementation baseline;
- complete documentation suite status;
- external release-validation gates;
- deliberately deferred future features.

Rebuilt `docs/NEXT_STEPS.md`:

- Priority 0 now starts from the 555-test immutable baseline and final-documentation exact-head verification;
- device/session/clipboard/biometric/screenshot validation;
- destructive/recovery tests;
- backup/database/CSV/attachment validation;
- accessibility/localization/responsive validation;
- performance/scale;
- dependency/license review;
- release engineering;
- independent security review;
- launch preparation;
- separate later-version feature projects.

### Complete-documentation verification contract

Added `docs/verification/COMPLETE_DOCUMENTATION_2026_08_16.md`:

- records the immutable `8566980f...` implementation baseline;
- records 555 passing tests and all-platform CI + CodeQL success;
- defines required documentation subject coverage;
- defines source facts that must remain synchronized;
- defines documentation-source regression gates;
- preserves historical verification records;
- requires final documentation-head CI/CodeQL before exact-head claims;
- keeps external/device/security-audit limitations explicit.

### Documentation regression protection

Expanded `tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs`:

- requires `QUICK_START.md`;
- requires `FEATURE_MATRIX.md`;
- requires `UI_REFERENCE.md`;
- requires `CONFIGURATION_REFERENCE.md`;
- requires `COMPLETE_DOCUMENTATION_2026_08_16.md`;
- requires root README links to all new canonical entry points;
- requires the documentation hub to link the new references;
- updates security-disclaimer assertions for current README wording;
- verifies the 555-test baseline is present in current docs;
- verifies current Hindi resource-backed status;
- verifies BMC coverage;
- adds a new `CompleteDocumentationSuite_CoversCurrentProductSurfaces` test covering the new documents' key feature/UI/configuration/section markers.

The added Fact increases the expected UI/source test count by one if the final suite passes.

### Changelog and history handling

Updated `CHANGELOG.md` under Unreleased:

- records the complete documentation expansion;
- records the new documentation regression guard;
- records synchronization from stale 240/554 current guidance to the 555-test implementation baseline;
- records corrected current TOTP/Hindi implementation/deferred wording.

The prior ~2,500-line `what_changed.md` implementation ledger was preserved byte-for-byte as:

`docs/history/what_changed_through_2026_08_15.md`

The live `what_changed.md` now continues from this documentation milestone rather than forcing future edits to rewrite the entire historical ledger through the GitHub contents API.

### Commits in this documentation expansion

- `f60985d8b624821cd94b6e14f97e1aeb16a90200` — `docs: add complete quick-start guide`
- `3c93c9f0335a0d7651ad67d9a58faaadfe5b7aed` — `docs: add exhaustive feature matrix`
- `c7998e4b3b45eba2e5280d66d3e390b7ac1d1b86` — `docs: add complete UI and navigation reference`
- `3e2b7d8c105cc8d5b628b1c15c14139709eb77fc` — `docs: add complete configuration reference`
- `8305815d03580b50a0aa91434a2989e09dfddd3a` — `docs: rebuild complete project documentation`
- `a56c896739322e6db0ab17155840bb6e0d21426d` — `docs(verification): define complete documentation gate`
- `df17220a35470cbec19cd0adaa549148d529fb1f` — `docs(index): rebuild complete documentation hub`
- `b512b39d33c8c31d735ee3b0ffad6aa0cb4f8ac8` — `docs(readme): publish complete documentation entry points`
- `a46fb9fb1bf19ee51e8ac3d0ceb76f9be9c77d68` — `docs(faq): synchronize current features and verification`
- `b25d9c57e1d40c4f7b0267c80c525e40019de728` — `docs(dev): synchronize developer guide with current source`
- `05275983a4bc9c68f00a26d5dee95e3416e4e599` — `docs(build): update complete build and verification guide`
- `568a0d62b3c1a286e0507ba5ad6a6ad0a91072dd` — `docs(ci): synchronize current verification baseline`
- `f8ca4edb262ae8d37ff5748bc91adb388c1a4b05` — `docs(status): publish current complete project status`
- `2a95fbf65daa93627179f2655375ab57701de740` — `docs(roadmap): align next steps with current baseline`
- `2dde69f7e4a804caa929c0aa6275310de971e753` — `test(docs): guard complete documentation suite`
- `82eac57af40f74d0961d4f08e83ed23322a69505` — `test(docs): align complete feature wording`
- `a9108c188b54729273bc2a04364bd7f21c5c3cb8` — `docs(changelog): record complete documentation expansion`
- `048c59993f77d90df9228331c9c9e567beb896f3` — `docs(history): preserve pre-documentation change ledger`
- this live-ledger continuation commit.

### Security and release claims intentionally unchanged

This documentation work does **not** claim:

- unknown bugs cannot exist;
- physical-device biometric/secure-storage behavior is fully validated;
- real clipboard/screenshot/lifecycle/share-sheet behavior is proven by source tests;
- accessibility is certified;
- historical/future migration compatibility is fully proven;
- signing/notarization/store review is complete;
- CipherNest has completed an independent professional security audit.

Those remain actual evidence/release gates.
