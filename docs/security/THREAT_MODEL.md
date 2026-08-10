# CipherNest Threat Model

## Assets

Vault data, the random vault data-encryption key (DEK), per-operation DEK lease copies while unlocked, master passphrase during entry/derivation, recovery material, optional biometric secondary secret, encrypted backups, encrypted attachments, imported plaintext before encryption, in-memory text previews, and decrypted values temporarily displayed, exported, or copied by explicit user action.

## Protects against

- **Copied database / locked lost device:** encrypted records do not reveal plaintext without the DEK; the DEK is wrapped by passphrase/recovery/optional-secondary mechanisms rather than stored directly.
- **Tampered records/backups:** AES-GCM authentication causes altered envelopes to be rejected. Backup header version/salt/KDF/chunk metadata is resource-validated before Argon2 work.
- **Tampered attachment chunks:** each chunk is authenticated with item ID, attachment ID, and chunk index in associated data; truncation, trailing data, or length mismatches are rejected.
- **Malformed attachment storage metadata:** opaque encrypted attachment filenames must be GUID-based `.cna` names without path separators before filesystem access.
- **Malformed decrypted record metadata:** decrypted item IDs must match their authenticated SQLite row ID and item metadata is validated before the object leaves the infrastructure boundary.
- **Resource-hostile local database metadata:** vault-header UTF-8 size, item count, per-record envelope size, aggregate encrypted-record bytes, serialized item JSON size, canonical row IDs, and aggregate item text are bounded. SQLite length/count checks happen before BLOB materialization where possible.
- **Backup export clobbering active vault files:** canonical destination validation blocks the live database, WAL/SHM/recovery files, and encrypted attachment directory; staging uses a collision-resistant sibling path.
- **Duplicate/pathological backup archive entries:** normalized duplicate ZIP paths are rejected and encrypted attachment entries must fit the implemented attachment-container size envelope in addition to global archive/path/count limits.
- **Offline brute force cost escalation:** Argon2id increases attacker cost; protection still depends heavily on master/backup passphrase strength and recorded KDF parameters. Untrusted KDF metadata is resource-bounded before Argon2 work is accepted.
- **Accidental secret/path logging:** centralized application exception reporting records redacted event metadata and intentionally omits exception messages/stacks and decrypted payloads. Sensitive file, backup, transfer, item-open, capability-probe, launcher, lifecycle, and cleanup failures use fixed user-facing messages plus the redacted reporter instead of rendering/logging raw exception text.
- **Accidental always-visible secrets:** UI masks secrets by default and requires explicit reveal/copy.
- **Stale biometric metadata after restore:** the app clears its local biometric secure-storage secret and disables biometric preference after a restore so an older backup wrapper is not silently trusted on the current installation.
- **Security-session continuation after master-passphrase rotation:** changing the master passphrase clears the remembered master-authentication timestamp, locks the vault, and requires the new master passphrase before biometric convenience unlock can resume.
- **Unsafe HTML note rendering:** the secure-note preview supports a deliberately small Markdown-like subset and neutralizes angle brackets rather than interpreting raw HTML. Stored/imported notes share the same 200,000-character and 5,000-line bounds as the renderer.
- **Structurally/resource-invalid restore database replacing the active vault:** replacement databases are opened read-only, checked with SQLite `quick_check`, required to match the exact supported schema version, checked for required tables/columns, and checked for bounded header/item metadata before active database mutation.

## Partially mitigates

