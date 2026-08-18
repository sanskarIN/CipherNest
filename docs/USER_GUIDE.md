# CipherNest User Guide

CipherNest is a local-first password, secure-note, credential, identity, reference, and encrypted-document vault. The current release does not require a CipherNest account or application cloud service. Your vault is stored locally on the device, and CipherNest cannot remotely reset a forgotten master passphrase.

> Security note: CipherNest has not completed an independent professional security audit. Use the project according to your own risk requirements and keep separate backups of important data.

## 1. First launch

On a new installation CipherNest routes through Startup and then to Onboarding when no local vault exists.

### Create the master passphrase

- Use a unique passphrase that is not reused for another account or service.
- The application requires at least 12 characters for creation and enforces the current cryptographic character ceiling of 4,096 characters.
- The onboarding strength check must consider the chosen passphrase sufficiently strong before vault creation is enabled.
- CipherNest does not store the master passphrase.
- A forgotten master passphrase cannot be reset by a CipherNest server because the current release has no CipherNest account/server recovery service.

### Recovery material

If recovery is enabled during setup, CipherNest returns independent recovery material once after vault creation. That recovery value wraps the same random vault data-encryption key through a separate authenticated path.

- Save the recovery value before leaving the recovery screen.
- Store it separately from the device and separately from the master passphrase.
- Do not put a real recovery value in screenshots, support requests, issue reports, chat messages, or source control.
- Recovery can unlock the vault, but it is not treated as a replacement for current-master authorization when CipherNest specifically requires the current master passphrase for a sensitive action.
- Losing both the master passphrase and all usable recovery material means the local-only vault is unrecoverable through CipherNest.

## 2. Unlocking and locking

### Master/recovery unlock

The Unlock screen accepts the master passphrase or configured recovery material. Failed interactive attempts use a bounded client-side delay after repeated failures. This delay helps slow repeated attempts against the running application; it does not stop offline guessing by an attacker who copied encrypted vault data.

Invalid/malformed credentials are handled through the normal authentication-failure path rather than being displayed as raw cryptographic exceptions.

### Optional biometric convenience unlock

On supported Android, iOS, and Mac Catalyst devices, CipherNest can configure biometric convenience unlock after current-master re-authentication and a successful OS biometric prompt.

- Biometrics do not store or replace the master passphrase.
- CipherNest generates a separate random secondary secret and stores it through platform secure storage.
- Windows currently uses master-passphrase fallback and does not advertise Windows Hello convenience unlock.
- A fresh process requires a master-passphrase-authenticated session before biometric convenience can be used later.
- CipherNest can require the master passphrase again after the configured interval.
- Restoring a backup disables the local biometric pairing until deliberately configured again.

See `security/BIOMETRIC_UNLOCK.md` for the exact design and limitations.

### Automatic lock

Settings control:

- inactivity lock timeout;
- lock when the app goes to the background;
- clipboard clearing interval;
- screenshot-protection preference where a platform implementation supports it.

The default lock timeout is 60 seconds and background locking defaults to enabled. Lifecycle failures take a fail-closed best-effort path: CipherNest attempts to lock and clear its matching clipboard secret while routing diagnostic details through the privacy-safe reporter.

### Manual lock

Use the vault's Lock action whenever leaving the device or ending a sensitive session. Locking removes/zeroes the shared session key buffer and cancels session-linked key leases used by cancellable vault operations.

## 3. Vault items

The current domain supports these item types:

- Login
- Secure Note
- Identity
- Payment Card Reference
- Wi-Fi Credential
- Software License
- Server/SSH Reference
- Document
- Custom
- Time-Based One-Time Password (TOTP)

Every item has a required title and can also contain a username/identifier, secret, URL, notes, collection, tags, favorite state, custom fields, attachment references, timestamps, optional review date, trash state, and optional per-item re-authentication requirement.

### Create an item

