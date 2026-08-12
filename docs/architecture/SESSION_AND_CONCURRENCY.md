# CipherNest Session and Concurrency Model

CipherNest is local-only, but its security-sensitive work is asynchronous: key derivation, database I/O, attachment streaming, backup/restore, platform lifecycle callbacks, and user actions can overlap. This document records the intended ordering/cancellation model used to prevent stale authorization, key-zeroing races, and lost updates.

## Core state

The vault service owns security-session state conceptually consisting of:

```text
shared unlocked DEK (32-byte array or null)
per-unlock session CancellationTokenSource
key/session synchronization object
serialized security transition gate
record/attachment mutation serialization where required
```

The shared DEK is not handed directly to long-running callers. Key-sensitive work obtains a private lease.

## `VaultKeyLease`

A key lease exists to separate two lifetimes:

1. the shared session key that Lock must be able to zero immediately; and
2. the operation-local key bytes an already-running authenticated operation is using.

A lease:

- owns a private 32-byte copy of the current DEK;
- links the caller cancellation token with the current unlock-session token;
- exposes the copied key only for the operation lifetime;
- zeroes the copied key on dispose;
- zeroes rejected key material on relevant constructor-failure paths.

This avoids an earlier class of race where Lock could zero the same array reference an operation was actively using, potentially causing an operation to continue with mutated/zero key bytes.

## Session cancellation token

Every unlocked session has its own cancellation source.

Key-using work links to that token. When the session ends, CipherNest cancels it so cancellable database/attachment operations are told that their authorization context no longer exists.

Caller cancellation and session cancellation are different concepts:

- caller cancellation means “this request is no longer wanted”;
- session cancellation means “the unlocked security context that authorized this work has ended.”

A key-using operation must honor either when safe to do so.

## Lock ordering

Conceptually Lock performs:

```text
acquire transition gate
  -> remove/zero shared session DEK under synchronization
  -> detach current session cancellation source
  -> cancel/dispose detached source best-effort
  -> publish locked state
release transition gate
```

Cancellation callback failures are contained after the key-state transition so they do not reverse or mask the already-completed security transition.

## Serialized security transitions

These transitions share the same gate:

- vault creation;
- master/recovery unlock;
- secondary unlock;
- public lock;
- full local-vault deletion.

The purpose is deterministic state publication, not performance serialization.

### Race prevented: late unlock after requested lock

Without a transition gate:

```text
T1: unlock starts expensive Argon2 work
T2: lock observes/clears current state
T1: Argon2 finishes and installs new DEK
```

The user could believe the requested lock won while a late unlock publishes an unlocked session afterward.

With serialized transitions, the state-changing portion follows gate order and cannot publish through that uncontrolled interleaving.

## Unlock derivation and publication

Expensive credential validation/unwrap must still be cancellation-aware. Before a derived/unwrapped key becomes authoritative, the service validates that the transition/caller context is still allowed and then replaces session state through the serialized transition path.

Replacing a session cancels/disposes the previous session and zeroes the prior shared DEK before the new session becomes authoritative.

## Full-vault deletion authorization

Full local-vault deletion has stronger requirements than ordinary unlocked work.

Current intent:

1. Verify current master passphrase.
2. Acquire a live key lease from the currently authorized unlocked session.
3. Wait for the security transition gate using the lease/session-linked token.
4. Re-check that live authorization has not been cancelled.
5. Clear the session key at the destructive commit point.
6. Proceed with managed vault deletion using an uncancelled token for the committed destructive transition.

### Why the live lease matters

If re-authentication succeeds and deletion then waits behind another transition, an intervening lock/unlock could create a different security session. Authorization from the old session must not remain valid merely because a password check happened earlier.

The live lease token binds the destructive action to the same session. Intervening session cancellation cancels stale deletion before the destructive transition.

## Destructive commit-point cancellation

Cancellation is useful before a destructive commit point. After the service clears the active security session and commits to full local-vault deletion, allowing the original caller token to cancel the database-delete phase can intentionally leave a partially committed security operation.

Therefore the committed deletion phase uses an uncancelled token. This is analogous to restore rollback: recovery/cleanup required to preserve invariants after mutation must not be cancelled by the request cancellation that caused the failure.

## Record mutation serialization

Read-modify-write operations over encrypted item records can lose updates if concurrent operations independently read the same old item, apply different changes, and then overwrite one another.

Vault record mutations are serialized where the implementation requires a consistent read-modify-write sequence. Examples include:

