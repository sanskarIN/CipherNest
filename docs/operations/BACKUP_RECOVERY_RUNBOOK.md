# CipherNest Backup and Recovery Runbook

This runbook is for maintainers/testers and careful users validating backup/recovery behavior with disposable data. It is not a substitute for keeping multiple protected backups.

## 1. Recovery assumptions

Current CipherNest is local-first:

- there is no CipherNest account/server recovery service;
- the master passphrase is not stored remotely;
- optional recovery material is generated locally and must be stored separately;
- encrypted backup uses its own backup passphrase;
- losing the master passphrase/recovery material affects vault unlock;
- losing the backup passphrase affects that `.cnbak` file;
- losing/deleting every backup file cannot be repaired by cryptography.

## 2. Preferred recovery asset

Use authenticated encrypted `.cnbak` backups for CipherNest-fidelity recovery.

Do not treat plaintext CSV as a complete backup because it omits attachments/custom fields and exposes readable secrets.

## 3. Before creating an important backup

Confirm:

- current vault unlock works with the expected master passphrase;
- recovery material, if configured, is stored separately;
- no current unsaved item/attachment edit is pending;
- destination storage has sufficient free space;
- backup passphrase is unique/strong and can be stored separately;
- the destination is not the active vault database/attachment directory;
- the device/storage is stable enough for file creation/share.

## 4. Create encrypted backup through the App

1. Open Settings.
2. Enter a backup passphrase of 12–4,096 characters.
3. Choose encrypted backup/export.
4. Read the confirmation that CipherNest will lock before the consistent snapshot.
5. Confirm.
6. CipherNest locks the current vault.
7. A consistent SQLite snapshot plus encrypted attachments is archived and outer-encrypted.
8. The resulting `.cnbak` file is placed in the app's backup area and can be shared/exported through the OS.
9. Keep the vault locked until you deliberately unlock again.

## 5. Immediately after backup creation

Record, without storing the passphrase beside the file if avoidable:

```text
creation date/time
CipherNest version/build
source platform
file name/location
file size
optional SHA-256 checksum
where the backup passphrase/recovery instructions are stored
```

Do not put the backup passphrase in the filename/notes stored next to the backup.

## 6. Copy strategy

For important data, keep more than one encrypted backup on independent storage locations appropriate to your risk model.

Examples can include protected offline/external storage or a cloud/file service you deliberately trust to hold the already-encrypted `.cnbak` file.

CipherNest does not control external provider retention/availability.

## 7. Test restore periodically

A backup that has never been restored is unproven.

Preferred test method:

1. Use a disposable CipherNest installation/device/profile or disposable test data.
2. Ensure you will not overwrite the only important live vault.
3. Copy the `.cnbak` into the test environment.
4. Enter the backup passphrase.
5. Select the backup and confirm restore.
6. Unlock the restored vault with its vault master passphrase/recovery material.
7. Verify representative items, tags, collections, protected items, trash state, attachments, notes, and review/recent metadata.
8. Reconfigure biometrics intentionally if needed; restore should disable the local pairing.
9. Record the result/date/version.

## 8. Restore preflight

Before restoring over an important current vault:

- create/verify a separate current backup if possible;
- confirm selected file is the intended `.cnbak`;
- confirm you know its backup passphrase;
- understand that successful restore replaces the current vault database/attachment set;
- ensure storage has enough free space for temporary staging plus recovery copies;
- avoid power loss/forced termination during replacement where practical.

## 9. Restore flow and validation

The implementation is designed to:

1. copy/select the encrypted backup;
2. verify `CNBK0002` framing;
3. validate the 16..16,384-byte header boundary, strict version-2 JSON schema/depth, and bounded KDF/chunk metadata before Argon2;
4. authenticate/decrypt encrypted payload chunks;
5. reject trailing unauthenticated data;
6. enforce ZIP entry count/aggregate/path/duplicate limits;
7. enforce `.cna` entry size/name rules;
8. require a staged `vault.db`;
9. validate SQLite signature;
10. validate the replacement DB through store integrity/schema/resource checks, including strict supported v1/v2 vault-header JSON, before active mutation;
11. create a rollback snapshot;
12. replace the active DB/attachments;
13. attempt uncancelled rollback if failure occurs after active mutation;
14. clean temporary working files best-effort;
15. clear local biometric pairing after successful App-level restore.

## 10. Wrong backup passphrase

Expected behavior:

- outer AES-GCM authentication fails;
- restore is rejected as damaged/incorrect-passphrase backup;
- active vault should not be intentionally replaced;
- do not retry many guesses if you genuinely do not know the passphrase; offline guessing risk remains tied to passphrase strength.

Do not edit the `.cnbak` header trying to “reset” the passphrase. The backup key is derived from the passphrase and the authenticated framing depends on the original header.

## 11. Corrupted/truncated backup

Expected behavior:

- invalid magic/header/chunk framing, authentication failure, unexpected EOF, invalid archive structure, or staged DB validation rejects restore;
- the active vault should remain untouched when failure occurs before replacement;
- if failure occurs after active mutation begins, rollback is attempted.

Keep the original damaged file for forensic/debugging only if doing so is safe; do not send it with credentials to public issue trackers.

## 12. Failure during active replacement

The replacement path uses recovery state:

