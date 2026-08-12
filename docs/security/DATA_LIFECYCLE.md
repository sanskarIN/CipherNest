# CipherNest Sensitive Data Lifecycle

This document records where sensitive values can exist, what the application deliberately clears, and where deterministic deletion cannot be promised.

## Scope

Sensitive data includes:

- master passphrase;
- recovery material;
- backup passphrase;
- biometric secondary secret;
- vault data-encryption key (DEK) and derived key-encryption keys (KEKs);
- decrypted vault records;
- usernames/secrets/custom secret fields;
- decrypted attachments/previews;
- plaintext CSV data;
- clipboard content;
- filesystem/share staging created by explicit export.

## 1. Master passphrase

### Entry

The master passphrase exists as a MAUI-bound managed string while entered.

### Cryptographic processing

`CryptoService` validates the character length, allocates a UTF-8 byte buffer for Argon2id, and zeroes that owned byte buffer in `finally`.

Derived KEK byte arrays are zeroed where owned by the service.

### Persistence

CipherNest does not write the master passphrase to the vault database, settings JSON, backup header, or biometric secure storage.

### UI lifetime

Sensitive ViewModels clear bound passphrase fields before long work or when leaving sensitive pages where practical.

### Limitation

Clearing a property/local reference does not erase the immutable .NET string object or every runtime/GC/UI copy. CipherNest does not claim deterministic managed-string erasure.

## 2. Recovery material

Recovery material is generated for the optional independent wrapper and shown during onboarding so the user can store it separately.

CipherNest does not intentionally persist the plaintext recovery value in the local vault after setup.

The Onboarding page clears the bound recovery value when leaving/continuing through the relevant flow.

Users must not send recovery material to public issues/support logs or store it only beside the encrypted vault on the same device.

## 3. Backup passphrase

The backup passphrase is a managed UI string used to derive the separate encrypted-backup key.

The bound Settings field is cleared before longer file-picker/share/backup/restore work where practical, and local method references are cleared afterward.

The same managed-string limitation applies.

The plaintext backup passphrase is not written into `.cnbak`.

## 4. Biometric secondary secret

When enabled, the App generates a high-entropy random secondary secret after current-master and biometric authentication.

- raw random bytes are zeroed after converting to the representation used by secure storage;
- the secondary secret managed string can exist transiently in App code;
- platform `SecureStorage` stores the secret for convenience unlock;
- the master passphrase is not stored for biometric convenience unlock;
- disable/restore/full-vault cleanup attempts to clear the stored secondary secret.

Platform secure-storage internals, backups, hardware binding, and physical deletion guarantees are controlled by the OS and are not claimed by CipherNest.

## 5. Vault DEK

### Creation

A random 32-byte DEK is generated and wrapped by credential-derived keys.

### Locked state

The plaintext DEK should not remain as active shared vault state.

### Unlocked state

A shared owned 32-byte DEK buffer exists for the active session. Access is synchronized.

### Per-operation use

Key-sensitive work gets a private `VaultKeyLease` copy rather than retaining the mutable shared array.

### Lock/replacement

The shared DEK is zeroed and removed. The session token is cancelled. Each lease zeroes its private key copy on disposal.

### Limitation

Privileged process-memory inspection while unlocked remains outside the protection guarantee.

## 6. Derived KEKs and passphrase UTF-8 buffers

Argon2id requires byte input and produces a derived key. CipherNest zeroes owned temporary passphrase UTF-8 and KEK buffers in cryptographic paths where practical.

Library/runtime/native internal copies are not guaranteed to be erasable by application code.

## 7. Decrypted vault item JSON

Encrypted records are decrypted into owned byte arrays, deserialized, validated, then the plaintext byte arrays are zeroed in `finally`.

Deserialized `VaultItem` strings/objects can remain in managed memory while used by search/UI and until GC/runtime reclamation.

The current architecture accepts this local in-memory exposure while unlocked; it does not create a plaintext persistent search index.

## 8. ViewModel decrypted state

Sensitive pages clear relevant bound state on disappearance. Item Editor additionally masks protected items until current-master re-authentication.

Clearing reduces references and visible lifetime. It is not process-memory sanitization.

Avoid adding global/static caches of decrypted `VaultItem` objects or secrets.

## 9. Secure-note preview

Secure-note content is stored inside encrypted item JSON.

Preview creates managed strings/models for safe Markdown-like rendering. Raw HTML is neutralized and size/line counts are bounded.

Preview managed strings cannot be deterministically wiped.

## 10. Attachment encryption plaintext buffers

Attachment import uses a reusable 256 KiB byte buffer.

For each filled chunk:

1. bytes are authenticated/encrypted;
2. the used plaintext span is zeroed;
3. the whole buffer is zeroed again on exit.

This limits avoidable plaintext retention in owned byte arrays.

The source stream/file is external input and remains outside CipherNest's deletion responsibility.

## 11. Attachment decryption buffers

Each encrypted attachment chunk decrypts to an owned plaintext byte array that is written to the requested destination and zeroed in `finally`.

The destination can be:

- an in-memory preview buffer;
- an explicit caller stream;
- a temporary plaintext export file.

Each destination has different lifetime guarantees.

## 12. In-memory attachment preview

For supported text-family files up to 512 KiB:

- decrypted bytes are written into a `MemoryStream`;
- strict UTF-8 decoding creates a managed string;
- control characters/angle brackets are sanitized/neutralized;
- display is truncated after 20,000 characters;
- owned backing byte buffer is zeroed when the preview flow exits where accessible.

