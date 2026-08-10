# Database Schema

SQLite schema version 1:

- `VaultHeader(Id INTEGER PRIMARY KEY CHECK(Id=1), HeaderJson TEXT NOT NULL)` — non-secret version/KDF metadata plus wrapped DEK ciphertext/tag/nonce.
- `VaultItems(Id TEXT PRIMARY KEY, Envelope BLOB NOT NULL)` — opaque authenticated encrypted JSON payload. Item type, title, username, secret, URL, notes, tags, collection, custom fields, attachment references, review dates, favorites, trash state, and recent-use timestamp remain encrypted inside the envelope.
- `AppSettings(Key TEXT PRIMARY KEY, Value TEXT NOT NULL)` — reserved for non-secret application preferences; current JSON preference storage remains separate and non-secret.
- `MigrationHistory(Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL)` — records completed ordered schema migrations.

Attachments are stored as individually encrypted files under the app data directory and referenced from encrypted item payloads. Generated storage names use opaque GUID `N` identifiers plus `.cna`; filesystem access re-validates that format and rejects path separators or other names.

SQLite WAL files are treated as sensitive encrypted-container material. No plaintext item fields are written to SQL indexes or FTS tables.

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

`SqliteVaultStore.ReplaceDatabaseAsync` validates the replacement file **before** deleting WAL/SHM sidecars or moving the active database:

1. open the candidate read-only;
2. run `PRAGMA quick_check;` and require `ok`;
3. require exactly `AppConstants.DatabaseSchemaVersion`;
4. validate the required current table/column shape;
5. only then move/copy the active/replacement files.

If candidate validation fails, the current database is not touched. If the replacement copy itself fails after the old database was moved to `.previous`, CipherNest attempts to move the previous database back while preserving the original copy exception even if that rollback attempt also encounters an I/O/access error. Integration/source tests cover invalid-schema preservation and validation/rollback ordering.

Encrypted-backup restore uses this store boundary after its own authenticated container/path/size validation, so a valid SQLite signature alone is not sufficient to replace the active vault.

## Decrypted record boundary

Each `VaultItems.Id` is included as associated data when the encrypted envelope is authenticated. After decryption, `VaultService` additionally requires the payload's `VaultItem.Id` to equal that authenticated row ID and runs `VaultItemValidator` before returning the object to application/search/UI code. The plaintext JSON byte buffer is zeroed in `finally` regardless of validation outcome.

This validation rejects malformed runtime-null metadata, invalid enum/empty identifiers, over-limit note/field/tag/attachment data, and duplicate attachment identifiers/storage names instead of allowing malformed authenticated payload objects to propagate into later code paths.

## Search design

CipherNest intentionally does not create a SQLite full-text index because that would require plaintext searchable terms at rest. Search decrypts authenticated item envelopes only while the vault is unlocked and filters in process. Key-using reads run through per-session cancellable key leases; locking cancels the active session token so cancellable reads/exports do not deliberately continue after the vault is locked. This trades database-level indexing performance for smaller plaintext metadata exposure.
