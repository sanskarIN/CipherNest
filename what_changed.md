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
