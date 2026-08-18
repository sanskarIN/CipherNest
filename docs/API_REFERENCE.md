# CipherNest Application API Reference

This document describes the current public application/domain contracts used across CipherNest source projects. It is an internal developer reference for the repository, not a network API: the current release exposes no CipherNest application server or remote account API.

Namespace locations are shown so changes can be reviewed against the actual source.

## `CipherNest.Application.Abstractions.IVaultService`

Main application-facing vault use-case boundary.

### State/event

```csharp
bool IsUnlocked { get; }
event EventHandler<bool>? LockStateChanged;
```

`LockStateChanged` communicates transitions to locked/unlocked application state. Consumers must not treat this event as authorization to retain a previously obtained key or decrypted record indefinitely.

### Vault lifecycle

```csharp
Task<bool> HasVaultAsync(CancellationToken cancellationToken = default);
Task<string?> CreateAsync(
    string masterPassphrase,
    bool createRecoveryKey = true,
    CancellationToken cancellationToken = default);
Task UnlockAsync(
    string masterPassphraseOrRecoveryKey,
    CancellationToken cancellationToken = default);
Task LockAsync(CancellationToken cancellationToken = default);
```

`CreateAsync` returns one-time recovery material when recovery creation is requested/succeeds; otherwise it can return `null`.

### Authentication and secondary convenience unlock

```csharp
Task UnlockWithSecondarySecretAsync(
    string secondarySecret,
    CancellationToken cancellationToken = default);
Task<bool> ReauthenticateAsync(
    string masterPassphrase,
    CancellationToken cancellationToken = default);
Task EnableSecondaryUnlockAsync(
    string masterPassphrase,
    string secondarySecret,
    CancellationToken cancellationToken = default);
Task DisableSecondaryUnlockAsync(
    string masterPassphrase,
    CancellationToken cancellationToken = default);
Task<bool> IsSecondaryUnlockConfiguredAsync(
    CancellationToken cancellationToken = default);
```

Secondary unlock is a wrapper path for an independently generated secret. The current MAUI app places that secret in platform secure storage and gates convenience use with platform biometric authentication where supported. It is not a recovery mechanism.

`ReauthenticateAsync` verifies the current master passphrase for sensitive actions; recovery material is not intended to substitute for current-master authorization in these flows.

### Master-passphrase change and full-vault deletion

```csharp
Task ChangeMasterPassphraseAsync(
    string currentMasterPassphrase,
    string newMasterPassphrase,
    CancellationToken cancellationToken = default);
Task DeleteVaultAsync(
    string masterPassphrase,
    CancellationToken cancellationToken = default);
```

These are security-sensitive mutations and participate in current session/transition rules documented in `architecture/SESSION_AND_CONCURRENCY.md`.

### Item operations

```csharp
Task<IReadOnlyList<VaultItem>> GetItemsAsync(
    bool includeTrash = false,
    CancellationToken cancellationToken = default);
Task<VaultItem?> GetItemAsync(
    Guid id,
    CancellationToken cancellationToken = default);
Task SaveItemAsync(
    VaultItem item,
    CancellationToken cancellationToken = default);
Task MarkAccessedAsync(
    Guid id,
    CancellationToken cancellationToken = default);
Task MoveToTrashAsync(
    Guid id,
    CancellationToken cancellationToken = default);
Task RestoreFromTrashAsync(
    Guid id,
    CancellationToken cancellationToken = default);
Task DeletePermanentlyAsync(
    Guid id,
    CancellationToken cancellationToken = default);
Task<IReadOnlyList<VaultItem>> SearchAsync(
    string query,
    CancellationToken cancellationToken = default);
```

Returned items are decrypted authenticated domain objects. They must not be persisted/logged by consumers in plaintext merely because the service returned them.

### Attachment operations

```csharp
Task<AttachmentReference> AddAttachmentAsync(
    Guid itemId,
    Stream source,
    string displayName,
    string mediaType,
    CancellationToken cancellationToken = default);
Task RemoveAttachmentAsync(
    Guid itemId,
    Guid attachmentId,
    CancellationToken cancellationToken = default);
Task ExportAttachmentAsync(
    Guid itemId,
    Guid attachmentId,
    Stream destination,
    CancellationToken cancellationToken = default);
```

`ExportAttachmentAsync` writes plaintext to the caller-provided stream after authenticated decryption. The caller owns the destination's security/cleanup behavior.

