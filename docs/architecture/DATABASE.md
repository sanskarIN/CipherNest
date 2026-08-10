# Database Schema

SQLite schema version 1:

- `VaultHeader(Id INTEGER PRIMARY KEY CHECK(Id=1), HeaderJson TEXT NOT NULL)` — non-secret version/KDF metadata plus wrapped DEK ciphertext/tag/nonce.
- `VaultItems(Id TEXT PRIMARY KEY, Envelope BLOB NOT NULL)` — opaque authenticated encrypted JSON payload. Item type, title, username, secret, URL, notes, tags, collection, custom fields, attachment references, review dates, favorites, trash state, and recent-use timestamp remain encrypted inside the envelope.
- `AppSettings(Key TEXT PRIMARY KEY, Value TEXT NOT NULL)` — reserved for non-secret application preferences; current JSON preference storage remains separate and non-secret.
- `MigrationHistory(Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL)` — records completed ordered schema migrations.

Attachments are stored as individually encrypted files under the app data directory and referenced from encrypted item payloads. Generated storage names use opaque GUID `N` identifiers plus `.cna`; filesystem access re-validates that format and rejects path separators or other names.

SQLite WAL files are treated as sensitive encrypted-container material. No plaintext item fields are written to SQL indexes or FTS tables.

## Storage resource budgets

The current source treats database length/count metadata as untrusted resource input and applies explicit budgets:

- vault-header JSON: maximum 64 KiB UTF-8;
- serialized decrypted item JSON: maximum 16 MiB;
- stored encrypted item envelope: maximum 24 MiB per row;
- item count: maximum 100,000 rows;
- aggregate stored encrypted envelope bytes: maximum 256 MiB;
- application-level combined item text: maximum 2,000,000 characters before serialization.

`ReadHeaderAsync` reads the UTF-8 byte length before materializing header text. `ReadAllItemsAsync` checks aggregate count/bytes and each `length(Envelope)` before reading the BLOB. Writes enforce the corresponding limits as well. Stored item IDs must be canonical lower-case GUID `D` strings; after decryption the payload ID must still equal the authenticated row ID.

These are safety/resource limits, not recommendations that ordinary vaults should approach them. Raising them requires memory/performance/security review and compatibility testing.

## Migration runner

`DatabaseMigrator` applies ordered migrations transactionally. Initialization creates `MigrationHistory`, reads the highest completed schema version, rejects databases from a newer unsupported version, applies each missing migration inside a transaction, records its completion, and verifies that the final version matches `AppConstants.DatabaseSchemaVersion`.

After migration/version resolution, the current implementation also validates the required schema shape by preparing zero-row reads for:

- `VaultHeader(Id, HeaderJson)`
- `VaultItems(Id, Envelope)`
- `AppSettings(Key, Value)`
- `MigrationHistory(Version, AppliedUtc)`

This prevents a database that merely forges a current-version `MigrationHistory` row from being accepted while required tables/columns are missing. Migration rollback is best-effort with an uncancelled rollback token, and a secondary rollback failure is prevented from replacing the original migration error.

Migration source must be append-only after release. A migration already shipped under a version number must never be silently rewritten. Backward-compatible restore tests and database-migration tests are release gates.

## Restore / database replacement boundary

`SqliteVaultStore.ReplaceDatabaseAsync` validates the replacement file **before** active database/WAL/SHM mutation:

1. open the candidate read-only;
2. run `PRAGMA quick_check;` and require `ok`;
3. require exactly `AppConstants.DatabaseSchemaVersion`;
4. validate the required current table/column shape;
5. require a bounded vault header;
6. validate item count, aggregate envelope bytes, per-envelope sizes, and canonical item IDs;
7. only then stage the active SQLite file set and install the replacement database.

The active database, WAL, and SHM are staged into a unique `.previous.<guid>` recovery file set. If staging or replacement fails, rollback restores only recovery components that actually exist. This matters for partial staging: a WAL or SHM that failed to move is not deleted simply because another component was staged successfully.

If candidate validation fails, the current database is not touched. Successful replacement cleans recovery artifacts best-effort. Full-vault deletion also sweeps the unique recovery naming pattern.

Encrypted-backup restore uses this store boundary after its own authenticated container/path/size validation, so a valid SQLite signature or even a structurally correct schema alone is not sufficient to replace the active vault.

## Decrypted record boundary

Each `VaultItems.Id` is included as associated data when the encrypted envelope is authenticated. After decryption, `VaultService` additionally requires the payload's `VaultItem.Id` to equal that authenticated row ID and runs `VaultItemValidator` before returning the object to application/search/UI code. The plaintext JSON byte buffer is zeroed in `finally` regardless of validation outcome.

`VaultService` independently enforces the 16 MiB plaintext-JSON and 24 MiB stored-envelope limits so alternate `IVaultStore` implementations cannot bypass the SQLite storage budgets. Validation also rejects malformed runtime-null metadata, invalid enum/empty identifiers, over-limit note/field/tag/attachment data, excessive aggregate item text, and duplicate attachment identifiers/storage names.

## Search design

CipherNest intentionally does not create a SQLite full-text index because that would require plaintext searchable terms at rest. Search decrypts authenticated item envelopes only while the vault is unlocked and filters in process. Key-using reads run through per-session cancellable key leases; locking cancels the active session token so cancellable reads/exports do not deliberately continue after the vault is locked. This trades database-level indexing performance for smaller plaintext metadata exposure.
