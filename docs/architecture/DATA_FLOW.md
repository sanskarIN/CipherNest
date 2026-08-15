# CipherNest Data Flow

This document follows sensitive data from input through encryption, storage, use, export, backup, restore, and deletion. It complements `ARCHITECTURE.md`, `DATABASE.md`, `SESSION_AND_CONCURRENCY.md`, `../security/CRYPTOGRAPHIC_DESIGN.md`, and `../security/DATA_LIFECYCLE.md`.

## 1. First-run vault creation

```text
User master passphrase
        |
        v
App Onboarding/ViewModel
        |
        v
IVaultService.CreateAsync
        |
        +--> random 256-bit vault DEK
        |
        +--> Argon2id(master, random salt) -> KEK
        |            |
        |            v
        |      AES-GCM wrap DEK
        |
        +--> optional random recovery material
                     |
                     v
              separate DEK wrapper
        |
        v
self-validated current-v2 vault header JSON
(exact root/wrapper/KDF schema; <=64 KiB; depth <=16)
        |
        v
SQLite VaultHeader
```

The master passphrase is not written to SQLite/settings. Owned UTF-8/KDF/key buffers are zeroed where practical, but immutable managed strings/runtime copies cannot be deterministically erased.

## 2. Master/recovery unlock

```text
entered credential
      |
      v
VaultService
      |
      +--> read byte-bounded vault header
      +--> strict v1/v2 root/wrapper/KDF JSON validation (depth <=16)
      +--> typed header/version/resource validation
      +--> select master or recovery wrapper path
      +--> validate KDF resource metadata
      +--> Argon2id -> KEK
      +--> AES-GCM authenticate/unwrap DEK
      |
      v
owned shared unlocked-session DEK
      |
      +--> per-session cancellation token
      +--> LockStateChanged(true)
```

Invalid credential or malformed wrapper authentication maps to vault authentication failure. Duplicate/unknown/missing/wrong-kind/deep vault-header JSON and future/unsupported versions are rejected before typed deserialization/wrapped-key unwrap. Historical v1 remains readable; current header mutations self-validate and write v2.

## 3. Secondary/biometric convenience unlock

```text
OS biometric prompt
      |
      v
App biometric service
      |
      +--> if allowed by fresh-process/periodic-master rules
      |
      v
platform secure storage -> random secondary secret
      |
      v
VaultService.UnlockWithSecondarySecretAsync
      |
      v
secondary wrapper authenticates/decrypts same DEK
```

The master passphrase is not stored/replayed for biometric unlock. Backup restore clears local secondary-secret pairing/configuration because restored wrapper metadata may not match current secure storage.

## 4. Saving a vault item

```text
ViewModel/domain VaultItem
      |
      v
VaultItem normalization/validation
      |
      +--> field/count/aggregate resource limits
      +--> secure-note shared limits
      +--> attachment metadata/identity validation
      |
      v
VaultService obtains VaultKeyLease
      |
      v
serialize item -> bounded UTF-8 JSON plaintext bytes
      |
      v
AES-GCM encrypt with fresh nonce
AAD = item GUID bytes
      |
      v
StoredVaultItem(Guid Id, opaque encrypted envelope)
      |
      v
IVaultStore / SQLite VaultItems
```

The row ID must remain canonical and is authenticated by record AAD. Searchable fields are not copied into plaintext SQL indexes.

## 5. Reading items

```text
SQLite stored row(s)
      |
      +--> count/aggregate/per-envelope resource checks
      |
      v
StoredVaultItem
      |
      v
VaultService obtains VaultKeyLease
      |
      v
AES-GCM authenticate/decrypt using row GUID as AAD
      |
      v
bounded plaintext JSON bytes
      |
      v
deserialize VaultItem
      |
      +--> payload ID must equal authenticated row ID
      +--> VaultItemValidator must pass
      |
      v
Domain item returned to unlocked application/UI
```

Owned plaintext JSON byte arrays are zeroed after deserialization/validation. The resulting managed domain strings/objects remain subject to .NET garbage-collection/memory-lifetime limitations.

## 6. Search/filter/sort/audit

```text
GetItemsAsync -> decrypted authenticated item objects
      |
      +--> local search query match
      +--> favorites/type/collection/review filters
      +--> local sort
      +--> security audit
      |
      v
50-item incremental visual rendering where applicable
```

There is no plaintext SQLite FTS/search index in the current design. Search input is bounded. The fact that local processing occurs in memory is a privacy tradeoff documented in the threat model.