1. Open Vault.
2. Choose the add/new-item action.
3. Select the item type.
4. Enter a title and any relevant optional fields.
5. Add comma-separated tags and/or a collection when useful.
6. Mark Favorite if required.
7. Optionally enable per-item re-authentication for content that should require an extra master-passphrase check after opening.
8. Save.

The application normalizes title/username/URL/collection and tags before storage. Tags are trimmed, empty tags are removed, duplicate tags are collapsed case-insensitively, and tags are sorted.

### TOTP items

Choose **Time-Based One-Time Password (TOTP)** when you are authorized to store an authentication seed in CipherNest. In this item type, the normal Secret field is the Base32 TOTP seed. Select the provider's algorithm (SHA-1/SHA-256/SHA-512), digit count (6 or 8), and period (15–120 seconds; commonly 30).

- CipherNest stores the seed and TOTP settings inside the authenticated encrypted vault item.
- Choose **Refresh code** to calculate the current code locally. There is no background refresh timer in this release.
- Generated codes are not saved into the vault record.
- **Copy code** refreshes immediately and then uses the same timed best-effort clipboard cleanup policy as other secrets.
- A copied seed has longer-lived risk than a copied one-time code because the seed can generate future codes.
- Bounded local `otpauth://totp/...` setup-URI import and canonical setup-URI copy are implemented.
- HOTP/counter setup URIs are intentionally rejected.
- QR scanning/rendering and provider/autofill enrollment are not implemented by the current source.
- If password and TOTP seed for the same service are kept in one vault, compromise of the unlocked vault can expose both factors.

#### Import a TOTP setup URI

Use this only with a setup URI you are authorized to import.

1. Open or create a TOTP item.
2. Paste the `otpauth://totp/...` value into the masked **Import a TOTP setup URI** field.
3. Choose **Import URI**.
4. CipherNest parses the URI locally; it does not contact the provider.
5. Review the imported account name, issuer/title, algorithm, digits, period, and seed context before saving.
6. Save the item only after confirming the imported metadata matches the intended account.

The dedicated import field is cleared after the import attempt and is also cleared when the Item Editor clears sensitive page state. The URI contains the long-lived seed, so do not paste it into support chats, logs, screenshots, issue reports, or untrusted applications.

The current parser deliberately applies defensive ceilings and rejects unsupported/ambiguous input such as HOTP, `counter`, duplicate query keys, unsupported algorithms/digits/periods, inconsistent issuer metadata, invalid percent encoding, control/format metadata characters, or a malformed Base32 seed.

#### Copy a TOTP setup URI

1. Open a TOTP item after any required re-authentication.
2. Confirm the account/issuer/seed/settings are correct.
3. Choose **Copy setup URI**.
4. CipherNest formats a canonical `otpauth://totp/...` value locally and copies it through the same timed secret-clipboard service used for other sensitive copy actions.
5. Paste only into a trusted authenticator or destination you intentionally selected.

A copied setup URI is more sensitive than a single generated code because it normally contains the long-lived seed. Clipboard cleanup is best effort: operating-system history, synchronization, keyboards, accessibility services, or other applications can retain/read the value outside CipherNest's control.

See `security/TOTP.md` for the precise security model, URI limits, and RFC compatibility details.

### Important item limits

- Title: required, maximum 256 characters.
- Username/identifier: maximum 2,048 characters.
- Secret: maximum 100,000 characters.
- URL: maximum 4,096 characters.
- Collection: maximum 128 characters.
- Tags: maximum 100; each non-empty tag maximum 128 characters.
- Custom fields: maximum 100; field names maximum 128 characters and values maximum 100,000 characters.
- Attachments: maximum 25 per item.
- Combined item text/metadata budget: 2,000,000 characters.
- Secure-note text: maximum 200,000 characters and 5,000 lines.
- TOTP setup URI: maximum 8,192 characters with at most 16 query pairs.

These are safety ceilings, not recommended everyday item sizes.

## 4. Custom fields

The Item Editor accepts custom fields using `name=value` lines. Prefix a field with `[secret]` when its value should be treated as a secret custom field in quick-copy UI.

