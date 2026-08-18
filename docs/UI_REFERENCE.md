# CipherNest UI and Navigation Reference

This document describes the current .NET MAUI application surface page by page. It is a functional/UI reference, not a substitute for the security and format documents.

The root `AppShell` disables the flyout and default Shell navigation bar. Top-level navigation is explicit through application buttons/commands. Routes that display decrypted data must respect vault lock state and must never become a hidden bypass after the session locks.

## 1. Shell route map

Current top-level Shell routes:

```text
startup
onboarding
unlock
vault
generator
audit
trash
settings
security-info
transfer
about
developer
```

Additional registered routes:

```text
ItemEditorPage
GeneratorDefaultsPage
```

### Typical first-launch flow

```text
Startup
  ├─ no vault ─> Onboarding ─> Vault
  └─ vault exists ─> Unlock ─> Vault
```

### Typical unlocked navigation

```text
Vault
  ├─ Add/Open ─> Item Editor
  ├─ Generate ─> Generator
  ├─ Audit ─> Audit
  ├─ Trash ─> Trash
  ├─ Settings ─> Settings
  │    ├─ Generator Defaults
  │    ├─ Security Info
  │    ├─ Transfer
  │    └─ About
  ├─ About ─> About
  └─ Lock ─> Unlock
```

## 2. Startup Page

**Route:** `startup`

### Purpose

Startup determines the local application state before a user enters a sensitive workflow.

### Responsibilities

- determine whether a local CipherNest vault exists;
- apply startup preference state through the application startup path;
- route to Onboarding for a new installation;
- route to Unlock when a vault already exists;
- fail safely when startup preference/platform application fails;
- avoid displaying decrypted vault content.

### Security notes

Startup preference application is not trusted to authorize a vault session. Vault existence does not imply unlock authorization.

## 3. Onboarding Page

**Route:** `onboarding`

### Purpose

Creates the local vault and optionally produces one-time recovery material.

### Main UI

- CipherNest logo;
- **Create your local vault** title;
- local-first/no-cloud explanation;
- master-passphrase field;
- strength label;
- master-passphrase confirmation field;
- optional recovery-key checkbox;
- recovery-limitation acknowledgement checkbox;
- **Create vault** button;
- busy/error states;
- one-time recovery-key display after successful creation;
- acknowledgement that recovery material has been stored safely;
- **Continue to vault** action;
- `Made by the Sanskar` creator credit.

### Master-passphrase behavior

- cryptographic input range: 12–4,096 characters;
- onboarding also applies its strength policy;
- the passphrase is not stored as the vault key;
- a random DEK is wrapped using a passphrase-derived key.

### Recovery behavior

If enabled, recovery material is shown during setup and must be stored separately. CipherNest does not provide a server-side password-reset path.

### Sensitive-state expectation

Bound credential/recovery fields are treated as sensitive state. They must not be copied into logs, diagnostics, screenshots, or public support artifacts.

## 4. Unlock Page

**Route:** `unlock`

### Purpose

Authenticates a user into an existing local vault.

### Main UI

- CipherNest logo;
- **Unlock CipherNest** title;
- local-vault explanation;
- optional **Unlock with biometrics** button when available;
- master-passphrase/recovery-key field;
- error status;
- **Unlock** button;
- busy indicator;
- explicit recovery-limitation explanation.

### Supported unlock paths

- master passphrase;
- configured recovery material;
- optional biometric convenience unlock on supported Android/iOS/Mac Catalyst installations after the required master-auth state exists.

### Biometric limitations

Biometrics are convenience authorization, not recovery. Windows currently falls back to master-passphrase unlock.

### Failed attempts

Interactive failures use bounded client-side backoff. This is not protection against an attacker performing offline guesses against copied encrypted data.

## 5. Vault Page

**Route:** `vault`

### Purpose

Primary unlocked workspace for finding, opening, creating, and organizing encrypted vault items.

### Header

- **Your vault** title;
- explanation that secrets remain hidden until explicit open/copy;
- **Lock** button.

