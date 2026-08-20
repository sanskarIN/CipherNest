# What Changed

Detailed historical ledgers are preserved at:

- [`docs/history/what_changed_through_2026_08_15.md`](docs/history/what_changed_through_2026_08_15.md) — implementation history through August 15, 2026.
- [`docs/history/what_changed_through_2026_08_18.md`](docs/history/what_changed_through_2026_08_18.md) — complete live ledger covering the August 16 documentation expansion and August 18 bounded TOTP setup-URI continuation.
- [`docs/history/what_changed_through_2026_08_19.md`](docs/history/what_changed_through_2026_08_19.md) — byte-identical archive of the August 19 localization, repository-wide documentation, and tracked-file completeness continuations.

This live ledger continues from **August 20, 2026**. Git history remains the authoritative commit-by-commit record.

---

## 2026-08-20 — Trash localization and feature-catalog continuation

### Goal

Continue the next repository-completable CipherNest improvement rather than restating already-finished work or pretending that external physical-device/release/security-review gates can be completed through source edits alone.

Repository inspection at the start of this continuation found:

- no open GitHub issues;
- no obvious `TODO`, `FIXME`, `HACK`, `XXX`, `NotImplementedException`, or `PlatformNotSupportedException` markers in repository search;
- the remaining localization roadmap still explicitly allows not-yet-migrated UI literals to appear in English.

The concrete gap selected for this continuation was the Trash/permanent-deletion workflow because it still contained English-only fixed text and runtime destructive-action messages.

### Starting head

This continuation started from:

`3e73f7b0ed55928b64702d4cece2f50c38cf9eeb`

Historical test/platform/CodeQL evidence recorded for earlier immutable candidates remains historical only and is not automatically inherited by the August 20 head.

### Scalable feature localization catalogs

Updated:

`src/CipherNest.App/Services/LocalizationService.cs`

The localization service now supports ordered resource catalogs:

1. the primary `AppStrings` resource set is checked first;
2. registered feature resource catalogs are checked next;
3. a missing key still returns the key name rather than silently becoming blank.

The first feature catalog is the Trash workflow. This keeps a complete screen/workflow migration cohesive while preserving the existing single `ILocalizationService` and culture-selection path. It does not introduce a parallel localization store or alter persistence, cryptography, authentication, backup compatibility, or vault formats.

### Neutral English Trash catalog

Added:

`src/CipherNest.App/Resources/Localization/TrashStrings.resx`

The neutral catalog covers the complete migrated Trash surface, including:

- page title and status semantics;
- permanent-deletion heading and explanation;
- current-master-passphrase placeholder;
- empty-trash, Restore, and Delete controls;
- empty-view and deleted-date presentation;
- trash-count/retention status formatting;
- already-empty status;
- per-item permanent-delete confirmation;
- empty-trash count-formatted confirmation;
- successful empty-trash status;
- missing-master and failed-master-confirmation statuses.

Security-sensitive English wording continues to state that recovery keys are not accepted for destructive confirmation and that filesystem/flash-storage/forensic remnants can remain outside CipherNest's control.

### Reviewed Hindi Trash catalog

Added:

`src/CipherNest.App/Resources/Localization/TrashStrings.hi-IN.resx`

The Hindi catalog has exact key parity with the neutral Trash catalog and reviewed distinct values for every migrated key. It preserves the same security meaning, including:

- current-master re-authentication for permanent deletion;
- rejection of recovery keys for this destructive confirmation;
- storage-remnant limitations;
- explicit forensic-recovery caveats;
- no false success after failed master-passphrase confirmation.

### Trash XAML migration

Updated:

`src/CipherNest.App/Views/TrashPage.xaml`

The fixed user-facing Trash surface now resolves reviewed resources through `l10n:Translate` for:

- title;
- Back action;
- status semantic description;
- permanent-deletion title/body;
- current-master placeholder;
- Empty trash action;
- empty view;
- deleted-date label;
- Restore/Delete actions.