- active DB/WAL/SHM can be staged to unique `.previous.<guid>` recovery names by the store;
- attachment directory can move to `attachments.previous.<guid>`;
- rollback database snapshot exists in the restore working set;
- rollback uses an uncancelled token after destructive mutation begins.

If an application crash/OS storage failure prevents successful automatic recovery, **do not repeatedly delete recovery-looking files manually** before investigation/backup. Preserve the app-data directory if possible and seek maintainer guidance using synthetic/path metadata only—never disclose passphrases or real vault plaintext.

## 13. Recovery after an interrupted restore

Recommended maintainer/tester process:

1. Stop further writes to the affected disposable/test installation if possible.
2. Make a byte-for-byte copy of the entire CipherNest app-data directory for analysis where policy permits.
3. Do not modify the original recovery copies before understanding which DB/WAL/SHM/attachment components exist.
4. Record filenames/sizes/timestamps without publishing user-identifying paths/content.
5. Reproduce the condition with synthetic data.
6. Use the store replacement/recovery tests/source to determine intended component restoration.
7. Restore from a previously verified `.cnbak` if the user has one rather than attempting unsupported manual cryptographic repair.

For real user support, avoid asking the user to upload the vault/recovery files publicly.

## 14. Database-only manual copying is not a supported complete backup

The active SQLite file can have WAL/SHM state, and attachments live separately. Copying only `ciphernest.db` from a live app can therefore be inconsistent/incomplete.

Use CipherNest's consistent encrypted backup path rather than manual file copying.

## 15. Attachment completeness check after restore

Using disposable data, verify:

- every item attachment reference corresponds to a readable/decryptable `.cna` container;
- expected plaintext length matches after authenticated decryption;
- no invalid duplicate attachment IDs/storage names exist;
- binary/text files round-trip correctly;
- tampered/truncated `.cna` files fail closed.

Do not compare by exposing sensitive plaintext in logs.

## 16. Biometric state after restore

Expected App behavior:

- local secure-storage secondary secret cleared;
- `BiometricUnlockEnabled=false` persisted;
- in-memory master-authentication state cleared;
- restored vault unlock requires master/recovery;
- biometrics can be re-enabled only after deliberate current-master/OS-biometric setup.

A failure to clear local biometric pairing is security-sensitive and release-blocking until understood.

## 17. Cross-platform restore matrix

Because database/backup formats are intended to be platform-independent at the application format level, release candidates should test expected combinations such as:

```text
Android -> Android
Android -> Windows
Windows -> Android
Windows -> Windows
Apple -> Apple
Apple -> Windows/Android where supported by the candidate toolchains
```

Only claim combinations actually tested successfully on the exact candidate.

## 18. Backup resource ceilings

Current backup limits include:

```text
Header JSON:          16..16,384 bytes framing
Header JSON depth:    max 16; exact version-2 root/KDF property sets
Salt:                 16..64 bytes
Chunk size accepted:  64 KiB..4 MiB
Current chunk size:   1 MiB
Chunk count:          max 65,536
Archive entries:      max 10,001
Archive bytes:        max 1 GiB
Restored vault header: max 64 KiB UTF-8; depth 16; exact supported v1/v2 root/wrapper/KDF schemas
```

A backup intentionally outside these limits is not supported by the current release.

## 19. Backup retention policy

CipherNest does not currently implement a server-side backup lifecycle/rotation policy for the user.

Users/maintainers must decide how many encrypted backups to retain and where. Consider:

- recovery point objectives;
- storage capacity;
- risk of old credentials/data remaining in backups;
- provider retention/versioning;
- secure destruction limitations.

Deleting an old encrypted backup is logical deletion and may not physically sanitize external media/provider copies.

## 20. Password/passphrase change relationship

Changing the vault master passphrase does not automatically re-encrypt old `.cnbak` files under the new master passphrase.

A `.cnbak` is protected by its **backup passphrase** and contains the snapshot state at its creation time, including whichever vault header/wrappers existed then.

After security-sensitive changes, create a fresh backup and decide deliberately how to retain/remove older backups.

## 21. Verification evidence template

For a tested backup/restore record:

```text
Candidate SHA:
CipherNest version/build:
Source platform/OS:
Restore platform/OS:
Backup size:
Attachment count/representative sizes:
Backup creation result:
Wrong-passphrase rejection result:
Corruption rejection result:
Restore result:
Biometric invalidation result:
Representative item/attachment comparison result:
CI/integration-test references:
Known limitation/exception:
```

Never include real secret values.

## 22. When recovery is impossible

CipherNest cannot recover data when all usable key paths are lost, for example:

- master passphrase forgotten;
- no usable recovery material;
- no unlocked session remaining;
- backups require unknown backup passphrases and contain vault state whose master/recovery credentials are also unavailable.

Do not promise a hidden master key/backdoor/server reset. The current design intentionally has none.

## 23. Reporting a backup/restore bug

Provide:

- CipherNest version/commit if known;
- platform/OS;
- whether failure occurred before or after confirmation/replacement;
- fixed error wording shown;
- approximate non-sensitive file sizes/counts;
- whether active vault still unlocks;
- whether a verified separate backup exists;
- synthetic reproduction if possible.

Do **not** provide:

- master/backup passphrase;
- recovery material;
- real vault contents;
- decrypted attachments;
- a real backup plus its credential.
