# TOTP Setup-URI Interoperability Verification Record — 2026-08-18

This record documents the repository-side implementation, regression coverage, documentation scope, and remaining release evidence for the bounded TOTP `otpauth://totp/...` interoperability continuation completed on August 18, 2026.

> **Security status:** CipherNest has **not** completed an independent professional security audit. The work in this record is repository engineering evidence, not proof that every authenticator/provider/platform accepts the same URI behavior and not proof that clipboard or managed-memory copies can be erased.

## 1. Scope

The continuation adds a deliberately narrow text-only interoperability surface for existing encrypted TOTP vault items:

- parse an authorized `otpauth://totp/...` setup URI locally;
- map account/issuer/seed/algorithm/digits/period into the existing TOTP item fields;
- format the current TOTP item into a canonical local `otpauth://totp/...` URI;
- copy the formatted URI through the existing secret clipboard service;
- clear the dedicated setup-URI import field after import attempts and when the Item Editor clears sensitive state.

It does **not** add:

- QR rendering;
- camera/QR scanning;
- HOTP/counter support;
- browser/application autofill;
- provider/network enrollment;
- cloud synchronization;
- a new persisted setup-URI vault field;
- a third-party URI/QR/camera/network dependency.

## 2. Application contract

Added:

```text
src/CipherNest.Application/Models/TotpUriProfile.cs
src/CipherNest.Application/Abstractions/ITotpUriCodec.cs
```

`TotpUriProfile` carries:

```text
AccountName
Issuer
Secret
Algorithm
Digits
PeriodSeconds
```

`ITotpUriCodec` exposes:

```csharp
TotpUriProfile Parse(string uriText);
string Format(TotpUriProfile profile);
```

The Application layer therefore owns the stable interoperability contract without depending on MAUI, SQLite, camera APIs, a QR library, or a provider SDK.

## 3. Infrastructure implementation

Added:

```text
src/CipherNest.Infrastructure/Services/TotpUriCodec.cs
```

Current parser/formatter safety properties:

| Boundary | Current rule |
|---|---|
| URI type | absolute `otpauth://totp/...` only |
| URI length | maximum 8,192 characters |
| Query pairs | maximum 16 |
| Query parameter name | maximum 64 characters; ASCII letters/digits/`-`/`_` |
| Query pair shape | non-empty `name=value`; empty pairs rejected |
| Query value encoding | percent encoding/control characters validated for every pair, including ignored unknown parameters |
| Account name | maximum 512 characters; `:` rejected inside component |
| Issuer | maximum 256 characters; `:` rejected inside component |
| Label separator | at most one decoded `:` issuer/account delimiter |
| Empty issuer prefix | rejected when a separator is present |
| User-info | rejected |
| Custom port | rejected |
| Fragment | rejected |
| Multi-segment label path | rejected |
| Duplicate query keys | rejected case-insensitively |
| Invalid percent encoding | rejected |
| HOTP host/type | rejected |
| `counter` parameter | rejected |
| Unsupported algorithm/digits/period | rejected |
| Invalid Base32 seed | rejected |
| Unicode Control/Format display metadata | rejected |
| Label/query issuer mismatch | rejected when both exist |

Imported seed/settings are not accepted under a weaker URI-specific rule. They are routed through the same `TotpPolicy.NormalizeSecret(...)` and `TotpPolicy.ValidateSettings(...)` used by normal TOTP code generation/item validation.

Formatting validates the same account/issuer/seed/settings boundary, reserves `:` exclusively for the issuer/account label separator, percent-encodes the label/query data, emits the configured algorithm/digits/period explicitly, and rejects an encoded result larger than the URI ceiling.

### Final ambiguity/resource hardening

The final repository pass identified and fixed three parser ambiguity/normalization problems before candidate freeze:

1. account or issuer text containing `:` could make a formatted URI parse back into different issuer/account metadata;
2. `StringSplitOptions.RemoveEmptyEntries` allowed empty query pairs such as `&&` or a trailing `&` to disappear silently;
3. unknown query parameters were name-validated but their values were not decoded/validated, allowing malformed percent encoding in ignored extension data to escape the URI structural boundary.

The final implementation now:

- rejects more than one decoded label `:`;
- rejects `:` inside account/issuer components;
- rejects an empty issuer before a separator;
- rejects empty query pairs;
- validates percent encoding/control characters for all query values before deciding whether a parameter is semantically used.

These rules preserve deterministic format→parse semantics and keep malformed extension parameters inside the same bounded parser trust boundary.

## 4. Dependency injection

`src/CipherNest.App/MauiProgram.cs` registers:

```csharp
AddSingleton<ITotpUriCodec, TotpUriCodec>()
```

UI code therefore consumes the Application abstraction instead of creating a second setup-URI parser.

## 5. Item Editor behavior

Updated:

