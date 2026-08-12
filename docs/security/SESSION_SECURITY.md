# CipherNest Session Security

This document explains authorization roles and security-session behavior from a security perspective. Concurrency mechanics are in `../architecture/SESSION_AND_CONCURRENCY.md`; cryptographic details are in `CRYPTOGRAPHIC_DESIGN.md`.

## Credential roles are intentionally different

### Master passphrase

Primary credential used to authenticate/decrypt the master wrapped-DEK path. It is also the credential required for current-master re-authentication of sensitive actions.

### Recovery material

Optional independent credential that can unlock the same vault DEK through a separate wrapper. It is intended for recovery, not as a silent substitute for current-master authorization.

### Secondary secret

High-entropy random credential generated for optional convenience unlock. The MAUI app stores it in platform secure storage after master re-authentication and a successful biometric prompt on supported platforms.

### Backup passphrase

Separate credential used for encrypted `.cnbak` containers. It protects a backup format, not the active vault master wrapper.

## Unlocked is not the strongest authorization state

An unlocked vault allows normal item operations, but some actions intentionally require fresh current-master proof:

- enabling/disabling biometric secondary unlock;
- per-item protected-item re-authentication;
- plaintext CSV export;
- manual permanent deletion/empty Trash UI flows;
- master-passphrase change;
- full local-vault deletion;
- other security-sensitive Settings workflows that explicitly request current-master proof.

This prevents recovery/secondary convenience access from automatically becoming equivalent to every master-authorized action.

## Fresh-process biometric rule

A new process starts without an in-memory remembered master-authentication timestamp. Biometric convenience unlock is therefore not immediately treated as sufficient after a fresh app process until the master passphrase establishes the required session state.

This design reduces dependence on an old secure-storage value alone after process restart.

## Periodic master-passphrase rule

Settings normalize the periodic master requirement to 1–168 hours, default 24 hours.

When biometric convenience unlock is configured, the remembered master-auth timestamp determines whether convenience unlock may continue or the user must provide the master passphrase again.

This timestamp is in-memory/session security state, not a server token.

## Passphrase change ends the security session

Successful master-passphrase change:

1. rewrites the authenticated master wrapper for the same random vault DEK;
2. clears remembered master-authentication session state;
3. locks the current vault session;
4. attempts conditional clipboard cleanup;
5. requires the new master passphrase before biometric convenience unlock can resume.

The current design does not bulk re-encrypt every item solely because the master passphrase changes.

## Backup restore invalidates local biometric pairing

A restored vault header may contain secondary-wrapper metadata that was created on another installation/state while the current device's secure-storage secret belongs to a different state.

After successful restore the app:

- clears the local secure-storage secondary secret;
- clears remembered master-auth state;
- disables biometric unlock preference locally;
- requires deliberate reconfiguration after unlocking the restored vault.

## Lock state and key state

Locking is a key-state operation, not just a navigation change.

The service:

- removes/zeroes the shared vault DEK buffer under synchronization;
- cancels the current session cancellation source;
- causes session-linked key leases to observe cancellation;
- publishes locked state;
- App workflows may also request conditional clipboard cleanup.

Views that display decrypted state clear sensitive properties when leaving relevant pages. These controls reduce lifetime but cannot guarantee managed-memory erasure.

## In-flight work

Key-using operations obtain private `VaultKeyLease` copies. A lease prevents lock from mutating the exact same key array currently being used while still allowing lock to cancel the session-linked operation.

The copied lease key is zeroed on dispose.

A runtime integration test covers a deliberately blocked plaintext attachment export that is cancelled when the vault locks.

## Stale authorization protection for full deletion

Full-vault deletion is bound to the active session:

1. current-master re-authentication succeeds;
2. deletion acquires a live key lease;
3. deletion waits for the security transition gate using that live session token;
4. an intervening lock/unlock cancels the old lease/session;
5. stale authorization cannot intentionally delete a newly established session's vault state.

## Interactive failure delay

Repeated failed interactive unlock attempts use a bounded exponential delay capped at five minutes.

This is not cryptographic lockout of the stored database. An attacker who copies encrypted vault data can attempt offline guesses independently of the app's UI delay.

CipherNest documentation must not imply otherwise.

## Clipboard relationship

Explicit secret copy places plaintext into the OS clipboard. CipherNest retains a SHA-256 fingerprint for delayed comparison instead of keeping the copied plaintext in timer state.

Manual/background/timeout lock can request conditional cleanup; CipherNest clears only if the current clipboard still matches its prior copied value so newer unrelated clipboard content is preserved.

OS clipboard history/sync/third-party managers remain outside the session boundary.

## Background and inactivity rules

`SessionLockPolicy` determines whether configured background/inactivity conditions require lock. Clock rollback is handled fail-closed rather than allowing a negative elapsed interval to extend an unlocked session.

Platform lifecycle failures invoke separately contained best-effort lock/clipboard cleanup.

## Screenshot protection

Screenshot protection is a preference/platform capability, not a cryptographic session property. Where an implementation is unsupported or incomplete, CipherNest must state the limitation rather than implying every screen capture path is blocked.

## Protected items

`VaultItem.RequiresReauthentication` causes the Item Editor to withhold protected item content until current-master re-authentication succeeds. Recovery/secondary convenience unlock does not silently satisfy this extra current-master check.

The re-authentication passphrase field is cleared after the authentication decision where practical.

## Destructive UI confirmations

Authentication and intent confirmation are separate:

- current-master verification proves credential knowledge;
- destructive confirmation proves the user intended the current operation.

For full-vault deletion the UI additionally requires exact phrase `DELETE MY VAULT`. Plaintext export similarly uses `EXPORT PLAINTEXT` as a deliberate risk acknowledgement.

Confirmation phrases are not cryptographic credentials.

## Session security non-goals

The design cannot protect against:

- privileged/rooted/jailbroken OS compromise;
- process injection/debugging by a privileged attacker;
- hardware/OS keyloggers;
- malicious accessibility/input services with sufficient privileges;
- memory snapshots while plaintext is in process;
- secrets intentionally copied/exported/shared;
- deterministic erasure of immutable .NET strings;
- platform clipboard/share/screenshot behavior outside app control.

## Review checklist for authorization changes

Any new sensitive action should state:

- whether unlocked-session access is sufficient;
- whether current-master re-authentication is required;
- whether recovery/secondary access is intentionally accepted or rejected;
- whether authorization must remain live while waiting for a concurrency gate;
- whether a destructive commit point changes cancellation rules;
- what state is cleared after success/failure/cancel/navigation;
- how target-device behavior is verified.
