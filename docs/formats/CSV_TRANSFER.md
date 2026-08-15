# CipherNest CSV Transfer Format

CSV transfer is a plaintext interoperability feature. It is deliberately separate from encrypted backup/restore. A CSV source/export can expose readable vault fields outside CipherNest's encrypted-at-rest boundary.

## 1. Security boundary

Encrypted backup (`.cnbak`) is the recommended backup/transfer path.

CSV is intended for:

- importing records from other tools through explicit mapping;
- exporting interoperable plaintext data when the user deliberately accepts the risk.

Importing does not delete/encrypt the original CSV. Exported CSV can be retained by the OS, share target, cloud provider, backup/indexing/antivirus systems, or storage remnants.

## 2. Encoding

The parser uses strict UTF-8 decoding and rejects malformed UTF-8 byte sequences. It does not auto-detect alternate encodings. One optional UTF-8 BOM at the beginning of the stream is accepted explicitly.

Plaintext export writes UTF-8 with a BOM.

## 3. Parser resource limits

Current `CsvTransferService` limits:

```text
Maximum columns:             256
Maximum data rows:           100,000
Maximum header-name chars:   256
Maximum characters/field:    1,000,000
Maximum characters/row:      2,000,000
Maximum retained warnings:   20
```

The row limit is enforced by the streaming parser rather than materializing the whole CSV first. The dedicated header-name ceiling is intentionally much smaller than the general field ceiling because header strings are presented in the import-mapping UI and should never become an unbounded display/mapping surface.

## 4. CSV grammar supported by current parser

The parser supports standard-style comma-separated fields with quoting:

- comma delimiter;
- `"..."` quoted fields;
- doubled quote `""` inside a quoted field represents one quote;
- embedded commas are allowed inside quoted fields;
- embedded CR/LF content is allowed while inside quotes for normal fields, but header validation rejects control/formatting characters before a parsed header is returned;
- CRLF row endings are handled together;
- LF and CR row endings are accepted;
- characters after a closing quote are not accepted before delimiter/newline.

An input ending inside a quoted field is rejected.

## 5. Final-field safety

The maximum-column check is applied through the same `AddField` path for:

- comma-terminated fields;
- newline-terminated final fields;
- EOF-terminated final fields.

This prevents a final extra field from bypassing the 256-column ceiling simply because it is not followed by a comma.

## 6. Header rules

The first row is the header.

Required rules:

- at least one header column;
- at most 256 columns;
- no empty/whitespace-only header names;
- at most 256 UTF-16 characters in each header name;
- no Unicode control characters in a header name;
- no Unicode `Format` category characters in a header name, including invisible/bidirectional formatting controls;
- case-insensitive uniqueness.

Duplicate header names are rejected rather than silently choosing one occurrence. Control/format characters are rejected before headers can be surfaced in mapping UI so an imported file cannot use tabs, embedded line breaks, NULs, zero-width formatting marks, or bidirectional controls to create misleading column labels.

## 7. Explicit import mapping

`CsvImportMapping` supports:

```text
Title        required mapping target
Username     optional
Secret       optional
Url          optional
Notes        optional
Tags         optional
Collection   optional
Type         optional
```

The mapped Title column must exist. Any other non-empty mapped column name must also exist in the input header.

CipherNest does not silently infer that an arbitrary source column should be treated as a password/secret.

The App may suggest obvious header matches for convenience, but the user is expected to review the mapping before import.

## 8. Import row conversion

For each row:

1. mapped Title is retrieved and trimmed;
2. empty title causes the row to be skipped;
3. a new GUID is generated;
4. mapped values populate a new `VaultItem`;
5. tags are split on `;` or `,`, trimmed, and empty entries removed;
6. Type is parsed case-insensitively against `VaultItemType`;
7. an unrecognized/missing type falls back to `Login`;
8. Created/Modified timestamps use the injected `IClock.UtcNow`;
9. `VaultItemValidator` checks the item before save;
10. valid items are saved through `IVaultService.SaveItemAsync` and therefore encrypted by the normal vault path.

## 9. Import warnings

Import warnings are deliberately bounded to the first 20 retained warnings.

Warnings identify the logical row number and a fixed validation reason rather than including the row's raw secret-bearing contents.

For example, a skipped invalid row can report that a local validation rule failed without echoing the actual field value.

## 10. Import is not transactional across the entire file

The current import loop saves valid items one by one. If a later row/parser/platform failure stops the operation, earlier successfully saved rows can remain imported.

Therefore user-facing text says import has stopped and no *additional* rows will be imported until retry; it does not claim all-or-nothing rollback of the entire CSV.

