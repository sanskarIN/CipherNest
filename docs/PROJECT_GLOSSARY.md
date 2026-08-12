# CipherNest Project Glossary

## AAD / Associated Data

Authenticated but not encrypted context supplied to AES-GCM. CipherNest uses associated data to bind encrypted material to its purpose/identity, such as item GUIDs, attachment item/attachment/chunk identity, and key-wrapper context.

## Active vault

The current local SQLite database plus encrypted attachment directory used by the running installation.

## Aggregate storage budget

A resource ceiling applied across many records/entries, such as the 256 MiB aggregate stored-envelope budget or 1 GiB backup archive budget. Aggregate limits complement per-record/per-file limits.

## App preferences

Non-secret local settings represented by `AppPreferences`. Values are normalized by `AppPreferencesPolicy` rather than blindly trusting deserialized data.

## Attachment container

A `.cna` file in the app's encrypted attachment directory. It uses `CNAT0001` framing and bounded AES-GCM chunks. Its opaque filename is derived from the attachment GUID rather than the user display name.

## Backup container

A `.cnbak` authenticated encrypted file using backup format version 2 / `CNBK0002`. It contains an encrypted bounded ZIP payload made from a consistent SQLite snapshot and encrypted attachment containers.

## Backup passphrase

Credential used only to derive the encryption key for an encrypted backup. It is distinct from the vault master-passphrase contract even if a user chooses the same text (reusing credentials is not recommended).

## Biometric convenience unlock

Optional supported-platform flow that gates use of an independently generated secondary secret stored through platform secure storage. It does not store or replace the master passphrase and is not recovery.

## CipherNest-managed plaintext staging

Temporary plaintext file/buffer created only when an explicit workflow needs plaintext, for example OS sharing of a CSV or decrypted attachment. CipherNest attempts bounded lifetime/cleanup but cannot remove copies retained by the OS, destination apps, backups, indexers, or storage remnants.

## Collection

Encrypted item organization string stored inside `VaultItem`. It is not a plaintext database folder/index.

## Current-master authorization

Successful verification of the current master passphrase for an operation that requires stronger authorization than simply having an unlocked session or recovery-key access.

## Data-encryption key / DEK

Random 256-bit vault key used to encrypt/authenticate records and attachments. Master, recovery, and optional secondary credentials wrap this key instead of directly encrypting every item with the passphrase.

## Encrypted envelope

Versioned AES-GCM nonce/ciphertext/tag structure represented by `EncryptedEnvelope` for record/key-wrapper internals.

## Funding CTA

Optional Buy Me a Coffee user-interface surface. It is enabled by default but can be removed from a distribution build using `CipherNestEnableFundingLink=false`; repository funding metadata remains independent.

## Header / Vault header

Bounded JSON metadata stored in `VaultHeader` that contains versioned wrapped-key/KDF information. Current header document version is 2; supported minimum is 1.

## Interactive unlock backoff

Client-side delay after repeated failed unlock attempts. It slows repeated interaction with the running app but does not prevent offline attacks on copied encrypted data.

## Key-encryption key / KEK

Key derived from a passphrase/recovery/backup credential using Argon2id and used to wrap/protect another key or backup payload. Owned temporary KEK buffers are zeroed where practical.

## Key lease / `VaultKeyLease`

Private 32-byte copy of the active vault DEK used by a key-sensitive operation. It links caller cancellation with the active session token and zeroes its copy on disposal.

## Local-first

Current product architecture where vault storage/processing occurs on the device and no CipherNest account/application server/cloud synchronization is required.

## Logical deletion

Application removal of records/files. It is not a guarantee of physical media sanitization because flash translation layers, snapshots, backups, filesystem history, and external copies can remain.

## Master passphrase

Primary user credential. It is never stored by CipherNest; it derives a KEK that authenticates/decrypts a wrapped random vault DEK.

## Migration history

SQLite `MigrationHistory` table recording applied schema migration versions. CipherNest validates sequence/version/required schema shape rather than treating one claimed version row as sufficient proof of database validity.

## Plaintext export

Explicit interoperability action that intentionally creates/writes readable data outside the normal encrypted-at-rest vault boundary. Current examples are CSV export and attachment export.

## Privacy-safe reporter

`IPrivacySafeExceptionReporter` implementation used for sanitized operation/type/HResult/severity diagnostics without directly recording exception messages/stacks or decrypted vault content.

## Recovery material / recovery key

Optional independent credential created during onboarding and shown to the user for separate storage. It can unwrap the vault DEK but is not accepted as current-master authorization for operations that explicitly require the master passphrase.

## Re-authentication

Additional current-master verification while the vault is otherwise unlocked, used for security-sensitive settings/destructive/plaintext-export/per-item workflows.

## Reparse point

Filesystem link/redirection concept. CipherNest storage/cache maintenance avoids recursively following reparse-point directories to reduce unexpected traversal/loop behavior.

## Restore commit point

The point in restore/database replacement after active state has begun mutation. Recovery after that point deliberately uses an uncancelled token so cancellation of the original request cannot cancel required rollback work.

## Secondary secret

High-entropy random value used for optional secondary wrapped-key convenience unlock. The MAUI application stores it using platform secure storage when biometrics are configured.

## Secure note

Vault item whose notes support a bounded safe Markdown-like subset. Raw HTML is neutralized; size limits are shared by parse/edit/save/import paths.

## Session token

Cancellation token associated with one unlocked vault security session. Lock/replacement cancels it so cancellable key-using work does not intentionally continue after the session ends.

## Source/UI regression test

A test in `CipherNest.UiTests` that inspects repository/UI/source structure without launching a device. It can prevent implementation-shape regressions but does not prove runtime behavior of platform APIs.

## Stored envelope

Opaque authenticated encrypted item record persisted in SQLite along with the canonical item GUID used as associated context.

## Transition gate

Serialized service gate shared by vault create/unlock/secondary unlock/lock/full-vault deletion so competing security-session transitions cannot publish state in an unsafe order.

## Trash retention

Configured period during which a logically trashed item can remain before routine maintenance permanently removes it. Manual permanent delete/empty-trash requires current-master re-authentication and confirmation.

## Vault item

Encrypted domain record represented by `VaultItem`, including title/type/credential/note/organization/custom-field/attachment/timestamp metadata.

## Wrapped key

Authenticated encrypted representation of the random vault DEK under a credential-derived KEK. Master, recovery, and optional secondary wrappers are independent paths to the same DEK.
