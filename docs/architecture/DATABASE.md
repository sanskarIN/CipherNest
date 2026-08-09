# Database Schema

SQLite schema version 1:

- `VaultHeader(Id INTEGER PRIMARY KEY CHECK(Id=1), HeaderJson TEXT NOT NULL)` — non-secret version/KDF metadata plus wrapped DEK ciphertext/tag/nonce.
- `VaultItems(Id TEXT PRIMARY KEY, Envelope BLOB NOT NULL)` — opaque authenticated encrypted JSON payload. Item type, title, username, secret, URL, notes, tags, collection, custom fields, attachment references, review dates, favorites, trash state, and recent-use timestamp remain encrypted inside the envelope.
- `AppSettings(Key TEXT PRIMARY KEY, Value TEXT NOT NULL)` — reserved for non-secret application preferences; current JSON preference storage remains separate and non-secret.
- `MigrationHistory(Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL)` — records completed ordered schema migrations.

Attachments are stored as individually encrypted files under the app data directory and referenced from encrypted item payloads; filenames use opaque identifiers.

SQLite WAL files are treated as sensitive encrypted-container material. No plaintext item fields are written to SQL indexes or FTS tables.

## Migration runner

`DatabaseMigrator` applies ordered migrations transactionally. Initialization creates `MigrationHistory`, reads the highest completed schema version, rejects databases from a newer unsupported version, applies each missing migration inside a transaction, records its completion, and verifies that the final version matches `AppConstants.DatabaseSchemaVersion`.

Migration source must be append-only after release. A migration already shipped under a version number must never be silently rewritten. Backward-compatible restore tests and database-migration tests are release gates.

## Search design

CipherNest intentionally does not create a SQLite full-text index because that would require plaintext searchable terms at rest. Search decrypts authenticated item envelopes only while the vault is unlocked and filters in process. This trades database-level indexing performance for smaller plaintext metadata exposure.
