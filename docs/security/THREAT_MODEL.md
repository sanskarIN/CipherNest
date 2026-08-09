# CipherNest Threat Model

## Assets

Vault data, the random vault data-encryption key (DEK), master passphrase during entry/derivation, recovery material, optional biometric secondary secret, encrypted backups, encrypted attachments, imported plaintext before encryption, in-memory text previews, and decrypted values temporarily displayed, exported, or copied by explicit user action.

## Protects against

- **Copied database / locked lost device:** encrypted records do not reveal plaintext without the DEK; the DEK is wrapped by passphrase/recovery/optional-secondary mechanisms rather than stored directly.
- **Tampered records/backups:** AES-GCM authentication causes altered envelopes to be rejected.
- **Tampered attachment chunks:** each chunk is authenticated with item ID, attachment ID, and chunk index in associated data; truncation, trailing data, or length mismatches are rejected.
- **Offline brute force:** Argon2id increases attacker cost; protection still depends heavily on master/backup passphrase strength and recorded KDF parameters.
- **Accidental secret logging:** centralized application exception reporting records redacted event metadata and intentionally omits exception messages/stacks and decrypted payloads.
- **Accidental always-visible secrets:** UI masks secrets by default and requires explicit reveal/copy.
- **Stale biometric metadata after restore:** the app clears its local biometric secure-storage secret and disables biometric preference after a restore so an older backup wrapper is not silently trusted on the current installation.
- **Unsafe HTML note rendering:** the secure-note preview supports a deliberately small Markdown-like subset and neutralizes angle brackets rather than interpreting raw HTML.

## Partially mitigates

- **Unlocked lost device:** auto-lock/background lock reduces exposure but cannot undo secrets already displayed, previewed, copied, or exported.
- **Shoulder surfing / screen capture:** masking and supported platform screenshot controls help; cameras and some desktop capture paths remain outside app control.
- **Clipboard exposure:** timed clearing reduces duration; clipboard history, other apps, keyboard software, and OS sync may retain copies.
- **Malicious local apps:** OS sandboxing helps; accessibility services, input methods, clipboard access, screen readers with compromised implementations, and compromised user sessions may bypass assumptions.
- **Weak master passphrase:** strength guidance, generated passphrases, and KDF cost help but cannot turn a chosen weak/passphrase-reused secret into a strong one.
- **Malicious import/backup:** strict CSV parsing, row/column/field bounds, temporary staging, format/version checks, and authenticated backup validation reduce risk; parser/runtime flaws remain possible.
- **Malicious text attachment:** preview is restricted to small UTF-8 text-family files, bounded in size/display length, strips unsafe control characters, and never renders HTML. Managed strings still remain a memory-exposure limitation.
- **Biometric convenience unlock:** supported platforms require an OS biometric prompt before the app retrieves the independent random secondary secret from secure storage. The current design does not claim hardware-backed cryptographic binding of every secret retrieval to a biometric operation.
- **Supply-chain compromise:** central package versions, CI, dependency review, CodeQL, third-party notices, and vulnerability review reduce risk but cannot eliminate it.

## Cannot protect against

- A rooted/jailbroken or otherwise compromised operating system with privileged malware.
- Kernel/hypervisor compromise, hardware keyloggers, hostile firmware, process injection, or an attacker controlling the user session.
- Secrets intentionally exported, photographed, pasted, previewed in front of an observer, or shared by the user.
- Copies retained by an operating-system share sheet, destination application, backup provider, antivirus/indexer, or filesystem snapshot after an explicitly requested plaintext export.
- Guaranteed managed-memory erasure: .NET strings and GC copies cannot be reliably wiped.
- Guaranteed physical erasure from flash storage after logical file deletion.
- Loss of both the master passphrase and all configured recovery material.

## Specific scenarios

### Locked vs unlocked theft
A locked vault exposes encrypted database/attachment material. An unlocked app may have decrypted objects and the DEK in process memory. Locking clears application references and zeroes owned byte buffers where practical, but memory-forensics resistance is not guaranteed.

### Brute force
The vault header stores Argon2id salts and versioned KDF parameters alongside wrapped-key ciphertext. An attacker with the database can perform offline guesses. Users should choose long unique passphrases. Interactive rate limiting does not prevent copied-database attacks.

### Biometric unlock
Biometrics are a secondary convenience mechanism, never a recovery mechanism. Enabling or disabling them requires master-passphrase confirmation. A random secondary secret wraps the same DEK independently and is stored through platform secure storage. A fresh process requires master-passphrase authentication before biometric unlock becomes available for later locks, and the app can require the master passphrase again after a configured interval. Compromised platform secure storage, privileged malware, biometric subsystem compromise, or process injection are outside the guarantees of the app. See `BIOMETRIC_UNLOCK.md`.

### Secure-note and text-attachment preview
Secure-note preview never executes HTML. Supported text attachments are decrypted to a bounded in-memory buffer rather than a preview file, decoded as strict UTF-8, sanitized, display-limited, and byte-buffer-zeroed afterward where practical. The displayed `string` may survive in managed memory until garbage collection; the app does not claim deterministic erasure.

### Plaintext attachment/CSV export
Vault data remains encrypted at rest inside CipherNest. Explicit plaintext export requires deliberate user actions and creates temporary app-cache material when a share target requires a file. CipherNest attempts or provides controls for cleanup, but cannot guarantee deletion from flash remnants, OS-managed copies, provider caches, backups, or the receiving application.

### Diagnostics
Unhandled exceptions and lifecycle failures are routed through a privacy-safe reporter that records only sanitized operation identifiers, exception type, HResult, severity, and fixed text. Exception messages and stacks are intentionally omitted because they may contain paths or sensitive context. A future crash/analytics provider requires a separate privacy/threat review before enablement.

### Backup tampering
The backup container is versioned and authenticated. Restore validates format, bounds, authentication, and a temporary copy before replacement. Any authentication failure aborts restore. Restoring also invalidates the local biometric secure-storage association so the restored vault must be deliberately re-enrolled for biometrics.

### Future synchronization
Cloud synchronization, accounts, conflict resolution, device enrollment, collaboration, sharing protocols, and server compromise are **out of scope** for this release. A future design requires a separate protocol threat model before code.