The deleted timestamp remains a culture-sensitive runtime binding while the fixed `Deleted:` label is resource-backed.

### Trash runtime/destructive-action migration

Updated:

`src/CipherNest.App/ViewModels/TrashViewModel.cs`

Runtime status and confirmation text now resolves through `ILocalizationService`, with dynamic counts/retention values formatted using `CultureInfo.CurrentUICulture`.

Migrated runtime states include:

- empty/list-count status;
- already-empty status;
- per-item permanent-delete confirmation;
- empty-trash confirmation with item count;
- completed empty-trash status;
- current-master-required status;
- failed-master-confirmation status;
- common localized Cancel action.

The authorization behavior is unchanged: manual permanent deletion still depends on the existing current-master re-authentication path and vault-service deletion behavior.

### Empty-trash success-state correction

The previous `EmptyTrashAsync` path published the successful `Trash emptied...` message and then immediately called `LoadAsync()`, which replaced that success message with the generic empty-trash status.

The completed path now:

- permanently deletes the selected Trash records through the existing service;
- clears the in-memory Trash collection;
- publishes the completed localized success message;
- does not immediately overwrite that result with a general reload status.

This is a UI state/publication correction, not a relaxation of destructive authorization.

### Regression protection

Added:

`tests/CipherNest.UiTests/TrashLocalizationSourceTests.cs`

The focused test guards:

- feature-catalog registration in `LocalizationService`;
- Trash XAML use of reviewed resource keys;
- removal of selected previous hard-coded English fixed text;
- ViewModel use of localized runtime/destructive-action keys;
- active-UI-culture formatting for dynamic values;
- preservation of the completed empty-trash success state;
- exact neutral/Hindi feature-catalog key parity;
- non-empty values;
- distinct reviewed Hindi values;
- preservation of recovery-key and filesystem/forensic-remnant safety language;
- preservation of `{0}`/`{1}` placeholders in the translated Trash-count format and `{0}` in the translated empty-trash confirmation format.

### Exhaustive documentation inventory synchronization

Updated:

- `docs/SOURCE_CODE_REFERENCE.md`
- `docs/TEST_SUITE_REFERENCE.md`
- `docs/REPOSITORY_FILE_REFERENCE.md`

The source reference now maps both new Trash resource files and the updated responsibilities of `LocalizationService`, `TrashViewModel`, and `TrashPage`.

The test reference now maps `TrashLocalizationSourceTests.cs`, which is required because the repository's automated documentation inventory gate checks every tracked `tests/` path.

The repository reference now maps the new August 19 ledger archive so the exhaustive tracked-file documentation contract remains synchronized after the ledger rollover.

### Localization architecture documentation

Updated:

`docs/architecture/LOCALIZATION.md`

The architecture reference now documents:

- primary versus feature resource catalogs;
- ordered lookup and visible missing-key fallback;
- exact neutral/satellite parity requirements per feature catalog;
- when to use shared `AppStrings` versus a feature catalog;
- mandatory feature-catalog registration/tests;
- the complete Trash localization scope;
- Trash destructive-action security meanings;
- the empty-trash success-state behavior;
- manual release validation required for translated Trash layouts and destructive workflows.

The documentation still does **not** claim that the entire application is fully translated. Remaining non-migrated UI text can still appear in English.

### Historical ledger preservation

Before rolling the live ledger forward, the previous `what_changed.md` blob was preserved unchanged as:

`docs/history/what_changed_through_2026_08_19.md`

This avoids rewriting or dropping the detailed August 19 continuation record while keeping the live ledger focused on current work.

### Commits in this continuation before this ledger refinement