## 7. Recent-use timestamps

When an item opens successfully, `MarkAccessedAsync` updates encrypted `LastAccessedUtc` without changing the user's modification timestamp. The timestamp remains inside the encrypted record payload.

## 8. Attachment import

```text
platform file picker
      |
      v
readable source Stream
      |
      +--> normalize leaf display name
      +--> normalize/bound media type
      +--> enforce file/count/global budgets
      |
      v
VaultService obtains VaultKeyLease
      |
      v
EncryptedAttachmentStore
      |
      +--> canonical opaque name: <attachment-id-N>.cna
      +--> unique CreateNew temp staging
      +--> read/fill bounded 256 KiB plaintext chunks
      +--> AES-GCM encrypt each chunk
      |      AAD = item ID + attachment ID + chunk index
      +--> zero reusable plaintext buffer
      +--> append end marker
      +--> move staging -> final without overwrite
      |
      v
update encrypted VaultItem attachment metadata
```

Attachment mutation is serialized separately from session transitions so a security lock can cancel long attachment work rather than waiting behind it.

## 9. Attachment in-memory text preview

```text
AttachmentReference
      |
      +--> supported text-family type check
      +--> <= 512 KiB plaintext limit
      |
      v
VaultService.ExportAttachmentAsync -> MemoryStream
      |
      +--> authenticated chunk decryption
      +--> strict UTF-8 decode
      +--> control-character sanitization
      +--> angle-bracket neutralization
      +--> display truncate after 20,000 chars
      |
      v
MAUI alert/display
```

The preview path does not intentionally create a plaintext file. The decoded managed string may remain in process memory until reclaimed by the runtime.

## 10. Attachment plaintext export

```text
explicit user warning/confirmation
      |
      v
unique app-cache plaintext path
      |
      v
VaultService.ExportAttachmentAsync
      |
      v
OS ShareFile request
      |
      v
best-effort delete app-cache staging in finally
```

Once plaintext is handed to an OS share target, CipherNest cannot delete destination copies, provider caches, OS backups, snapshots, antivirus/indexing copies, or physical remnants.

## 11. CSV import

```text
external plaintext CSV
      |
      v
platform picker -> readable stream
      |
      v
bounded CsvParser
      |
      +--> <=256 columns
      +--> <=100,000 data rows
      +--> <=1,000,000 chars/field
      +--> <=2,000,000 chars/row
      +--> strict quote/header rules
      |
      v
explicit CsvImportMapping
      |
      v
construct + validate VaultItem
      |
      v
VaultService.SaveItemAsync -> encrypted SQLite record
```

Importing does not alter/delete the original external plaintext file.

## 12. Plaintext CSV export

```text
exact acknowledgement phrase
      + current-master re-authentication
      + explicit confirmation
      |
      v
GetItemsAsync(decrypted)
      |
      v
UTF-8 CSV plaintext -> unique cache file
      |
      v
OS share request
      |
      v
best-effort delete temporary CSV
```

Attachments are not included. Destination/OS copies remain outside CipherNest control.

## 13. Encrypted backup creation

```text
backup passphrase
      |
      v
Settings confirms lock-and-back-up
      |
      v
VaultService.LockAsync
      |
      v
IVaultStore.CreateConsistentSnapshotAsync
      |
      v
snapshot vault.db + encrypted .cna files
      |
      v
bounded ZIP/archive staging
      |
      +--> <=10,001 entries
      +--> <=1 GiB aggregate plaintext archive bytes
      |
      v
backup header + Argon2id(backup passphrase)
      |
      v
1 MiB AES-GCM encrypted chunks
      |
      v
unique encrypted sibling staging
      |
      v
final .cnbak destination
```

Backup path policy prevents export over the active DB/WAL/SHM/recovery set or into the encrypted attachment store.

## 14. Encrypted backup restore

```text
selected .cnbak + backup passphrase
      |
      v
copy to temporary app-cache staging
      |
      v
validate magic/header framing
      |
      +--> format version/salt/KDF/chunk size before Argon2
      |
      v
derive backup key
      |
      v
authenticate/decrypt bounded chunks
      |
      v
bounded ZIP extraction
      |
      +--> path allowlist
      +--> duplicate normalized path rejection
      +--> entry count/aggregate size limits
      +--> .cna container-size limits
      |
      v
staged vault.db candidate validation
      |
      +--> SQLite quick_check
      +--> exact supported schema version
      +--> required table/column shape
      +--> byte/depth-bounded strict supported vault-header schema
      +--> canonical item IDs
      +--> count/per-envelope/aggregate limits
      |
      v
active DB/WAL/SHM staged to unique recovery set
      |
      v
candidate installed
      |
      +--> on failure after mutation: uncancelled rollback path
      |
      v
attachment replacement/recovery
      |
      v
clear local biometric secondary secret/pairing
```