A future whole-import transaction design would need memory/storage/concurrency review and explicit tests.

## 11. Source stream requirements

`ReadHeadersAsync` and `ImportCsvAsync` require a readable stream.

Import also requires the vault to be unlocked.

Unreadable streams are rejected before parsing.

## 12. Plaintext export gate

The App UI requires:

- exact phrase `EXPORT PLAINTEXT`;
- current-master re-authentication;
- separate final confirmation/warning.

Recovery keys are not accepted as a substitute for current-master confirmation for plaintext export.

This UI authorization is intentionally outside `IPlaintextTransferService`; the transfer service assumes the caller has already established the appropriate application authorization.

## 13. Exported columns

Current header line is exactly:

```text
Title,Type,Username,Secret,URL,Notes,Tags,Collection,Favorite,ReviewAfterUtc
```

Each active non-trash item is exported with:

| Column | Source |
|---|---|
| Title | `VaultItem.Title` |
| Type | `VaultItem.Type.ToString()` |
| Username | `VaultItem.Username` |
| Secret | `VaultItem.Secret` |
| URL | `VaultItem.Url` |
| Notes | `VaultItem.Notes` |
| Tags | tags joined with `;` |
| Collection | `VaultItem.Collection` |
| Favorite | `true` / `false` |
| ReviewAfterUtc | ISO-8601 round-trip (`O`) or empty |

Current CSV export does **not** include:

- attachments;
- custom fields;
- created/modified/last-accessed/deleted timestamps;
- recovery/master/secondary credentials;
- encrypted storage envelopes.

Do not assume CSV is a complete fidelity backup.

## 14. CSV escaping

An exported field is quoted when it contains any of:

```text
comma
quote
CR
LF
```

Inside a quoted field, each `"` is doubled.

Other fields are emitted without quotes.

## 15. Export destination stream

`ExportCsvAsync` requires a writable stream and an unlocked vault.

It retrieves active items through `IVaultService.GetItemsAsync(includeTrash: false)`, writes the CSV through a buffered `StreamWriter`, checks cancellation between items, and flushes at completion.

The caller controls where plaintext is written.

## 16. MAUI export staging

The current App export UI:

1. creates an app-cache `plaintext-exports` directory;
2. creates a unique timestamped plaintext CSV with `FileMode.CreateNew`;
3. calls `ExportCsvAsync`;
4. sends it to `Share.Default.RequestAsync`;
5. attempts deletion in `finally`;
6. privacy-safe reports cleanup failure;
7. keeps a manual “Clean plaintext export cache” action for leftovers the OS previously prevented removing.

Cleanup cannot remove copies retained outside CipherNest's cache.

## 17. Sensitive-field omissions and semantics

CSV transfer is generic interoperability, not a credential-manager universal schema.

Important consequences:

- custom fields are not exported by the current CSV exporter;
- attachments are not exported;
- source-specific metadata from another password manager is ignored unless mapped to a supported field;
- unsupported item types default to Login on import;
- Favorites and ReviewAfterUtc are currently export-only fields; `CsvImportMapping` does not map them;
- deleted/trash items are excluded from export.

Use encrypted backup when exact CipherNest fidelity is required.

## 18. Malformed input rejection examples

The parser rejects conditions such as:

- empty CSV;
- empty header names;
- header names longer than 256 characters;
- control or invisible Unicode formatting characters in header names;
- duplicate header names;
- too many columns;
- too many rows;
- field over 1,000,000 characters;
- row over 2,000,000 characters;
- malformed UTF-8;
- EOF inside a quoted field;
- characters after closing quote before delimiter/newline;
- mapped column not present in header.

Integration tests include fixed malformed examples plus a deterministic adversarial header corpus. The corpus is intentionally deterministic so a failing input is reproducible in CI rather than dependent on ambient randomness.

## 19. Privacy/diagnostic behavior

File/open/parser/import/export failures at the App boundary use fixed user-facing messages plus the privacy-safe reporter rather than displaying raw filesystem exception messages/paths.

Do not change warnings to include raw source rows or secret fields for debugging convenience.

## 20. Compatibility notes

The current CSV format is a best-effort human/interoperability format, not a versioned cryptographic container.

If exported columns are added/renamed/reordered, consider:

- compatibility with existing imports/scripts;
- whether `CsvImportMapping` should support the new field;
- whether the field is safe/desirable to expose in plaintext;
- documentation and release notes;
- parser/round-trip tests.

Do not add recovery keys, master passphrases, backup passphrases, or biometric secondary secrets to CSV.