## `CipherNest.Application.Abstractions.IVaultStore`

Infrastructure persistence abstraction. Application/UI code should normally use `IVaultService`, not `IVaultStore` directly.

```csharp
string DatabasePath { get; }
Task InitializeAsync(CancellationToken cancellationToken = default);
Task<bool> HasVaultAsync(CancellationToken cancellationToken = default);
Task<string?> ReadHeaderAsync(CancellationToken cancellationToken = default);
Task WriteHeaderAsync(string headerJson, CancellationToken cancellationToken = default);
Task<IReadOnlyList<StoredVaultItem>> ReadAllItemsAsync(CancellationToken cancellationToken = default);
Task UpsertItemAsync(StoredVaultItem item, CancellationToken cancellationToken = default);
Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);
Task CreateConsistentSnapshotAsync(
    string destinationDatabasePath,
    CancellationToken cancellationToken = default);
Task ReplaceDatabaseAsync(
    string sourceDatabasePath,
    CancellationToken cancellationToken = default);
Task DeleteDatabaseAsync(CancellationToken cancellationToken = default);
```

### `StoredVaultItem`

```csharp
public sealed record StoredVaultItem(Guid Id, byte[] Envelope);
```

The `Id` is authenticated as record context/associated data; the opaque `Envelope` contains authenticated encrypted item JSON. Store implementations must enforce resource bounds compatible with `VaultStorageLimits` and service-level validation.

## `CipherNest.Application.Abstractions.ICryptoService`

Low-level cryptographic abstraction implemented by Infrastructure.

### Parameter/envelope records

```csharp
public sealed record KdfParameters(
    int MemoryKiB,
    int Iterations,
    int Parallelism);

public sealed record WrappedKeyEnvelope(
    int Version,
    byte[] Salt,
    KdfParameters Kdf,
    byte[] Nonce,
    byte[] Ciphertext,
    byte[] Tag);

public sealed record EncryptedEnvelope(
    int Version,
    byte[] Nonce,
    byte[] Ciphertext,
    byte[] Tag);
```

### Methods

```csharp
WrappedKeyEnvelope CreateWrappedKey(ReadOnlySpan<char> passphrase);
WrappedKeyEnvelope WrapKey(
    ReadOnlySpan<byte> dataKey,
    ReadOnlySpan<char> passphrase);
byte[] UnwrapKey(
    ReadOnlySpan<char> passphrase,
    WrappedKeyEnvelope envelope);
EncryptedEnvelope Encrypt(
    ReadOnlySpan<byte> plaintext,
    ReadOnlySpan<byte> key,
    ReadOnlySpan<byte> associatedData);
byte[] Decrypt(
    EncryptedEnvelope envelope,
    ReadOnlySpan<byte> key,
    ReadOnlySpan<byte> associatedData);
byte[] DeriveKey(
    ReadOnlySpan<char> passphrase,
    ReadOnlySpan<byte> salt,
    KdfParameters parameters);
```

Do not use this abstraction from UI code to create parallel encryption flows that bypass Vault/Backup/Attachment format/version policies.

## `CipherNest.Application.Abstractions.IBackupService`

```csharp
Task ExportEncryptedAsync(
    string destinationPath,
    string backupPassphrase,
    CancellationToken cancellationToken = default);
Task RestoreEncryptedAsync(
    string sourcePath,
    string backupPassphrase,
    CancellationToken cancellationToken = default);
```

Export/restore operates on the authenticated encrypted backup format documented in `formats/ENCRYPTED_BACKUP.md` and intentionally uses a backup passphrase separate from the vault master-passphrase API contract.

## `CipherNest.Application.Abstractions.IPlaintextTransferService`

Plaintext interoperability boundary.

### Mapping/result records

```csharp
public sealed record CsvImportMapping(
    string Title,
    string? Username = null,
    string? Secret = null,
    string? Url = null,
    string? Notes = null,
    string? Tags = null,
    string? Collection = null,
    string? Type = null);

public sealed record CsvImportResult(
    int Imported,
    int Skipped,
    IReadOnlyList<string> Warnings);
```

### Methods

```csharp
Task<IReadOnlyList<string>> ReadHeadersAsync(
    Stream source,
    CancellationToken cancellationToken = default);
Task<CsvImportResult> ImportCsvAsync(
    Stream source,
    CsvImportMapping mapping,
    CancellationToken cancellationToken = default);
Task ExportCsvAsync(
    Stream destination,
    CancellationToken cancellationToken = default);
```