- **Unlocked lost device:** auto-lock/background lock reduces exposure but cannot undo secrets already displayed, previewed, copied, or exported. Sensitive screen ViewModels clear their credential/decrypted fields when pages disappear and credential-bound UI properties are cleared before longer security/file/share operations where practical. Key-using work operates on zeroing private DEK lease copies linked to the current unlock session; locking cancels the session so cancellable in-flight I/O stops instead of intentionally continuing after lock. .NET managed-memory copies cannot be deterministically erased.
- **Shoulder surfing / screen capture:** masking and supported platform screenshot controls help; cameras and some desktop capture paths remain outside app control.
- **Clipboard exposure:** username/password/custom-secret copies require explicit actions. CipherNest keeps a SHA-256 fingerprint for delayed comparison instead of retaining the copied plaintext in the timer state, zeroes owned fingerprint buffers when practical, uses fixed-time fingerprint comparison, and preserves newer unrelated clipboard values. Manual/background/timeout locks request the same conditional cleanup. Clipboard history, other apps, keyboard software, OS sync, memory snapshots, and platform restrictions may still retain copies. The fingerprint reduces plaintext lifetime but is not a secret hash suitable for resisting offline guessing if process memory is compromised.
- **Interactive brute-force attempts:** a bounded exponential delay begins after repeated failed unlocks and caps at five minutes. This affects only the interactive client; it does not protect a copied database from offline guessing.
- **Malicious local apps:** OS sandboxing helps; accessibility services, input methods, clipboard access, screen readers with compromised implementations, and compromised user sessions may bypass assumptions.
- **Weak master passphrase:** strength guidance, generated passphrases, and KDF cost help but cannot turn a chosen weak/passphrase-reused secret into a strong one.
- **Malicious import/backup:** strict CSV parsing, row/column/field bounds, final-field column enforcement, temporary staging, format/version checks, authenticated backup validation, duplicate-entry/attachment-container bounds, and pre-replacement SQLite/schema/resource validation reduce risk; parser/runtime flaws remain possible.
- **Malicious text attachment:** preview is restricted to small UTF-8 text-family files, bounded in size/display length, strips unsafe control characters, and never renders HTML. Managed strings still remain a memory-exposure limitation.
- **Biometric convenience unlock:** supported platforms require an OS biometric prompt before the app retrieves the independent random secondary secret from secure storage. Android source uses the API-28 `BiometricPrompt` baseline without depending on a newer preflight manager; enrollment/hardware/lockout results are left to the prompt/fallback path. Apple request cancellation invalidates the native authentication context. The design does not claim hardware-backed cryptographic binding of every secret retrieval to a biometric operation.
- **Logical permanent deletion:** trash retention and explicit permanent-deletion actions remove CipherNest-managed encrypted records/attachment containers. Manual permanent deletion requires the current master passphrase and confirmation. Item deletion removes the database record before best-effort encrypted attachment cleanup so a database-delete failure cannot leave a surviving record that references files already intentionally removed. Flash translation layers, filesystem snapshots, backups, or forensic remnants can persist outside application control.
- **Supply-chain compromise:** central package versions, cross-platform compile gates, CodeQL application analysis, dependency review, third-party notices, and vulnerability review reduce risk but cannot eliminate it.

## Cannot protect against

- A rooted/jailbroken or otherwise compromised operating system with privileged malware.
- Kernel/hypervisor compromise, hardware keyloggers, hostile firmware, process injection, or an attacker controlling the user session.
- Secrets intentionally exported, photographed, pasted, previewed in front of an observer, or shared by the user.
- Copies retained by an operating-system share sheet, destination application, backup provider, antivirus/indexer, clipboard history service, or filesystem snapshot after an explicitly requested plaintext action.
- Guaranteed managed-memory erasure: .NET strings and GC copies cannot be reliably wiped.
- Guaranteed physical erasure from flash storage after logical file deletion.
- Loss of both the master passphrase and all configured recovery material.

## Specific scenarios

### Locked vs unlocked theft
A locked vault exposes encrypted database/attachment material. An unlocked app may have decrypted objects and the session DEK in process memory. Key-using operations copy the DEK into short-lived `VaultKeyLease` buffers that are zeroed on disposal and link caller cancellation with a per-unlock session token. Locking first removes/zeroes the shared session key under synchronization and cancels the session token; cancellable database/attachment operations therefore receive cancellation while their private lease remains independent from the already-zeroed shared array. Sensitive pages also clear ViewModel fields when leaving the screen, and credential fields are cleared earlier before several authenticated operations. These controls reduce lifetime/exposure but do not constitute memory-forensics resistance.