### Reminder/status surface

- backup reminder message;
- review reminder message.

### Search

The SearchBar searches decrypted authenticated item content while the vault is unlocked. The placeholder identifies searchable categories such as title, collection, tags, username, URL, notes, and custom fields.

Search does not create a plaintext database FTS index.

### Sort controls

Current sort choices are supplied by the ViewModel and include:

- Favorites & title;
- Recently used;
- Recently modified;
- Title.

### Filter controls

Current filter surface includes:

- item-type filtering;
- Favorites;
- Review due;
- other ViewModel-defined filter modes;
- optional collection/folder text filter.

### Item list

Each visible result card presents non-secret summary information such as:

- title;
- item type;
- collection;
- **Open** action.

Secrets are not rendered in the list.

### Incremental rendering

The page displays a result-count message and an optional **Load more** action. Matching results enter the visual tree in 50-item pages.

### Main action strip

Current actions:

- **Add**
- **Generate**
- **Audit**
- **Trash**
- **Settings**
- **About**
- **☕ Support** when the funding surface is enabled
- **Refresh**

The action strip uses a wrapping layout for narrow/resizable surfaces.

### BMC support behavior

The compact support action routes through About and appears only when `BuildFeatureFlags.IsFundingLinkEnabled` is true. A distribution build can compile the app with the funding CTA disabled.

## 6. Item Editor Page

**Route:** `ItemEditorPage`

### Purpose

Creates, opens, edits, protects, and organizes one vault item.

### Re-authentication gate

When the item requires re-authentication, the page first displays a protected gate:

- explanation that current-master authentication is required;
- current-master field;
- **Unlock this item** button.

Recovery material is not accepted for this current-master authorization check.

### Type selector

The picker supports the domain item types:

- Login;
- Secure Note;
- Identity;
- Payment Card Reference;
- Wi-Fi Credential;
- Software License;
- Server/SSH Reference;
- Document;
- Custom;
- Time-Based One-Time Password.

### Core fields

- Title;
- Collection/folder;
- Username/identifier;
- Secret;
- URL;
- Tags;
- Favorite;
- Require master re-authentication;
- optional review/expiration date.

### Username actions

- username/identifier entry;
- explicit **Copy** button.

### Secret actions

- masked secret entry by default;
- **Reveal** action;
- explicit **Copy** action.

Reveal/copy are deliberate actions and do not remove clipboard/screen-capture limitations.

### TOTP panel

Visible when the selected item type is TOTP.

Controls include:

- explanation that Secret is the Base32 TOTP seed;
- masked sensitive `otpauth://totp/...` setup-URI import field;
- **Import URI**;
- **Copy setup URI**;
- algorithm picker: SHA-1 / SHA-256 / SHA-512;
- digit picker: 6 / 8;
- period display;
- period Stepper: 15–120 seconds in 15-second increments;
- current generated code;
- **Refresh code**;
- **Copy code**;
- seconds-remaining status;
- warning about clipboard exposure and authorization to store the seed/setup URI.

#### TOTP setup-URI import

The import field is treated as sensitive transient state because a normal setup URI contains the long-lived seed. Import is local and accepts bounded `otpauth://totp/...` input only. HOTP/counter input, duplicate security-sensitive query keys, unsupported TOTP settings, malformed seeds, inconsistent issuer metadata, and out-of-policy metadata are rejected.

A successful import maps:

- `secret=` to the item Secret;
- account label to Username/identifier;
- issuer to Title when supplied;
- algorithm, digits, and period to the existing TOTP settings.

The dedicated URI entry is cleared after the import attempt. Users should review the imported account/issuer/settings before saving because local parsing does not contact or verify the authentication provider.

#### TOTP setup-URI copy

**Copy setup URI** formats the current account/issuer/seed/settings into a local canonical `otpauth://totp/...` value and copies it through `IClipboardSecurityService`. The URI is not added as a second persisted vault field.