The service itself provides parsing/transfer behavior. UI-level current-master confirmation and the exact plaintext-export acknowledgement phrase are separate App responsibilities.

## `CipherNest.Application.Abstractions.ISettingsStore`

```csharp
Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default);
Task SaveAsync(
    AppPreferences preferences,
    CancellationToken cancellationToken = default);
```

Settings are non-secret preferences. `JsonSettingsStore` plus `AppPreferencesPolicy` provide bounded normalization/fallback semantics.

## `CipherNest.Application.Abstractions.IPasswordGenerator`

```csharp
string Generate(GeneratorOptions options);
PasswordStrengthResult Evaluate(string secret);
```

### `GeneratorOptions`

```csharp
public enum GeneratorMode
{
    Password,
    Passphrase
}

public sealed record GeneratorOptions
{
    public GeneratorMode Mode { get; init; } = GeneratorMode.Password;
    public int Length { get; init; } = 20;
    public bool Uppercase { get; init; } = true;
    public bool Lowercase { get; init; } = true;
    public bool Digits { get; init; } = true;
    public bool Symbols { get; init; } = true;
    public bool ExcludeAmbiguous { get; init; } = true;
    public int WordCount { get; init; } = 8;
    public string Separator { get; init; } = "-";
}
```

### `PasswordStrengthResult`

```csharp
public sealed record PasswordStrengthResult(
    int Score,
    string Label,
    IReadOnlyList<string> Suggestions);
```

Strength evaluation is guidance, not a proof of resistance against a particular attacker.

## `CipherNest.Application.Abstractions.ITotpService`

Local time-based one-time-password generation boundary. It is not a network/authentication-provider API.

```csharp
TotpCodeResult Generate(
    string base32Secret,
    TotpAlgorithm algorithm,
    int digits,
    int periodSeconds,
    DateTimeOffset utcNow);
```

`TotpCodeResult`:

```csharp
public sealed record TotpCodeResult(
    string Code,
    int SecondsRemaining,
    DateTimeOffset ValidUntilUtc);
```

The current `TotpService` validates bounded Base32 input/settings, supports SHA-1/SHA-256/SHA-512 with 6 or 8 digits and 15..120-second periods, computes codes locally, and zeroes decoded seed/hash/counter byte buffers where practical. Generated codes are not persisted by the service.

See `security/TOTP.md` for security/compatibility rules.

## `CipherNest.Application.Abstractions.ITotpUriCodec`

Local TOTP setup-URI interoperability boundary. This contract does not perform network/provider enrollment and intentionally supports TOTP only.

```csharp
TotpUriProfile Parse(string uriText);
string Format(TotpUriProfile profile);
```

`TotpUriProfile`:

```csharp
public sealed record TotpUriProfile(
    string AccountName,
    string Issuer,
    string Secret,
    TotpAlgorithm Algorithm,
    int Digits,
    int PeriodSeconds);
```

The current `TotpUriCodec` accepts bounded absolute `otpauth://totp/...` URIs, rejects HOTP/counter input, rejects duplicate query parameters, applies URI/query/display-metadata ceilings, validates issuer consistency, and routes imported seed/settings through `TotpPolicy`. `Format(...)` emits a canonical local TOTP setup URI. A setup URI contains the long-lived seed and must be handled as secret data.

Current URI-specific ceilings are documented in `LIMITS_AND_DEFAULTS.md`; threat and clipboard guidance is documented in `security/TOTP.md`.

## `CipherNest.Application.Abstractions.ISecurityAuditService`

```csharp
IReadOnlyList<SecurityAuditFinding> Analyze(
    IReadOnlyList<VaultItem> items,
    DateTimeOffset now);
```

The current implementation analyzes decrypted items locally. TOTP seeds are intentionally excluded from password weakness/reuse heuristics; exact duplicate detection still includes TOTP parameters. The returned findings are application findings, not the result of an independent source-code security audit.

## `CipherNest.Application.Abstractions.ISafeNoteMarkupService`

```csharp
SafeNotePreview Parse(string? markdown);
string AppendChecklistItem(string? markdown, string text);
string ToggleChecklistItem(string? markdown, int checklistIndex);
```

The service implements the bounded safe Markdown-like subset documented in `security/SECURE_NOTES.md`.

## `CipherNest.Application.Abstractions.IClock`