### Brute force
The vault header stores Argon2id salts and versioned KDF parameters alongside wrapped-key ciphertext. An attacker with the database can perform offline guesses. Users should choose long unique passphrases. Interactive failed-attempt backoff only affects the running client and must never be described as protection against offline attack.

### Vault/header compatibility and storage bounds
CipherNest accepts only explicitly supported vault-header versions. A future/unknown version is rejected before key unwrap instead of being interpreted as if it were a current structure. Header JSON is additionally bounded to 64 KiB UTF-8 before deserialization at the SQLite/service boundaries. Database migrations reject a newer schema version, and post-migration shape checks ensure a forged migration-history row cannot substitute for required tables/columns.

Stored encrypted records are bounded to 100,000 rows, 24 MiB per envelope, and 256 MiB aggregate encrypted envelope bytes. Decrypted/serialized item JSON is bounded to 16 MiB, and combined item text is bounded to 2,000,000 characters. These limits reduce memory/resource abuse but do not make arbitrarily large local datasets safe.

### Master-passphrase change
Changing the master passphrase first validates the requested new passphrase and uses the current master to rewrite the authenticated master wrapper for the same random DEK. Bound current/new passphrase fields are cleared before the rotation service call. After a successful change, the application clears its remembered master-authentication session, locks the vault, attempts conditional clipboard clearing, and routes to the unlock screen. Existing independent recovery/secondary wrappers are not silently treated as a substitute for the fresh-master requirement.

### Biometric unlock
Biometrics are a secondary convenience mechanism, never a recovery mechanism. Enabling or disabling them requires master-passphrase confirmation. A random secondary secret wraps the same DEK independently and is stored through platform secure storage. A fresh process requires master-passphrase authentication before biometric unlock becomes available for later locks, and the app can require the master passphrase again after a configured interval. Compromised platform secure storage, privileged malware, biometric subsystem compromise, or process injection are outside the guarantees of the app. See `BIOMETRIC_UNLOCK.md`.

### Clipboard lifecycle
CipherNest writes username/password/custom-secret values only after an explicit copy action. After a successful copy it tracks only a fixed-size SHA-256 fingerprint for the scheduled comparison, not the copied plaintext string. The current clipboard is hashed and compared in fixed time; clearing happens only if it still matches. Caller cancellation after the successful copy does not silently cancel the independent security timer. Security-triggered vault locks use the same conditional cleanup, so unrelated clipboard content copied later is preserved. The tracked hash is not intended as password storage and does not make compromised process memory safe. OS clipboard history/sync and third-party clipboard managers are outside CipherNest's deletion guarantees.

### Lifecycle failure
Background/resume handlers are `async void` platform callbacks, so their fallback path must not leak a second cleanup exception. CipherNest routes primary lifecycle failures through the privacy-safe reporter and separately contains/reports fallback vault-lock and clipboard-cleanup failures. This is fail-closed best effort; a compromised or failing runtime/OS remains outside the guarantee.

### Trash and deletion
Moving an item to Trash is reversible until retention expiry or permanent deletion. Configured retention cleanup runs during normal vault maintenance. Manual permanent delete/empty-trash operations require current-master re-authentication and a separate destructive confirmation. The bound destructive passphrase is cleared immediately after the re-authentication decision and before the destructive confirmation dialog. Permanent item deletion removes the database record first, then performs best-effort encrypted attachment cleanup. Logical deletion removes CipherNest-managed encrypted data but cannot promise physical media sanitization.