Setup-URI clipboard exposure is more consequential than copying one short-lived generated code because the URI contains the long-lived seed. Timed clipboard cleanup remains best effort and cannot erase operating-system history/synchronization copies.

Generated codes are not persisted.

### Review date

A checkbox enables the DatePicker for an optional review/expiration reminder date.

### Custom fields

The editor accepts one `name=value` pair per line.

Secret fields use the prefix:

```text
[secret]PIN=1234
```

Secret custom-field values are not displayed in the quick-copy list; only the field name is shown with an explicit **Copy secret** action.

### Secure Note panel

Controls include:

- encrypted Markdown-like editor;
- safe-subset explanation;
- **Preview** toggle;
- safe preview display;
- checklist draft field;
- **Add checklist** action.

The safe subset covers headings, bullets, checklists, and fenced code. HTML is not rendered.

### Attachments panel

Controls include:

- **Add file**;
- attachment display name;
- plaintext byte length metadata;
- **Preview**;
- **Export**;
- **Remove**.

Text preview supports the bounded safe text-family policy. Export is a deliberate plaintext boundary and uses the operating-system share flow after confirmation.

### Save/delete actions

- **Save**;
- **Move to trash** for an existing item.

Permanent deletion is intentionally handled through the Trash workflow rather than a casual editor action.

## 7. Generator Page

**Route:** `generator`

### Purpose

Generates passwords or memorable passphrases locally.

### Supported behavior

- password mode;
- passphrase mode;
- saved defaults loaded on page appearance;
- configurable length/word count;
- character-group selection;
- ambiguous-character exclusion;
- strength/entropy guidance;
- explicit copy behavior through the application clipboard policy.

### Security note

Strength labels are guidance, not a formal proof of resistance against a specific attacker.

## 8. Generator Defaults Page

**Route:** `GeneratorDefaultsPage`

### Purpose

Persists non-secret generator defaults.

### Configurable defaults

- password vs passphrase mode;
- password length;
- passphrase word count;
- uppercase;
- lowercase;
- digits;
- symbols;
- ambiguous-character exclusion.

If password mode would contain no character group, normalization restores a safe character group rather than persisting an unusable configuration.

## 9. Audit Page

**Route:** `audit`

### Purpose

Displays the local security-audit findings generated from decrypted items while unlocked.

### Current finding families

- weak secrets;
- reused secrets;
- exact duplicate entries;
- missing titles;
- overdue review dates.

TOTP seeds are not treated as ordinary user passwords for weakness/reuse heuristics.

### Important wording

This page is a vault-content audit, not an independent professional security audit of CipherNest source code or cryptographic design.

## 10. Trash Page

**Route:** `trash`

### Purpose

Manages soft-deleted vault items.

### User actions

- inspect trashed items;
- restore an item;
- permanently delete an item;
- empty trash.

### Authentication rules

Manual permanent deletion and Empty Trash require:

1. current-master re-authentication;
2. separate destructive confirmation.

### Retention

Default retention is 30 days; configurable range is 1–365 days. Routine maintenance can remove expired trash records.

### Deletion limitation

Deletion is logical application-managed deletion, not guaranteed physical-media sanitization.

## 11. Settings Page

**Route:** `settings`

The Settings page is the largest configuration surface and is intentionally divided into visible cards/sections.

### Appearance & accessibility

- Theme picker: System / Light / Dark;
- Language picker: System / English / Hindi;
- **Save language preference**;
- Reduced motion switch;
- Larger interface switch.

Neutral English remains the fallback. The reviewed Hindi catalog covers the resource-backed interface; every remaining literal is not claimed translated.

### Lock & privacy

- lock timeout seconds;
- lock-on-background switch;
- clipboard-clear seconds;
- screenshot-protection switch;
- screenshot support message;
- trash-retention days.

### Local reminders

- backup reminder interval;
- review-reminder enable switch;
- review-reminder lead time;
- local-only explanation;
- **Save settings**.

### Generator defaults

- explanation;
- **Configure generator defaults**.

### Biometric unlock