Example using synthetic values:

```text
account-id=demo-123
[secret]api-token=synthetic-example-only
```

Do not place real secrets in documentation, issue reports, screenshots, or source examples.

Secret custom-field values are not shown in the quick-copy list. Copy remains an explicit user action.

## 5. Secure notes

Secure Note content uses a deliberately small Markdown-like subset rather than arbitrary HTML.

Supported concepts include:

- headings;
- paragraphs;
- bullet lists;
- checklists;
- fenced code blocks.

Raw HTML is neutralized instead of executed. The same 200,000-character and 5,000-line policy applies to parsing, checklist changes, save/import validation, and preview so one path cannot save a note that another path rejects only because of size.

See `security/SECURE_NOTES.md`.

## 6. Attachments

Attachments are encrypted separately from item JSON and referenced from the encrypted item payload.

### Add attachment

1. Save a new item first.
2. Reopen the saved item.
3. Choose Add Attachment.
4. Select a local file through the platform file picker.
5. CipherNest normalizes/validates display name and media type before encryption.
6. The file is encrypted in bounded chunks into the local encrypted attachment store.

Current limits:

- maximum plaintext size: 100 MiB per file;
- maximum 25 attachments per item;
- maximum 10,000 referenced attachments across the current vault resource policy;
- display name maximum 240 characters;
- media type maximum 256 characters.

Encrypted storage names are opaque GUID-based `.cna` names bound to the attachment ID.

### Safe text preview

Small supported UTF-8 text-family attachments can be previewed in memory. The preview path is bounded and does not intentionally create a plaintext preview file. Unsupported/binary content requires deliberate export when the user needs an external viewer.

### Export attachment

Attachment export leaves the encrypted vault boundary.

1. CipherNest shows an explicit plaintext-export warning.
2. The decrypted attachment is written to a unique temporary app-cache path for the operating-system share flow.
3. CipherNest attempts to delete the temporary plaintext file after the share request returns.
4. Other apps, platform share services, filesystem snapshots, backups, antivirus/indexers, or destination providers can retain copies outside CipherNest's control.
5. If CipherNest cannot confirm removal of its temporary staging file, it reports a fixed warning without exposing the sensitive path.

## 7. Search, filters, sorting, collections, and favorites

Search and organization run over decrypted authenticated items only while the vault is unlocked. CipherNest intentionally avoids storing a plaintext searchable SQL/FTS index.

Current organization includes:

- local text search;
- favorites;
- collections;
- type filter;
- review-due filter;
- recent-use sorting;
- recent-modification sorting;
- title sorting;
- favorite/title ordering.

Large local matching result sets render in 50-item visual pages with a Load More action. This visual paging does not mean the encrypted database stores plaintext search indexes.

Search input itself is bounded by the service so extremely large queries are rejected rather than being processed without limit.

## 8. Recent use and review dates

Opening an item records an encrypted `LastAccessedUtc` timestamp without changing the user-visible modification timestamp. Review dates are stored encrypted inside the item payload.

Settings can enable local review reminders and configure a lead time. These reminders are computed locally over decrypted authenticated item data while unlocked.

## 9. Security audit

The local Security Audit can identify findings such as:

- weak secrets;
- reused secrets;
- exact duplicate entries;
- missing titles;
- overdue review dates.

TOTP seeds are deliberately excluded from password weakness/reuse findings because they are authentication seeds rather than user-chosen passwords; exact duplicate detection still applies.

Audit results are local application findings, not an independent security audit of the CipherNest codebase.

## 10. Password and passphrase generator

The password generator uses the platform cryptographic random-number generator. Settings can persist defaults for:

- password/passphrase mode;
- password length;
- passphrase word count;
- uppercase/lowercase/digits/symbols;
- ambiguous-character exclusion.

Password length is normalized to 8–256 characters. Passphrase word count is normalized to 6–16 words. The memorable passphrase mode uses a validated local list of exactly 256 unique lowercase words and defaults to eight words.