```text
src/CipherNest.App/ViewModels/ItemEditorViewModel.Totp.cs
src/CipherNest.App/ViewModels/ItemEditorViewModel.Clipboard.cs
src/CipherNest.App/Views/ItemEditorPage.xaml
```

### Import

The TOTP panel now exposes a masked sensitive setup-URI entry and **Import URI** action.

On import:

1. the bound URI field is cleared before parsing continues;
2. `ITotpUriCodec.Parse(...)` performs local bounded validation;
3. only after successful parsing are the existing item fields updated;
4. account name maps to Username/identifier;
5. issuer maps to Title when available;
6. Secret/algorithm/digits/period map to their existing TOTP fields;
7. fixed user-facing status/error text is used rather than echoing URI content;
8. the local managed reference is released when the operation leaves its scope, subject to ordinary .NET managed-memory limitations.

`ClearSensitiveState()` also clears `TotpUriImportText` when the Item Editor disappears/clears owned sensitive state.

### Copy

**Copy setup URI**:

1. creates a `TotpUriProfile` from the current existing TOTP fields;
2. calls `ITotpUriCodec.Format(...)`;
3. loads the configured clipboard-clear interval;
4. calls `IClipboardSecurityService.CopySecretAsync(...)`;
5. retains no new persisted setup-URI field.

A normal setup URI contains the long-lived TOTP seed. It therefore has a materially different exposure lifetime from one generated code even though both use the same conditional timed clipboard mechanism.

## 6. Unit-test coverage

Added/expanded:

```text
tests/CipherNest.UnitTests/TotpUriCodecTests.cs
```

Current coverage includes:

- canonical parse;
- standard omitted-parameter defaults;
- label issuer handling;
- explicit issuer handling;
- percent-encoded account/issuer behavior;
- canonical format/parse round trip;
- no-issuer formatting;
- well-formed unknown query parameter compatibility within the published query-name bound;
- wrong scheme rejection;
- HOTP rejection;
- `counter` rejection;
- missing secret rejection;
- case-insensitive duplicate-key rejection;
- unsupported algorithm/digits/period rejection;
- mismatched label/query issuer rejection;
- user-info/custom-port/fragment rejection;
- multi-segment label-path rejection;
- malformed query rejection;
- empty query-pair and trailing-separator rejection;
- invalid percent-encoding rejection for used and ignored/unknown parameters;
- URI-length/query-count rejection;
- exact 16-query-pair acceptance and first-over rejection;
- query-name ceiling;
- exact account/issuer boundary acceptance;
- account/issuer first-over-limit rejection;
- control/format metadata rejection;
- multi-colon/empty-issuer label rejection;
- colon-bearing formatter account/issuer rejection;
- invalid formatter seed/settings/metadata/enum rejection;
- encoded formatter output exceeding the URI ceiling.

These tests are deterministic repository coverage. They are not a compatibility certification for every third-party authenticator.

## 7. UI/source regression coverage

Updated:

```text
tests/CipherNest.UiTests/TotpUiSourceTests.cs
tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs
```

Current source/documentation invariants include:

- TOTP controls remain TOTP-item-only;
- code refresh/copy remain explicit;
- no background TOTP timer is introduced;
- URI import/copy commands are present;
- import/copy route through `ITotpUriCodec`;
- setup-URI input remains masked;
- HOTP rejection warning remains visible;
- setup-URI input is cleared after attempts and on sensitive-state cleanup;
- setup-URI copy uses the secret clipboard path;
- `VaultItem` does not gain a separate `TotpUri`/`OtpAuth` persisted property;
- `ITotpUriCodec -> TotpUriCodec` registration is present exactly once;
- the codec remains local-only and does not depend on `HttpClient`, `WebRequest`, ZXing, or camera code;
- parser constants and downstream TOTP validation remain visible at the source boundary;
- the August 18 verification record remains part of the required documentation suite;
- current-facing docs keep text-only `otpauth://totp/...` interoperability implemented rather than regressing to the old deferred claim;
- QR/HOTP/provider/universal-compatibility limitations remain visible.

## 8. Documentation synchronized in this continuation

Current-facing documentation updated during this continuation includes:

```text
README.md
CHANGELOG.md
PROJECT_STATUS.md
docs/README.md
docs/QUICK_START.md
docs/FEATURE_MATRIX.md
docs/UI_REFERENCE.md
docs/CONFIGURATION_REFERENCE.md
docs/COMPLETE_PROJECT_DOCUMENTATION.md
docs/USER_GUIDE.md
docs/FAQ.md
docs/DEVELOPER_GUIDE.md
docs/MAINTAINER_GUIDE.md
docs/API_REFERENCE.md
docs/LIMITS_AND_DEFAULTS.md
docs/NEXT_STEPS.md
docs/TEST_PLAN.md
docs/TESTING_GUIDE.md
docs/RELEASE_CHECKLIST.md
docs/releases/STORE_LISTING_GUIDE.md
docs/architecture/DATA_FLOW.md
docs/security/THREAT_MODEL.md
docs/security/TOTP.md
```