- capability/support message;
- explanation that biometrics do not replace the master/recovery model;
- periodic master-passphrase interval;
- current-master field;
- **Enable biometrics**;
- **Disable biometrics**.

### Security review

- **Run security audit**;
- **Security & privacy info**.

### Encrypted backup & restore

- separate backup-passphrase field;
- **Create backup**;
- **Restore backup**;
- explanation that the vault locks before consistent snapshot creation.

### Storage & cache

- storage-usage message;
- explanation of cache-cleanup scope;
- **Refresh storage**;
- **Clear temporary cache**.

Cache cleanup is not intended to delete the encrypted database, encrypted attachment store, or app-data backups.

### Import & export

- explanation that encrypted backup is recommended;
- **Open import & export**.

### BMC support card

Visible only when funding UI is enabled.

- highlighted `☕ Support CipherNest development` heading;
- `bmc_support.svg` visual;
- statement that support is optional and does not change feature/security/privacy/licensing/recovery/support priority;
- **☕ View Buy Me a Coffee support** button routing through About.

### About/legal

- **Open About & legal**.

### Change master passphrase

- current master passphrase;
- new master passphrase;
- confirmation;
- **Change master passphrase**.

A successful change clears remembered master-auth state and locks the vault so the new master passphrase is required again.

### Danger zone

- physical-erasure limitation warning;
- current-master field;
- confirmation phrase field expecting `DELETE MY VAULT`;
- **Delete local vault** destructive action.

## 12. Security Info Page

**Route:** `security-info`

### Purpose

Presents user-facing security/privacy limitations and design facts without exposing raw cryptographic internals as an unsupported promise.

Expected topics include:

- local-first design;
- audit status;
- master/recovery limitations;
- biometrics as convenience only;
- clipboard/screenshot/platform boundaries;
- plaintext export limitations;
- managed-memory limitations;
- logical deletion limitations.

The page should remain synchronized with the threat model and privacy documentation.

## 13. Transfer Page

**Route:** `transfer`

### Purpose

Hosts plaintext CSV interoperability.

### Import flow

- select CSV;
- preview bounded headers;
- map source headers explicitly to supported targets;
- confirm import;
- import bounded/validated rows.

Supported mapping targets include:

- Title;
- Username;
- Secret;
- URL;
- Notes;
- Tags;
- Collection;
- Type.

### Export flow

Plaintext export requires:

- exact phrase `EXPORT PLAINTEXT`;
- current-master re-authentication;
- explicit warning/confirmation;
- share flow;
- best-effort temporary-file cleanup.

Attachments are not silently embedded in CSV export. TOTP setup-URI interoperability is a separate single-item Item Editor workflow rather than part of generic CSV transfer.

## 14. About Page

**Route:** `about`

### Purpose

Central product identity, legal, repository, support, audit-status, and optional funding surface.

### Information represented

- product name;
- runtime/source version/build information;
- GPL-3.0-or-later license reference;
- privacy reference;
- terms reference;
- third-party notices/acknowledgements;
- repository URL;
- creator URL;
- business/support contacts;
- independent-audit status;
- optional Buy Me a Coffee support.

### External links

External links are user-initiated platform operations and can fail. Failures should use fixed user-safe text plus privacy-safe diagnostics rather than exposing raw exception/path/context details.

## 15. Developer Page

**Route:** `developer`

### Purpose

Provides developer-oriented/redacted diagnostic information without turning production UI into a secret-bearing log viewer.

### Privacy boundary

Developer diagnostics must not expose:

- master/backup passphrases;
- recovery material;
- secondary secrets;
- DEKs/KEKs;
- decrypted vault values;
- TOTP seeds/codes/setup URIs;
- clipboard plaintext;
- private attachment contents;
- raw exception messages/stacks containing paths/context.

## 16. Sensitive page lifecycle rules

Sensitive ViewModels/pages clear bound credential/decrypted state when disappearing where the current implementation owns that state. Several longer-running authentication/file/share workflows also clear bound passphrase fields before continuing where practical.