- `7ec54f97cea5e835064407e7f4325a65995f2470` — `feat(localization): support feature resource catalogs`
- `74c3635844645fa6281235115e9d622785eedbf7` — `feat(localization): add Trash neutral resource catalog`
- `3f73d51fe6470bc2f869352dfb3be84d37ff01de` — `feat(localization): add reviewed Hindi Trash catalog`
- `565417f5559e376ff40456608bf2d0a43ddf7c4d` — `feat(trash): localize destructive action runtime text`
- `34a92b19cdfdfe5909c3d93bfe340e76dfa5a865` — `feat(trash): localize permanent deletion page surface`
- `142ce6125a8f893701857680a99d01b78f860feb` — `test(localization): guard Trash safety surface translations`
- `d605b6381553b3e1e609b7a27abb79107eba9e84` — `docs(source): map Trash localization resources and behavior`
- `b8b86d55a6aa8f166709457dad25c0e758098c49` — `docs(tests): map Trash localization regression coverage`
- `64294d3529777d51a891b40ea19f4f359c6abaa0` — `docs(localization): document feature catalogs and Trash migration`
- `c74fd2c0114b8c6420dc3790d49d1d2d4ae1d597` — `docs(history): preserve August 19 live change ledger`
- `d434ff1df4a0cc7a148e9959d0dfc7560d78fbfc` — `docs(repo): map August 19 ledger archive`
- `abaa3a42c72c35247f5d14b1e41f19c1533fa68d` — `docs(ledger): record August 20 Trash localization continuation`
- `56cdc78e29cd4db4cb4c6474ffffb84bad3d4de7` — `test(localization): protect Trash format placeholders`

All commits in this continuation use the sign-off identity `Sanskar <sanskarin@outlook.in>`.

### Verification status at ledger refinement

No exact-head build/test/platform pass claim is made merely because code, tests, and documentation were committed.

For the prior ledger head `abaa3a42c72c35247f5d14b1e41f19c1533fa68d`, the GitHub combined-status lookup exposed no status checks and the available commit-workflow lookup exposed no runs. The repository CI workflow is configured for pushes to `main`, but the available commit-workflow helper is limited to pull-request-triggered runs; therefore the empty lookup is **not** interpreted as either a pass or a failure.

The final immutable head created by this ledger refinement must likewise be checked separately through observable exact-SHA evidence. Historical 555-test/platform/CodeQL results remain attached only to their documented historical SHA.

Physical-device lifecycle, biometrics, secure storage, clipboard-history/synchronization, screenshot/task-preview behavior, accessibility/translated-layout validation, representative third-party TOTP interoperability, signing/notarization/store acceptance, dependency/license release review, and independent professional security review remain external/manual gates.

### Security/release claims intentionally unchanged

This continuation does **not** claim:

- complete Hindi translation of all CipherNest screens;
- guaranteed physical erasure of deleted data;
- completion of physical-device validation;
- signing/notarization/store acceptance;
- absence of unknown defects;
- completion of an independent professional security audit.

---

## 2026-08-20 — Transfer plaintext-boundary localization continuation

### Goal and starting point

After completing the Trash/permanent-deletion localization pass, the next repository-completable high-risk English-only surface was the generic CSV import and plaintext CSV export workflow.

This second August 20 phase started from:

`9828c4cc7576245b313bb90caf082185f4ac36fb`

The goal was to translate the user-facing Transfer boundary without changing CSV parsing limits, vault encryption, current-master authorization, the exact plaintext-export acknowledgement contract, or the documented limitations of temporary/plaintext cleanup.

### Transfer feature catalog registration

Updated:

`src/CipherNest.App/Services/LocalizationService.cs`

The ordered feature resource list now includes both:

- `TrashStrings`;
- `TransferStrings`.

The primary `AppStrings` lookup remains first, each feature catalog retains normal neutral-English satellite fallback, and missing keys remain visible by returning the key name.

### Neutral English and reviewed Hindi Transfer catalogs

Added:

- `src/CipherNest.App/Resources/Localization/TransferStrings.resx`
- `src/CipherNest.App/Resources/Localization/TransferStrings.hi-IN.resx`

The feature catalogs contain exact key parity for the complete migrated Transfer surface, including:

- page/import/plaintext-export headings and explanations;
- CSV picker and every explicit column-mapping label;
- encrypted-backup recommendation;
- current-master and plaintext acknowledgement inputs;
- export/cache actions and accessibility descriptions;
- picker/no-selection/mapping-review/failure states;
- import confirmation, localized imported/skipped result formats, and failure states;
- current-master/recovery-key plaintext-export security messages;
- plaintext-export confirmation and share warnings;
- temporary share staging, cleanup, and cache-removal messages.

The reviewed Hindi text preserves the same security meaning as neutral English.

### Exact `EXPORT PLAINTEXT` contract preserved

The literal acknowledgement token:

`EXPORT PLAINTEXT`

remains unchanged in `TransferViewModel.ExportPhrase` and remains visibly embedded unchanged in both language catalogs where the user is instructed what to type.

The surrounding instruction is translated, but localization cannot translate, normalize, case-fold, or replace the control token that the authorization path compares with exact ordinal equality.

### Transfer XAML migration and accessibility

Updated:

`src/CipherNest.App/Views/TransferPage.xaml`

The fixed Transfer UI now uses `l10n:Translate` for:

- page title and Back action;
- CSV import heading/summary/picker;
- all mapping labels;
- mapped-import action;
- plaintext-export heading/warning;
- master-passphrase and acknowledgement placeholders;
- export and cache-clean actions.

Localized semantic descriptions were also added for the sensitive master confirmation, acknowledgement input, plaintext-export action, and plaintext-cache cleanup action.

### Transfer runtime and confirmation localization

Updated:

`src/CipherNest.App/ViewModels/TransferViewModel.cs`

`ILocalizationService` is now constructor-injected through the existing MAUI dependency-injection graph. `ILocalizationService` was already registered as a singleton before the transient `TransferViewModel`, so no new service-registration dependency was required.

Runtime localization now covers:

- no-CSV-selected state;
- file picker title;
- mapping-review guidance;
- CSV selection/open failures;
- missing title mapping;
- import confirmation and failure;
- import result counts;
- exact acknowledgement guidance;
- master-confirmation exception/failure states;
- plaintext-export confirmation/failure;
- temporary-share status/title;
- best-effort staging cleanup warning;
- plaintext export-cache cleanup success/failure.

Dynamic result text uses `CultureInfo.CurrentUICulture` formatting.

### Privacy-safe localized import result

Previously the successful import result appended up to three raw `CsvTransferService` warning strings through:

`string.Join(" ", result.Warnings.Take(3))`

That mixed English infrastructure/parser/validator text into a localized presentation surface and exposed row-specific diagnostic wording directly in the main status line.

The ViewModel now publishes:

- localized imported/skipped counts when there are no warnings;
- localized imported/skipped counts plus a generic reviewed statement that some skipped rows did not satisfy local CSV or vault-item validation rules when warnings exist.

The underlying parser/validator behavior and warning collection remain available to their implementation/tests; the presentation change does not weaken validation or alter which rows import.

### Regression coverage

Added:

`tests/CipherNest.UiTests/TransferLocalizationSourceTests.cs`

The test protects:

- `TransferStrings` registration;
- XAML use of all migrated fixed/semantic resource keys;
- removal of selected old English literals;
- ViewModel use of every migrated runtime/confirmation key;
- `ILocalizationService` injection and active-UI-culture formatting;
- use of the shared localized Cancel action;
- suppression of raw `result.Warnings` concatenation in the localized result surface;
- exact neutral/Hindi feature-catalog parity;
- non-empty and distinct reviewed Hindi values;
- preservation of recovery-key, backup/share/antivirus/OS, and filesystem-snapshot security meanings;
- required `{0}` / `{1}` formatting placeholders;
- exact preservation of the `EXPORT PLAINTEXT` acknowledgement token.

### Existing security test compatibility fix

Updated:

`tests/CipherNest.UiTests/SensitiveCredentialLifetimeSourceTests.cs`

The production ViewModel now obtains the plaintext-export confirmation title from the resource catalog, so the existing credential-lifetime source test could no longer use the removed English literal as its ordering marker.