Historical dated verification/history records are intentionally not rewritten to pretend they described functionality that did not exist at their original SHA/date.

## 9. Security/trust conclusions

The implementation reduces parser ambiguity/resource abuse, but it does **not** authenticate the source of a URI.

A malicious person/application can provide a structurally valid TOTP URI containing attacker-chosen metadata/seed. CipherNest local parsing cannot prove:

- issuer identity;
- provider ownership;
- server-side enrollment state;
- that the URI belongs to the account the user intended.

Users must review imported metadata before saving.

The setup URI is also a secret-bearing transfer format. Copying it can expose the long-lived seed through:

- OS clipboard history;
- clipboard synchronization;
- another application;
- input/accessibility software;
- screenshots/screen recording;
- process-memory/runtime copies;
- the destination application after paste.

CipherNest's timed conditional cleanup is best effort and is not a guaranteed deletion mechanism for those external copies.

## 10. Historical verification baseline

The immutable implementation baseline used by the complete-documentation expansion before this August 18 feature remains:

```text
8566980ff981b8b4072f9010ec7b7ba54aba051e
```

Recorded evidence for that exact historical SHA:

```text
UnitTests:         346 passed
IntegrationTests:  98 passed
UI/source tests:   111 passed
Total:             555 passed, 0 failed, 0 skipped
Windows Release:   passed
Windows funding-disabled Release: passed
Android Release:   passed
iOS simulator Release: passed
Mac Catalyst Release: passed
CodeQL v4:         passed
```

Recorded runs:

```text
CipherNest CI: 31937127961
CodeQL:       31937127900
```

This August 18 feature does **not** inherit that exact-head verification status.

## 11. Current continuation verification status

During this continuation, source/code/test/documentation changes are being pushed as many small logical commits on `main`. The repository's push workflow uses `cancel-in-progress` concurrency, so intermediate branch runs can be superseded by later commits.

The environment performing this continuation does not provide a local .NET SDK, so it cannot truthfully substitute a local `dotnet build/test/format` result for the repository's configured CI.

Therefore the final August 18 head must be frozen before exact-head evidence is recorded. Until the final head's configured runs complete successfully, the correct status is:

```text
Repository implementation: present
Repository tests: added/expanded
Current-facing docs: synchronized
Final ambiguity/resource hardening: present
Historical verified baseline: preserved
Final August 18 exact-head core/platform/CodeQL verification: required
Independent professional security audit: not completed
```

## 12. Required exact-head automated gates

After the final August 18 head stops moving:

- run/observe core restore/build/test/format;
- run/observe Windows default Release;
- run/observe Windows funding-disabled Release;
- run/observe Android Release;
- run/observe iOS simulator Release;
- run/observe Mac Catalyst Release;
- run/observe CodeQL application analysis;
- run/observe dependency/security review as applicable;
- record exact SHA/run IDs/results without borrowing historical evidence.

Any compile/analyzer/test/format failure is a defect to fix; the release status must not be advanced merely because source inspection looks correct.

## 13. Required target/manual interoperability gates

Using synthetic seeds only:

- import representative compatible TOTP setup URIs;
- validate percent-encoded account/issuer metadata;
- validate SHA-1/SHA-256/SHA-512 where supported;
- validate 6/8-digit settings;
- validate representative 30/60-second periods;
- export and import the generated URI in representative compatible authenticator applications;
- confirm deliberate HOTP/counter rejection;
- confirm extra/empty issuer separators and colon-bearing account/issuer values are rejected as documented;
- confirm empty query pairs and malformed unknown parameter values are rejected;
- confirm import field clearing after success/failure/navigation;
- confirm no actual seed/URI leaks through semantic accessibility text;
- inspect platform clipboard history/sync behavior for copied setup URIs;
- confirm security warnings remain readable at large text/narrow widths;
- record provider/platform quirks without weakening the parser silently.

## 14. Release-claim boundary

The current source may accurately claim:

> CipherNest includes bounded local TOTP `otpauth://totp/...` text import and canonical setup-URI formatting/copy for existing TOTP vault items.

It must **not** claim from this implementation alone:

- universal authenticator/provider compatibility;
- QR scanning/rendering;
- camera enrollment;
- HOTP support;
- automatic provider enrollment;
- browser/application autofill;
- clipboard-history erasure;
- cryptographic factor separation when password and TOTP seed share the same unlocked vault;
- independent professional audit.

## 15. Commit/provenance note

The requested project commit identity is `Sanskar <sanskarin@outlook.in>`. The GitHub connector used for these edits does not expose an author-email override. Later commits in this continuation therefore include a `Signed-off-by: Sanskar <sanskarin@outlook.in>` trailer where commit-message text is available; that trailer must not be misrepresented as proof that Git author/committer email metadata itself was rewritten. Verify actual Git metadata independently as part of release provenance.