```csharp
DateTimeOffset UtcNow { get; }
```

The clock abstraction allows deterministic time-sensitive policies/tests without directly coupling those policies to `DateTimeOffset.UtcNow`.

## `CipherNest.Domain.Models.VaultItem`

```csharp
public sealed record VaultItem
{
    public Guid Id { get; init; }
    public VaultItemType Type { get; init; }
    public string Title { get; init; }
    public string Username { get; init; }
    public string Secret { get; init; }
    public string Url { get; init; }
    public string Notes { get; init; }
    public string Collection { get; init; }
    public IReadOnlyList<string> Tags { get; init; }
    public bool IsFavorite { get; init; }
    public IReadOnlyList<CustomField> CustomFields { get; init; }
    public IReadOnlyList<AttachmentReference> Attachments { get; init; }
    public TotpAlgorithm TotpAlgorithm { get; init; }
    public int TotpDigits { get; init; }
    public int TotpPeriodSeconds { get; init; }
    public DateTimeOffset CreatedUtc { get; init; }
    public DateTimeOffset ModifiedUtc { get; init; }
    public DateTimeOffset? LastAccessedUtc { get; init; }
    public DateTimeOffset? ReviewAfterUtc { get; init; }
    public DateTimeOffset? DeletedUtc { get; init; }
    public bool RequiresReauthentication { get; init; }
}
```

`Normalize(DateTimeOffset now)` trims `Title`, `Username`, `Url`, and `Collection`; trims/removes empty tags; de-duplicates/sorts tags case-insensitively; and sets `ModifiedUtc` to the supplied time.

For `OneTimePassword` items, `Secret` is the encrypted Base32 seed and the three TOTP settings select HMAC algorithm, decimal digit count, and period. Generated codes and imported setup-URI text are not `VaultItem` fields.

### `TotpAlgorithm`

```csharp
Sha1 = 0
Sha256 = 1
Sha512 = 2
```

### `VaultItemType`

Persisted numeric values are explicit because the current encrypted JSON serializer writes enum values numerically:

```csharp
Login = 0
SecureNote = 1
Identity = 2
PaymentCardReference = 3
WifiCredential = 4
SoftwareLicense = 5
ServerSshReference = 6
Document = 7
Custom = 8
OneTimePassword = 9
```

Do not renumber/reorder an existing persisted value without an explicit compatibility migration/version boundary.

## `CipherNest.Domain.Models.AppPreferences`

Current persisted preferences:

```text
Theme
Language
LockTimeoutSeconds
LockOnBackground
ClipboardClearSeconds
ScreenshotProtection
BiometricUnlockEnabled
ReducedMotion
LargerInterface
TrashRetentionDays
RequireMasterPassphraseAfterHours
BackupReminderDays
ReviewRemindersEnabled
ReviewReminderLeadDays
GeneratorPassphraseMode
GeneratorPasswordLength
GeneratorPassphraseWordCount
GeneratorUppercase
GeneratorLowercase
GeneratorDigits
GeneratorSymbols
GeneratorExcludeAmbiguous
LastSuccessfulBackupUtc
```

`Language` currently supports System, English, and Hindi. Hindi is a reviewed resource-backed catalog for migrated strings; it is not a claim that every UI literal is translated.

Defaults/bounds are documented in `LIMITS_AND_DEFAULTS.md`. Do not treat settings as secret storage.

## Version/storage constants

`CipherNest.Shared.AppConstants` currently defines:

```text
ProductName = CipherNest
Version = 0.1.0
DatabaseSchemaVersion = 1
CryptoFormatVersion = 1
DatabaseFileName = ciphernest.db
AttachmentDirectoryName = attachments
BackupExtension = .cnbak
```

It also centralizes repository/contact/support/watermark metadata used by the app.

`CipherNest.Shared.VaultStorageLimits` currently defines:

```text
MaximumVaultHeaderUtf8Bytes = 64 KiB
MaximumItemPlaintextJsonBytes = 16 MiB
MaximumStoredEnvelopeBytes = 24 MiB
MaximumItemCount = 100,000
MaximumAttachmentCountTotal = 10,000
MaximumStoredEnvelopeBytesTotal = 256 MiB
```

## Compatibility rule

This reference reflects current source. When a public contract, serialized model, schema/format version, or shared limit changes, update this document in the same change series and add compatibility/release tests before treating the new behavior as complete.