- recent-access updates;
- trash state changes;
- attachment metadata mutation;
- other item mutations that derive a new encrypted record from an existing one.

Do not remove mutation serialization merely because SQLite writes themselves are atomic; the application-level read-modify-write sequence can still race.

## Attachment mutation gate

Attachment add/remove/permanent-delete operations share a cancellable attachment mutation gate.

This gate is deliberately separate from the session transition gate.

Why:

- attachment operations can perform long streaming file I/O;
- Lock must not wait behind a long attachment upload/export just because both need serialization;
- session cancellation should be able to cancel the attachment work promptly;
- within attachment mutation itself, per-item/global attachment budgets and metadata/file updates must remain consistent.

## Lock versus attachment export

A dedicated integration test deliberately blocks plaintext attachment export after writing begins and then locks the vault. The expected result is cancellation, demonstrating that the operation's lease token is linked to the active security session.

This test covers service/infrastructure cancellation behavior. It does not prove how every platform share-sheet implementation handles an already-created external plaintext copy.

## Backup creation concurrency

The Settings backup workflow locks the vault before the database/attachment snapshot is taken. This prevents normal unlocked edits from racing with the consistent backup snapshot in the application workflow.

Infrastructure still validates destinations/resources independently; UI locking is not a substitute for persistence validation.

## Backup restore and rollback cancellation

Before active mutation, restore remains cancellable.

After active DB/attachment state begins replacement:

- caller cancellation can be the reason forward restore fails;
- rollback must use `CancellationToken.None`/an uncancelled recovery context;
- otherwise the same cancelled request token could immediately cancel the rollback intended to restore the active vault.

Secondary rollback errors are best-effort and must not intentionally replace the original restore failure.

## SQLite replacement recovery set

Database replacement treats:

```text
DB
DB-wal
DB-shm
```

as one logical active set.

Each existing component is staged to a unique recovery name. Rollback restores only components whose corresponding recovery file actually exists. This avoids deleting/replacing a sidecar that never successfully moved during partial staging.

## Database deletion attempts

Full database deletion attempts the complete CipherNest-managed SQLite/recovery set. The implementation/reporting must not declare successful logical cleanup merely because only one component was removed.

Failure behavior is documented as logical/application deletion rather than physical sanitization.

## Settings writes

Settings saves use a unique `CreateNew` sibling staging file followed by replacement. This avoids multiple failed saves colliding on one deterministic `.tmp` path.

Settings are non-secret, but persistence should still preserve cancellation semantics and avoid cleanup failure masking the primary write result.

## Plaintext export staging

CSV/attachment plaintext exports create unique cache staging paths. Cleanup runs in `finally`/best-effort paths and failure is privacy-safe reported.

A cleanup failure should not replace the primary export/share failure with an unrelated secondary exception, but the UI should still warn when CipherNest cannot confirm deletion of application-managed plaintext staging.

## Lifecycle callback concurrency

MAUI lifecycle handlers are platform `async void` callbacks. Primary errors are caught/reported, then a fail-closed fallback separately attempts:

- vault lock;
- clipboard cleanup.

Each fallback is independently contained/reported so a secondary cleanup exception does not escape the native callback and hide the primary lifecycle failure.

## Credential binding lifetime

The UI clears bound credential properties before longer service/file/share work wherever practical. Local method variables can still hold immutable managed strings until the runtime releases them.

This is lifetime reduction, not deterministic secure erasure.

## Invariants for new asynchronous features

Before adding asynchronous vault work, answer:

1. What security session authorizes it?
2. Does it need a `VaultKeyLease`?
3. What happens if Lock occurs midway?
4. What caller cancellation points are safe?
5. Is there a commit point after which rollback/cleanup must ignore caller cancellation?
6. Is the operation a read-modify-write requiring serialization?
7. Can its gate block Lock behind long I/O?
8. Can cleanup failure mask the original error?
9. Can stale authorization survive a new session?
10. What integration/concurrency test proves the intended ordering?

## Required regression coverage for changes

Session/concurrency changes should update or add coverage for:

- key lease copy/zero/cancellation invariants;
- lock-cancelled in-flight key work;
- delayed unlock versus lock ordering;
- stale full-vault-deletion authorization;
- cancellation after destructive commit point;
- attachment mutation serialization/global budgets;
- backup rollback cancellation;
- partial SQLite component recovery;
- cleanup-error preservation.

Source-structure tests are useful for critical ordering but should be complemented by runtime integration/stress tests when controllable scheduling can be implemented.