The Item Editor specifically clears the dedicated TOTP setup-URI import field after import attempts and again when the editor clears sensitive state on page disappearance.

This reduces lifetime but cannot guarantee removal of .NET managed strings or OS/application copies.

## 17. Accessibility expectations

UI changes must preserve:

- semantic names/descriptions where required;
- status/live-region semantics where supported;
- keyboard/focus behavior on desktop;
- adequate touch targets;
- dynamic typography;
- Larger Interface behavior;
- Reduced Motion preference behavior;
- System/Light/Dark readability;
- narrow/resizable layouts;
- security warnings without semantic dilution.

Source metadata is not a substitute for TalkBack, VoiceOver, Narrator, keyboard-only, focus, scaling, and contrast testing on release targets.

## 18. Localization expectations

The current preference model supports:

- System;
- English;
- Hindi.

Neutral English is the fallback. A reviewed `hi-IN` catalog exists for the resource-backed surface, but remaining non-resource literals can still appear in English.

Do not claim a fully translated interface until all remaining user/security/error text has been migrated and reviewed.

## 19. Theme and responsive behavior

CipherNest uses MAUI resources and responsive layouts to support phone and desktop/resizable windows. UI changes should avoid fixed assumptions that only work at one device size.

The Vault action area specifically uses wrapping behavior so primary actions can reflow instead of clipping on narrow surfaces.

## 20. Screenshot, clipboard, picker, share, and lifecycle boundaries

These are OS/platform services, not pure UI controls. Source behavior can only partially control them.

Release validation must verify:

- screenshot/task-preview behavior;
- clipboard history/synchronization/cleanup, including copied TOTP setup URIs;
- file picker cancellation/errors;
- share-sheet completion/retention;
- app background/sleep/resume transitions;
- secure-storage behavior;
- biometric enrollment/cancellation/lockout;
- setup-URI import/copy usability and representative third-party TOTP compatibility using synthetic seeds;
- screen reader/focus interactions.

## 21. Funding-surface build behavior

In-app BMC UI is enabled by default. The build property:

```bash
-p:CipherNestEnableFundingLink=false
```

defines the app's funding-disable symbol and removes/hides in-app funding surfaces guarded by `BuildFeatureFlags.IsFundingLinkEnabled`.

Repository `.github/FUNDING.yml` remains separate.

## 22. UI change checklist

Before merging a UI change, confirm:

- decrypted data is not introduced into persistent/plaintext UI caches;
- secrets stay masked until explicit reveal;
- copy is explicit;
- setup URIs containing TOTP seeds are treated as secret data;
- sensitive fields are cleared when ownership/lifecycle allows;
- raw exception messages are not shown on sensitive paths;
- platform calls are contained and reported safely;
- lock-state navigation cannot leave a hidden decrypted bypass;
- accessibility metadata is preserved;
- narrow/resizable layout is considered;
- localization/security wording stays accurate;
- funding surfaces obey the build flag;
- source/UI tests and user documentation are updated;
- physical-device behavior is still validated before release claims.

## 23. Related documentation

- [`QUICK_START.md`](QUICK_START.md)
- [`USER_GUIDE.md`](USER_GUIDE.md)
- [`FEATURE_MATRIX.md`](FEATURE_MATRIX.md)
- [`CONFIGURATION_REFERENCE.md`](CONFIGURATION_REFERENCE.md)
- [`ACCESSIBILITY.md`](ACCESSIBILITY.md)
- [`architecture/LOCALIZATION.md`](architecture/LOCALIZATION.md)
- [`security/SESSION_SECURITY.md`](security/SESSION_SECURITY.md)
- [`security/DATA_LIFECYCLE.md`](security/DATA_LIFECYCLE.md)
- [`security/TOTP.md`](security/TOTP.md)
- [`privacy/DIAGNOSTICS.md`](privacy/DIAGNOSTICS.md)
- [`COMPLETE_PROJECT_DOCUMENTATION.md`](COMPLETE_PROJECT_DOCUMENTATION.md)