The displayed entropy guidance applies to randomly selected generated output. Editing generated output can reduce the stated random-selection entropy.

See `security/PASSPHRASE_GENERATOR.md`.

## 11. Clipboard behavior

Username, primary-secret, secret-custom-field, TOTP-code, and TOTP-setup-URI copy actions are explicit.

After a successful sensitive copy, CipherNest keeps a fixed-size SHA-256 fingerprint for delayed comparison rather than retaining the copied plaintext in the timer state. It clears only when the current clipboard still matches the value CipherNest previously copied, helping avoid erasing unrelated clipboard content copied afterward.

A TOTP setup URI normally contains the long-lived seed, so its clipboard exposure can remain useful to an attacker much longer than one generated code.

Platform clipboard history, clipboard synchronization, keyboard software, accessibility services, other apps, screenshots, and OS caches remain outside CipherNest's deletion guarantees.

## 12. Trash and permanent deletion

### Move to Trash

Moving an item to Trash is reversible while the record remains within the configured retention period.

### Restore from Trash

Open Trash and restore the item before permanent deletion or retention cleanup.

### Permanent deletion

Manual permanent deletion and Empty Trash require:

- the current master passphrase;
- successful re-authentication;
- a separate destructive confirmation.

Permanent item deletion removes the encrypted database record before best-effort encrypted attachment cleanup. Logical deletion cannot guarantee physical erasure from flash translation layers, filesystem snapshots, device backups, or forensic remnants.

Default trash retention is 30 days and the valid setting range is 1–365 days.

## 13. Encrypted backups

Encrypted backup is the recommended transfer/recovery mechanism.

### Create a backup

1. Open Settings.
2. Enter a strong backup passphrase of 12–4,096 characters.
3. Confirm backup creation.
4. CipherNest locks the vault before creating the consistent snapshot so edits do not race the snapshot.
5. The database snapshot plus encrypted attachment containers are placed inside a bounded archive and then encrypted/authenticated using a separately derived backup key.
6. The resulting extension is `.cnbak`.
7. Store the backup passphrase separately from the backup file.
8. Periodically test restore using disposable data rather than assuming a backup is valid.

The app-private backup path is not automatically equivalent to an off-device backup. Copy/store the encrypted backup according to your recovery plan.

### Restore a backup

1. Enter the backup passphrase.
2. Select a `.cnbak` file.
3. Confirm replacement.
4. CipherNest locks the current vault.
5. The encrypted container is authenticated and staged.
6. Backup metadata, chunk framing, archive paths/counts/sizes, and replacement SQLite structure/resources are validated before active database mutation.
7. On successful restore, local biometric pairing is cleared and must be configured again intentionally.
8. Unlock the restored vault with its own master passphrase or recovery material.

Failed restore attempts are designed to preserve the active vault through staging/recovery logic, but release confidence still depends on the repository's tests and target-environment verification.

## 14. CSV import and plaintext export

### CSV import

CSV import requires explicit user mapping. CipherNest does not silently decide which arbitrary column contains a secret.

Supported mapping targets are:

- Title
- Username
- Secret
- URL
- Notes
- Tags
- Collection
- Type

Review every mapping before import. The source CSV remains plaintext outside CipherNest; importing it does not delete or encrypt the original external file.

Dedicated TOTP setup-URI import is handled directly inside a TOTP Item Editor rather than through generic CSV mapping.

### Plaintext CSV export

Plaintext CSV export is an interoperability escape hatch, not the recommended backup path.

It requires:

- the exact confirmation phrase `EXPORT PLAINTEXT`;
- current-master re-authentication;
- a separate warning/confirmation.

Attachments are not included in plaintext CSV export. CipherNest creates a temporary plaintext CSV for the share operation and attempts cleanup afterward, but destination applications and the operating system can retain copies.

## 15. Settings reference

Current settings include:

- System/Light/Dark theme;
- System/English/Hindi language preference for the reviewed resource-backed interface;
- inactivity lock timeout;
- lock on background;
- clipboard-clear delay;
- screenshot-protection preference;
- biometric convenience unlock;
- periodic master-passphrase interval;
- reduced motion;
- larger interface;
- trash-retention period;
- backup reminder interval;
- review-reminder enable/lead time;
- generator defaults;
- local storage/cache inspection and cleanup;
- encrypted backup/restore;
- CSV transfer;
- security/privacy information;
- About/legal/acknowledgements;
- master-passphrase change;
- full local-vault deletion.

Invalid persisted numeric/enum values are normalized by the settings policy rather than blindly trusted.

## 16. Change master passphrase

Changing the master passphrase requires the current master passphrase and a sufficiently strong new passphrase. CipherNest rewrites the master wrapper for the same random vault data-encryption key rather than bulk-re-encrypting every item solely because the master passphrase changes.

After a successful change:

- the remembered master-auth session is cleared;
- the current vault session is locked;
- conditional clipboard cleanup is attempted;
- the new master passphrase is required before biometric convenience unlock can resume.

Existing recovery material remains an independent wrapper unless a future reviewed design explicitly changes that behavior.

## 17. Delete the local vault

Full local-vault deletion requires:

- exact phrase `DELETE MY VAULT`;
- current master passphrase;
- explicit final confirmation.

CipherNest binds destructive authorization to the active security session so an intervening lock/unlock can invalidate stale authorization before the destructive transition proceeds.

The operation removes CipherNest-managed local encrypted database and attachment data where permitted. It cannot guarantee physical sanitization of device storage, snapshots, backups, exported plaintext, or copies held by other applications.

## 18. Storage and cache

Settings can measure CipherNest app-data and temporary-cache usage. Cache cleanup targets CipherNest-managed temporary files where platform permissions permit access and avoids intentionally deleting the encrypted vault/attachments/backups.

Reparse-point directories are not recursively followed by the maintenance implementation.

## 19. Accessibility and language

CipherNest includes semantic metadata, larger-interface preference support, reduced-motion state, responsive layouts, minimum touch-target guidance, and neutral-English-fallback/reviewed-Hindi resource-backed localization architecture.

Neutral English remains the fallback. System/English/Hindi preferences exist, and the reviewed Hindi (`hi-IN`) catalog covers the currently resource-backed interface. A completely translated application is not claimed because remaining literal UI strings may still appear in English.

See `ACCESSIBILITY.md` and `architecture/LOCALIZATION.md`.

## 20. Privacy-safe diagnostics

CipherNest does not enable a third-party analytics or crash-reporting service in the current source. The internal privacy-safe exception reporter records sanitized operation identifiers, exception type, HResult, severity, and fixed text while intentionally omitting exception messages/stacks and decrypted vault content.

TOTP seeds, generated codes, and setup URIs must not be emitted through diagnostics or support artifacts.

See `privacy/DIAGNOSTICS.md` and `../PRIVACY.md`.

## 21. Support and security reports

Repository: https://github.com/sanskarIN/CipherNest  
Business: sanskarin@outlook.in  
Support: supportramsandesh@gmail.com

For security issues, follow `../SECURITY.md`. Never send a real vault, passphrase, recovery key, TOTP seed/setup URI, decrypted backup, secret-bearing screenshot, signing key, or private attachment to a public issue.

## 22. Features intentionally not represented as complete

The current release does not claim completed support for:

- cloud synchronization/accounts/collaboration;
- server-side vault storage;
- browser/app autofill;
- Windows Hello convenience unlock;
- rich binary/PDF preview and document scanning beyond bounded safe text preview;
- pronounceable-password mode;
- destructive automatic wipe after failed unlock attempts;
- complete migration/review of the remaining UI into Hindi/additional translation catalogs;
- TOTP QR scanning/rendering, HOTP interoperability, and autofill/provider enrollment integration.

See `NEXT_STEPS.md` for the reviewed future-work sequence.