The decoded string may remain until GC/runtime reclamation.

## 13. Plaintext attachment export

Explicit export creates a unique temporary plaintext file in app cache because OS sharing commonly requires a file.

CipherNest attempts deletion after the share request returns and warns when it cannot confirm cleanup.

CipherNest cannot delete:

- copies created by the receiving app;
- provider/cloud uploads;
- filesystem snapshots;
- antivirus/indexer copies;
- OS share-service caches;
- physical flash remnants;
- external backups.

## 14. CSV import plaintext

The selected CSV remains an external plaintext file. CipherNest reads it through a bounded stream parser and saves mapped valid rows as encrypted vault items.

CipherNest does not delete or encrypt the original CSV.

Parser `StringBuilder`/managed strings used for fields are not deterministic-erasure buffers. Avoid importing data from a location whose plaintext retention is unacceptable.

## 15. Plaintext CSV export

Export writes decrypted item fields to a unique app-cache CSV and passes it to the OS share flow.

It is gated by current-master re-authentication plus exact acknowledgement `EXPORT PLAINTEXT` and a separate warning.

The application attempts to delete its staging file afterward. External copies/remnants remain outside the guarantee.

Attachments are not included in CSV export.

## 16. Encrypted backups

A backup archive is built from:

- a consistent SQLite snapshot containing encrypted records/header structural data;
- already-encrypted `.cna` attachment containers.

The ZIP/archive staging contains encrypted vault material but is still treated as sensitive application data. The archive is then encrypted/authenticated under a backup-passphrase-derived key.

Owned reusable plaintext archive chunk buffers are zeroed after encryption and on exit where implemented.

The final `.cnbak` remains encrypted but should still be protected from deletion/loss because confidentiality is only one part of backup security.

## 17. Restore staging

Restore creates app-cache/staging data while authenticating/extracting the backup.

- encrypted source `.cnbak` copy can exist in cache temporarily;
- decrypted outer backup payload is a ZIP containing the encrypted SQLite database and encrypted attachments rather than plaintext item fields;
- staged DB/attachment data is validated before replacement;
- temporary restore artifacts are removed best-effort;
- active DB/WAL/SHM recovery copies exist transiently during replacement and are cleaned after success or used during rollback.

Failure cleanup must not hide the original restore error.

## 18. Clipboard

The OS clipboard contains plaintext after an explicit copy.

CipherNest does not retain the copied plaintext in delayed timer state; it keeps a SHA-256 fingerprint for matching. Owned fingerprint/hashing buffers are zeroed where practical.

Clipboard history/sync/other apps remain external.

The fingerprint is not intended as a password hash or secure password storage. If process memory is compromised, low-entropy secret guessing against a fingerprint can still be a concern.

## 19. Settings JSON

`settings.json` contains non-secret preferences such as theme/lock/clipboard/trash/reminder/generator/biometric preference flags and backup timestamp.

It must not be used to store:

- master passphrase;
- recovery material;
- DEK/KEK;
- backup passphrase;
- vault item plaintext.

## 20. SQLite at rest

The database stores:

- vault header/key-wrapper metadata;
- encrypted item envelopes;
- app structural/settings/migration metadata.

Searchable vault item fields remain encrypted inside item payloads; there is no plaintext vault FTS index.

SQLite WAL/SHM/recovery files are part of the managed persistence footprint and must be considered in snapshot/replacement/deletion logic.

## 21. Encrypted attachment store at rest

Files use opaque GUID `.cna` names and authenticated encrypted contents. User display names/media types are referenced inside encrypted `VaultItem` payloads.

Filesystem metadata such as file sizes/timestamps may still reveal coarse operational information; the current design does not claim padding/traffic-analysis resistance.

## 22. Diagnostics

Privacy-safe diagnostics intentionally omit exception messages/stacks and decrypted vault content.

Do not add diagnostic fields containing:

- user-entered secrets;
- raw paths to sensitive documents;
- decrypted item titles/notes;
- recovery values;
- encrypted payloads solely for debugging;
- secure-storage contents.

## 23. Logs in DEBUG builds

The App can enable normal .NET debug logging infrastructure under `DEBUG`. Developers must still avoid writing sensitive values to debug logs, traces, `Console`, `Debug.WriteLine`, or exception messages intentionally surfaced by custom code.

## 24. Logical deletion versus physical sanitization

CipherNest can delete application-managed files/records where the OS permits it. It cannot guarantee secure physical erase on modern flash/filesystems.

Never document local delete/empty-trash/full-vault-delete as cryptographic/physical sanitization.

## 25. Support/issue handling

Maintainers should never ask a user to upload/send:

- a real vault database;
- master/backup passphrases;
- recovery material;
- decrypted `.cnbak` contents;
- real encrypted backup plus credential;
- decrypted attachments;
- screenshots exposing credentials;
- secure-storage dumps.

Use synthetic reproduction cases and privacy-safe diagnostics instead.

## 26. Data-lifetime review checklist

For every new feature, identify:

1. plaintext input source;
2. managed string/object copies;
3. owned byte/char arrays that can be zeroed;
4. persistent storage locations;
5. cache/temp locations;
6. OS/platform boundaries;
7. network boundaries, if any;
8. logs/diagnostics;
9. cancellation/error cleanup behavior;
10. what the app can and cannot delete afterward.

If any new network/server boundary is added, update the privacy notice and threat model before implementation is represented as complete.