### Secure-note and text-attachment preview
Secure-note preview never executes HTML. Secure-note storage, import, preview, checklist append, and checklist toggle share the 200,000-character/5,000-line policy so one path cannot persist an input that only another path rejects on size. Supported text attachments are decrypted to a bounded in-memory buffer rather than a preview file, decoded as strict UTF-8, sanitized, display-limited, and byte-buffer-zeroed afterward where practical. Attachment encryption also zeroes its reusable plaintext chunk buffer after encryption and on exit. The displayed `string` may survive in managed memory until garbage collection; the app does not claim deterministic erasure.

### Plaintext attachment/CSV export
Vault data remains encrypted at rest inside CipherNest. Explicit plaintext export requires deliberate user actions and creates temporary app-cache material when a share target requires a file. The master-passphrase UI field is cleared immediately after plaintext-export authentication. Attachment-export staging filenames include a random component so an unresolved previous staging file does not cause reuse/overwrite. Opaque encrypted attachment storage metadata is separately constrained to a GUID `.cna` filename before app-data filesystem access. Locking cancels an in-flight plaintext attachment export through the per-session key lease token where the destination honors cancellation. CipherNest attempts cleanup and reports failure without displaying the temporary path, but cannot guarantee deletion from flash remnants, OS-managed copies, provider caches, backups, or the receiving application.

### Diagnostics
Unhandled exceptions, lifecycle failures, settings/backup/transfer/attachment file failures, unlock capability probes, external-link failures, and security cleanup failures are routed through a privacy-safe reporter that records only sanitized operation identifiers, exception type, HResult, severity, and fixed text. Exception messages and stacks are intentionally omitted because they may contain paths or sensitive context. A future crash/analytics provider requires a separate privacy/threat review before enablement.

### Backup tampering and restore replacement
The backup container is versioned and authenticated. Export canonicalizes the destination and refuses the active DB/WAL/SHM/recovery/attachment-store paths; temporary encrypted output uses a collision-resistant sibling `CreateNew` file.

Restore validates header version/salt/KDF/chunk bounds before Argon2, authenticates every encrypted chunk, bounds extracted paths/counts/sizes, rejects duplicate normalized paths, and constrains attachment entries to the implemented encrypted-container envelope. It then relies on the store replacement boundary to run SQLite `quick_check`, exact schema-version validation, required table/column checks, vault-header presence/size checks, item count/aggregate/per-record bounds, and canonical item-ID checks before touching the active database.

The active DB/WAL/SHM set is staged into unique recovery names. Component-aware rollback restores only components that actually moved. Encrypted-backup rollback uses an uncancelled recovery token once active-state mutation has begun so caller cancellation cannot cancel the recovery DB replacement. Secondary rollback failure remains best-effort and does not intentionally replace the original restore exception. The backup-passphrase bound field is cleared before file-picker/staging work; temporary staging cleanup failures are redacted. Restoring also invalidates the local biometric secure-storage association so the restored vault must be deliberately re-enrolled for biometrics.

### Settings and local cache
Settings JSON is not a secret store, but malformed/out-of-range values could weaken runtime expectations if trusted blindly. CipherNest normalizes supported enum/numeric bounds on load and save, restores a valid password-generator character selection when needed, and falls back to secure defaults on malformed/unreadable preference files. Saves use collision-resistant sibling staging and best-effort cleanup while preserving cancellation semantics. Cache/storage traversal materializes directory enumerations inside guarded blocks and skips reparse-point directories so lazy enumeration failures or link recursion do not bypass the intended fail-soft behavior.

### CI and source gates
The repository configures core tests/formatting, Windows/Android/iOS/Mac Catalyst compile gates, a funding-disabled Windows build, CodeQL application analysis, and dependency review. These controls reduce regressions and platform-binding drift but do not prove device behavior, signing/store compliance, or security. A configured workflow is not a passing workflow until the exact candidate executes successfully.

### Future synchronization
Cloud synchronization, accounts, conflict resolution, device enrollment, collaboration, sharing protocols, and server compromise are **out of scope** for this release. A future design requires a separate protocol threat model before code.
