# Privacy

CipherNest's current release is local-only. It does not require an account, email address, phone number, CipherNest server, or cloud synchronization service.

## Data handled locally

CipherNest may store the following on the device:

- encrypted vault header/key wrappers;
- encrypted vault item payloads;
- encrypted attachment files;
- non-secret application preferences such as theme, lock timeout, language readiness, reminder intervals, generator defaults, and biometric-enabled state;
- encrypted backups created by the user;
- temporary cache files created only for explicit import/export/share/restore/diagnostic workflows.

The master passphrase is not stored. Optional biometric unlock stores a separately generated random secondary secret in supported operating-system secure storage rather than storing the master passphrase.

A detailed location/lifetime/cleanup reference is maintained in `docs/security/DATA_LIFECYCLE.md`.

## No intentional application telemetry

The current release does not intentionally send vault contents, device identifiers, analytics, advertising identifiers, or telemetry to a CipherNest service. No third-party crash-reporting service is enabled.

Central exception reporting is local/debug-oriented and intentionally omits exception messages, stack traces, decrypted vault fields, passphrases, recovery keys, cryptographic keys, clipboard values, and attachment content. See `docs/privacy/DIAGNOSTICS.md`.

## User-initiated data leaving CipherNest

Encrypted backup export, plaintext CSV export, attachment export, external links, and operating-system share actions occur only after explicit user action in the relevant workflow. Plaintext export paths display warnings because share targets, search indexing, antivirus software, filesystem snapshots, device backups, cloud providers, or receiving applications may retain copies outside CipherNest's control.

Encrypted backup is the recommended CipherNest-fidelity transfer/recovery path. Plaintext CSV is an interoperability feature and is not a complete encrypted backup.

CipherNest provides cache-cleanup controls but cannot guarantee deletion of copies created outside its app-owned storage or physical erasure from flash media.

## Clipboard

Copying a username/secret/custom secret field writes plaintext to the operating-system clipboard. CipherNest keeps only a SHA-256 fingerprint for delayed matching in its cleanup timer state rather than retaining the copied plaintext there, but operating-system clipboard history/sync, other apps, accessibility/input software, and third-party clipboard managers can retain or observe copies independently.

## In-memory plaintext

While the vault is unlocked/used, decrypted item objects, secure-note/attachment preview strings, and UI-bound secret values can exist in process memory. CipherNest zeroes owned byte/character arrays where practical, but .NET immutable strings, serializer-created objects, UI controls, runtime/JIT copies, and garbage-collected memory cannot be guaranteed to be erased deterministically.

See `docs/security/DATA_LIFECYCLE.md` and `docs/security/THREAT_MODEL.md`.

## Operating-system and third-party processing

The operating system, app store, secure-storage service, clipboard manager/history, backups configured by the device owner, sharing providers, accessibility/input software, security software, or file/cloud provider selected by the user may independently process data according to their own policies. CipherNest cannot control a compromised, rooted, jailbroken, or otherwise privileged operating system.

## Backups and recovery files

Encrypted `.cnbak` backups contain authenticated encrypted CipherNest recovery data and should still be treated as sensitive assets. Temporary restore/recovery files can exist during a restore or SQLite replacement operation and are removed best-effort after success/failure where the OS permits it.

CipherNest has no server-held copy of the master passphrase or hidden recovery key. Losing all usable master/recovery paths can make a restored vault unrecoverable even when an encrypted backup file still exists.

See `docs/operations/BACKUP_RECOVERY_RUNBOOK.md`.

## Logical deletion

Trash cleanup, permanent item deletion, cache cleanup, and full local-vault deletion remove CipherNest-managed records/files where permitted. They do not guarantee physical sanitization from flash translation layers, filesystem snapshots, device/provider backups, or external/plaintext copies.

## Diagnostics and vulnerability reports

Diagnostic exports must remain redacted and must never contain decrypted vault records, master passphrases, recovery keys, biometric secondary secrets, clipboard values, or cryptographic keys. Security reports should follow `SECURITY.md` and must not include real user secrets.

Maintainer security-response handling is documented in `docs/operations/SECURITY_RESPONSE.md`.

## Documentation

The complete privacy/security documentation set is indexed at `docs/README.md`, including the threat model, cryptographic design, session security, sensitive-data lifecycle, diagnostics policy, and format documentation.
