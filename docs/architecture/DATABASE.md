# Database Schema

SQLite schema version 1:

- `VaultHeader(Id INTEGER PRIMARY KEY CHECK(Id=1), HeaderJson TEXT NOT NULL)` — non-secret version/KDF metadata plus wrapped DEK ciphertext/tag/nonce.
- `VaultItems(Id TEXT PRIMARY KEY, Envelope BLOB NOT NULL)` — opaque authenticated encrypted JSON payload. Item type/title/notes/tags remain encrypted.
- `AppSettings(Key TEXT PRIMARY KEY, Value TEXT NOT NULL)` — non-secret application preferences only.
- `MigrationHistory(Version INTEGER PRIMARY KEY, AppliedUtc TEXT NOT NULL)`.

Attachments are stored as individually encrypted files under the app data directory and referenced from encrypted item payloads; filenames use opaque identifiers.

SQLite WAL files are treated as sensitive encrypted-container material. No plaintext item fields are written to SQL indexes or FTS tables.