The test now verifies that `ExportMasterPassphrase` is cleared before the localized `TransferExportConfirmTitle` is resolved/displayed. This preserves the original security invariant while allowing translated confirmation copy.

`TransferCsvFailureStateSourceTests.cs` was also aligned with the localized `TransferNoCsvSelected` reset state.

### Documentation synchronization

Updated:

- `docs/SOURCE_CODE_REFERENCE.md`
- `docs/TEST_SUITE_REFERENCE.md`
- `docs/architecture/LOCALIZATION.md`

The exhaustive source inventory now maps the two new Transfer resource files and the updated Transfer ViewModel/page responsibilities.

The exhaustive test inventory now maps `TransferLocalizationSourceTests.cs`, so the tracked-file documentation gate has a canonical owner for the new test path.

The localization architecture now documents:

- Trash and Transfer as current feature catalogs;
- exact feature-catalog parity requirements;
- the complete generic CSV/plaintext Transfer migration scope;
- the unchanged exact acknowledgement token;
- current-master/recovery-key rules;
- source/plaintext/share/backup/search/antivirus/OS/snapshot limitations;
- the generic localized import-result policy instead of raw infrastructure warning concatenation;
- release validation for translated Transfer layouts, semantics, token behavior, share-return cleanup, and cache cleanup.

### Commits in this Transfer phase before this ledger commit

- `a711410c17ae36d63af15fd6ffa10812837bafca` — `feat(localization): register Transfer feature catalog`
- `3aba3a01b3b1b99455741e215e2b3bd73d263a9b` — `feat(localization): add Transfer neutral resource catalog`
- `e85de46015c142636b650afbf1156d9f64e44cf7` — `feat(localization): add reviewed Hindi Transfer catalog`
- `751cb80a53191176ed9e5efb5b2cf97971ea4e90` — `feat(transfer): localize import and plaintext export runtime text`
- `c6bf6666818fd3c6e6f91e25f74f252edf60b60c` — `feat(ui): localize Transfer plaintext boundary surface`
- `7de4cdf0168b9ec1bde44d3373a9b1e08be119cb` — `test(localization): guard Transfer plaintext boundary translations`
- `0eec0f1e60de5ecf4576820935b8684ead42574b` — `test(transfer): align reset guard with localized status`
- `e40e0dc07babec6fbb5bb39e769645959ed380bc` — `docs(source): map Transfer localization resources and behavior`
- `0f1730f585a3bfccad9934670cc002770c1e454f` — `docs(tests): map Transfer localization regression coverage`
- `b7077f5a141d70e08f57f97abe5d1f967795541a` — `docs(localization): document Transfer plaintext boundary migration`
- `b890cda67a00dfdfc6718e7e037efb20833ac230` — `test(security): align Transfer credential lifetime guard with localization`

All commits use the sign-off identity `Sanskar <sanskarin@outlook.in>`.

### Verification status at publication

This source/documentation phase does not itself prove that the new immutable head passes build, test, target-platform, or CodeQL gates.

The final head created by this ledger commit must be inspected separately through observable exact-SHA GitHub evidence. If the available connector exposes no statuses/runs, that will be reported as unavailable evidence rather than interpreted as success or failure.

Historical **555-test**, Windows, Android, iOS simulator, Mac Catalyst, and CodeQL success remains evidence only for the previously documented immutable baseline that actually produced those results.

External/manual gates remain unchanged: physical-device lifecycle/biometrics/secure storage/clipboard/screenshot behavior, translated-layout and assistive-technology validation, real file-picker/share-sheet behavior, representative third-party TOTP interoperability, signing/notarization/store acceptance, dependency/license release review, and independent professional security review.

### Security/release claims intentionally unchanged

This Transfer localization phase does **not** claim:

- that plaintext CSV becomes protected outside CipherNest;
- that temporary-cache deletion removes copies created by share targets, backups, snapshots, indexing, antivirus, or the operating system;
- that recovery keys can authorize plaintext export;
- that every remaining CipherNest screen is fully translated;
- that external/device/release/security-review gates are complete.
