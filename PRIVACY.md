# Privacy

CipherNest's current release is local-only. It does not require an account, email address, phone number, CipherNest server, or cloud synchronization service.

## Data handled locally

CipherNest may store the following on the device:

- encrypted vault header/key wrappers;
- encrypted vault item payloads;
- encrypted attachment files;
- non-secret application preferences such as theme, lock timeout, language readiness, reminder intervals, generator defaults, and biometric-enabled state;
- encrypted backups created by the user;
- temporary cache files created only for explicit import/export/share workflows.

The master passphrase is not stored. Optional biometric unlock stores a separately generated random secondary secret in supported operating-system secure storage rather than storing the master passphrase.

## No intentional application telemetry

The current release does not intentionally send vault contents, device identifiers, analytics, advertising identifiers, or telemetry to a CipherNest service. No third-party crash-reporting service is enabled.

Central exception reporting is local/debug-oriented and intentionally omits exception messages, stack traces, decrypted vault fields, passphrases, recovery keys, cryptographic keys, clipboard values, and attachment content. See `docs/privacy/DIAGNOSTICS.md`.

## User-initiated data leaving CipherNest

Encrypted backup export, plaintext CSV export, attachment export, and operating-system share actions occur only after explicit user action. Plaintext export paths display warnings because share targets, search indexing, antivirus software, filesystem snapshots, device backups, cloud providers, or receiving applications may retain copies outside CipherNest's control.

CipherNest provides cache-cleanup controls but cannot guarantee deletion of copies created outside its app-owned storage or physical erasure from flash media.

## Operating-system and third-party processing

The operating system, app store, secure-storage service, clipboard manager/history, backups configured by the device owner, sharing providers, accessibility/input software, or security software may independently process data according to their own policies. CipherNest cannot control a compromised, rooted, jailbroken, or otherwise privileged operating system.

## Diagnostics and vulnerability reports

Diagnostic exports must remain redacted and must never contain decrypted vault records, master passphrases, recovery keys, biometric secondary secrets, clipboard values, or cryptographic keys. Security reports should follow `SECURITY.md` and must not include real user secrets.