Temporary restore staging is removed best-effort. A cleanup error is reported without replacing the primary restore result where designed.

## 15. Settings persistence

```text
AppPreferences
      |
      v
AppPreferencesPolicy.Normalize
      |
      v
JSON serialize + 64 KiB safety check
      |
      v
unique sibling CreateNew temp file
      |
      v
replace settings.json
```

Loading bounds the file before JSON parse, normalizes values, and falls back to defaults for malformed/unreadable non-secret preferences. Cancellation is not intentionally converted into fallback success.

## 16. Clipboard copy/clear

```text
explicit username/secret copy action
      |
      v
platform clipboard plaintext
      |
      v
SHA-256 fingerprint retained for scheduled comparison
      |
      v
later: hash current clipboard + fixed-time compare
      |
      +--> match: request clear
      +--> mismatch: preserve newer unrelated clipboard content
```

The delayed security state avoids retaining the copied plaintext in its timer state, but OS clipboard history/sync and other apps remain external risks.

## 17. Lock transition

```text
manual/background/timeout/change-master/delete transition
      |
      v
serialized session transition gate
      |
      v
remove/zero shared DEK under synchronization
      |
      v
cancel/dispose active session CTS
      |
      v
in-flight session-linked leases observe cancellation
      |
      v
LockStateChanged(false)
      |
      +--> conditional clipboard cleanup where requested
```

Each private key lease zeroes its copied DEK on disposal. Locking does not claim to erase immutable managed strings already created by the application/runtime.

## 18. Trash/permanent deletion

### Trash

`DeletedUtc` is encrypted inside the item record. Routine maintenance removes expired trash according to the normalized retention policy.

### Permanent item deletion

```text
current-master re-authentication + confirmation (manual UI path)
      |
      v
serialized attachment mutation
      |
      v
snapshot attachment storage names
      |
      v
delete authenticated DB record first
      |
      v
best-effort delete encrypted attachment containers
```

Database-record deletion first prevents an unsuccessful database delete from leaving a surviving item whose referenced files were already intentionally removed.

## 19. Full local-vault deletion

```text
exact destructive phrase + current master passphrase
      |
      v
VaultService re-authentication
      |
      v
live VaultKeyLease authorization
      |
      v
wait for security transition gate using live-session token
      |
      +--> intervening lock/unlock cancels stale authorization
      |
      v
clear session key
      |
      v
delete CipherNest-managed attachment/db file set
      |
      v
clear biometric session/pairing in App flow
```

After the service crosses the destructive session commit point, database deletion uses an uncancelled token so caller cancellation does not leave an intentionally half-committed security transition.

## 20. Diagnostics

```text
exception at guarded application/platform boundary
      |
      v
fixed user-facing message
      |
      v
IPrivacySafeExceptionReporter
      |
      v
sanitized operation + exception type + HResult + severity
```

Exception messages/stacks and decrypted vault context are intentionally omitted from the privacy-safe report path.

## 21. Future data-flow warning

Cloud sync/accounts/collaboration/autofill/TOTP/server storage are not part of this graph. Any future feature that moves vault data across process/device/network boundaries requires a new protocol/data-flow diagram, threat model, privacy assessment, compatibility/version design, and release gate before implementation.

## TOTP seed and code flow

A `OneTimePassword` item's Base32 seed and algorithm/digit/period settings follow the normal authenticated encrypted `VaultItem` path. After the item is decrypted while unlocked, explicit **Refresh code** passes the seed/settings plus current UTC time to `ITotpService`. The Infrastructure implementation performs bounded Base32 validation/decoding, HMAC generation, and dynamic truncation locally; the returned decimal code exists only in ViewModel presentation state and is not written to SQLite or backup as a generated-code field.

Changing the seed/settings/item type clears displayed code state. A re-authentication-protected item cannot generate/display its code until current-master re-authentication succeeds. Explicit **Copy code** recalculates before copying through `IClipboardSecurityService`, so the same conditional timed-cleanup and OS clipboard-history limitations apply as for other secrets.

The encrypted backup path naturally carries the encrypted seed/settings because it carries the encrypted vault database. QR/`otpauth://` import/export and autofill/provider enrollment are outside this current flow.
