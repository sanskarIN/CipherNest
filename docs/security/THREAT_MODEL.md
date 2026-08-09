# CipherNest Threat Model

## Assets

Vault data, the random vault data-encryption key (DEK), master passphrase during entry/derivation, recovery material, optional biometric secondary secret, encrypted backups, attachments, and decrypted values temporarily displayed, exported, or copied by explicit user action.

## Protects against

- **Copied database / locked lost device:** encrypted records do not reveal plaintext without the DEK; the DEK is wrapped by a passphrase-derived key.
- **Tampered records/backups:** AES-GCM authentication causes altered envelopes to be rejected.
- **Offline brute force:** Argon2id increases attacker cost; protection still depends heavily on passphrase strength and KDF parameters.
- **Accidental secret logging:** application logging is designed around event metadata, not decrypted payloads.
- **Accidental always-visible secrets:** UI masks secrets by default and requires explicit reveal/copy.
- **Stale biometric metadata after restore:** the app clears its local biometric secure-storage secret and disables biometric preference after a restore so an older backup wrapper is not silently trusted on the current installation.

## Partially mitigates

- **Unlocked lost device:** auto-lock/background lock reduces exposure but cannot undo secrets already displayed or copied.
- **Shoulder surfing / screen capture:** masking and supported platform screenshot controls help; cameras and some desktop capture paths remain outside app control.
- **Clipboard exposure:** timed clearing reduces duration; clipboard history, other apps, and OS sync may retain copies.
- **Malicious local apps:** OS sandboxing helps; accessibility services, keyboard software, clipboard access, and compromised user sessions may bypass assumptions.
- **Weak master passphrase:** strength guidance and KDF cost help but cannot make a weak passphrase strong.
- **Malicious import/backup:** strict parsing, bounded sizes, version checks, and authenticated backup validation reduce risk; parser flaws remain possible.
- **Biometric convenience unlock:** supported platforms require an OS biometric prompt before the app retrieves the independent random secondary secret from secure storage. The current design does not claim hardware-backed cryptographic binding of every secret retrieval to a biometric operation.
- **Supply chain compromise:** locked dependencies, review, CI, and vulnerability scanning reduce risk but cannot eliminate it.

## Cannot protect against

- A rooted/jailbroken or otherwise compromised operating system with privileged malware.
- Kernel/hypervisor compromise, hardware keyloggers, hostile firmware, or an attacker controlling the user session.
- Secrets intentionally exported, photographed, pasted, or shared by the user.
- Copies retained by an operating-system share sheet or destination application after an explicitly requested plaintext export.
- Guaranteed managed-memory erasure: .NET strings and GC copies cannot be reliably wiped.
- Loss of both the master passphrase and all configured recovery material.

## Specific scenarios

### Locked vs unlocked theft
A locked vault exposes encrypted database material. An unlocked app may have decrypted objects and the DEK in process memory. Locking clears application references and zeroes owned byte buffers where practical, but memory-forensics resistance is not guaranteed.

### Brute force
The vault header stores an Argon2id salt and versioned KDF parameters. An attacker with the database can perform offline guesses. Users should choose long unique passphrases. Rate limiting only protects interactive unlock, not copied-database attacks.

### Biometric unlock
Biometrics are a secondary convenience mechanism, never a recovery mechanism. Enabling or disabling them requires master-passphrase confirmation. A random secondary secret wraps the same DEK independently and is stored through platform secure storage. A fresh process requires master-passphrase authentication before biometric unlock becomes available for later locks, and the app can require the master passphrase again after a configured interval. Compromised platform secure storage, privileged malware, biometric subsystem compromise, or process injection are outside the guarantees of the app. See `BIOMETRIC_UNLOCK.md`.

### Plaintext attachment export
Attachments remain encrypted at rest inside CipherNest. Explicit export creates a temporary plaintext cache file to hand to the operating-system share mechanism, then attempts deletion after the share request returns. CipherNest cannot guarantee deletion from flash remnants, OS-managed copies, provider caches, backups, or the receiving application.

### Backup tampering
The backup container is versioned and authenticated. Restore validates format, bounds, authentication, and a temporary copy before replacement. Any authentication failure aborts restore.

### Future synchronization
Cloud synchronization, accounts, conflict resolution, device enrollment, sharing, and server compromise are **out of scope** for this release. A future design requires a separate protocol threat model before code.
