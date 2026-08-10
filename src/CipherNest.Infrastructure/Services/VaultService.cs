using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Exceptions;
using CipherNest.Application.Validation;
using CipherNest.Domain.Models;
using CipherNest.Shared;

namespace CipherNest.Infrastructure.Services;

public sealed class VaultService : IVaultService, IDisposable
{
    private const int MinimumSupportedHeaderVersion = 1;
    private const int CurrentHeaderVersion = 2;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IVaultStore _store;
    private readonly ICryptoService _crypto;
    private readonly IClock _clock;
    private readonly EncryptedAttachmentStore _attachments;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _keySync = new();
    private byte[]? _dataKey;
    private CancellationTokenSource? _sessionCancellation;

    public VaultService(IVaultStore store, ICryptoService crypto, IClock clock)
    {
        _store = store; _crypto = crypto; _clock = clock;
        var root = Path.GetDirectoryName(store.DatabasePath) ?? throw new InvalidOperationException("Vault data directory is unavailable.");
        _attachments = new EncryptedAttachmentStore(Path.Combine(root, "attachments"), crypto);
    }

    public bool IsUnlocked
    {
        get
        {
            lock (_keySync) return _dataKey is { Length: 32 } && _sessionCancellation is not null;
        }
    }

    public event EventHandler<bool>? LockStateChanged;

    public async Task<bool> HasVaultAsync(CancellationToken cancellationToken = default)
    {
        await _store.InitializeAsync(cancellationToken).ConfigureAwait(false);
        return await _store.HasVaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> CreateAsync(string masterPassphrase, bool createRecoveryKey = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(masterPassphrase); string? recoveryKey = null;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _store.InitializeAsync(cancellationToken).ConfigureAwait(false);
            if (await _store.HasVaultAsync(cancellationToken).ConfigureAwait(false)) throw new InvalidOperationException("A vault already exists on this device.");
            var masterWrapped = _crypto.CreateWrappedKey(masterPassphrase.AsSpan());
            var dataKey = _crypto.UnwrapKey(masterPassphrase.AsSpan(), masterWrapped);
            try
            {
                WrappedKeyEnvelope? recoveryWrapped = null;
                if (createRecoveryKey) { recoveryKey = GenerateRecoveryKey(); recoveryWrapped = _crypto.WrapKey(dataKey, recoveryKey.AsSpan()); }
                await _store.WriteHeaderAsync(JsonSerializer.Serialize(new VaultHeaderDocument(CurrentHeaderVersion, masterWrapped, recoveryWrapped, null), JsonOptions), cancellationToken).ConfigureAwait(false);
                ReplaceDataKey(dataKey.ToArray());
            }
            finally { CryptographicOperations.ZeroMemory(dataKey); }
        }
        finally { _gate.Release(); }
        LockStateChanged?.Invoke(this, true); return recoveryKey;
    }

    public async Task UnlockAsync(string masterPassphraseOrRecoveryKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(masterPassphraseOrRecoveryKey);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var header = await ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
            byte[] key;
            try { key = _crypto.UnwrapKey(masterPassphraseOrRecoveryKey.AsSpan(), header.Master); }
            catch (VaultAuthenticationException) when (header.Recovery is not null) { key = _crypto.UnwrapKey(masterPassphraseOrRecoveryKey.AsSpan(), header.Recovery); }
            ReplaceDataKey(key);
        }
        finally { _gate.Release(); }
        LockStateChanged?.Invoke(this, true);
    }

    public async Task UnlockWithSecondarySecretAsync(string secondarySecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secondarySecret);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var header = await ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
            if (header.Secondary is null) throw new VaultAuthenticationException();
            var key = _crypto.UnwrapKey(secondarySecret.AsSpan(), header.Secondary);
            ReplaceDataKey(key);
        }
        finally { _gate.Release(); }
        LockStateChanged?.Invoke(this, true);
    }

    public async Task<bool> ReauthenticateAsync(string masterPassphrase, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(masterPassphrase)) return false;
        var header = await ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
        byte[]? candidate = null;
        try
        {
            candidate = _crypto.UnwrapKey(masterPassphrase.AsSpan(), header.Master);
            using var lease = AcquireKeyLease(cancellationToken);
            return CryptographicOperations.FixedTimeEquals(candidate, lease.Key);
        }
        catch (VaultAuthenticationException) { return false; }
        catch (VaultLockedException) { return false; }
        finally { if (candidate is not null) CryptographicOperations.ZeroMemory(candidate); }
    }

    public async Task EnableSecondaryUnlockAsync(string masterPassphrase, string secondarySecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secondarySecret);
        if (secondarySecret.Length < 32) throw new ArgumentException("Secondary unlock secret is too short.", nameof(secondarySecret));
        if (!await ReauthenticateAsync(masterPassphrase, cancellationToken).ConfigureAwait(false)) throw new VaultAuthenticationException();
        using var lease = AcquireKeyLease(cancellationToken);
        await _gate.WaitAsync(lease.Token).ConfigureAwait(false);
        try
        {
            var header = await ReadHeaderUnlockedAsync(lease.Token).ConfigureAwait(false);
            var wrapped = _crypto.WrapKey(lease.Key, secondarySecret.AsSpan());
            lease.Token.ThrowIfCancellationRequested();
            await _store.WriteHeaderAsync(JsonSerializer.Serialize(header with { Version = CurrentHeaderVersion, Secondary = wrapped }, JsonOptions), lease.Token).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task DisableSecondaryUnlockAsync(string masterPassphrase, CancellationToken cancellationToken = default)
    {
        if (!await ReauthenticateAsync(masterPassphrase, cancellationToken).ConfigureAwait(false)) throw new VaultAuthenticationException();
        using var lease = AcquireKeyLease(cancellationToken);
        await _gate.WaitAsync(lease.Token).ConfigureAwait(false);
        try
        {
            var header = await ReadHeaderUnlockedAsync(lease.Token).ConfigureAwait(false);
            await _store.WriteHeaderAsync(JsonSerializer.Serialize(header with { Version = CurrentHeaderVersion, Secondary = null }, JsonOptions), lease.Token).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> IsSecondaryUnlockConfiguredAsync(CancellationToken cancellationToken = default)
    {
        if (!await HasVaultAsync(cancellationToken).ConfigureAwait(false)) return false;
        var header = await ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
        return header.Secondary is not null;
    }

    public async Task ChangeMasterPassphraseAsync(string currentMasterPassphrase, string newMasterPassphrase, CancellationToken cancellationToken = default)
    {
        if (!await ReauthenticateAsync(currentMasterPassphrase, cancellationToken).ConfigureAwait(false)) throw new VaultAuthenticationException();
        if (newMasterPassphrase.Length < 12) throw new ArgumentException("The new master passphrase must contain at least 12 characters.", nameof(newMasterPassphrase));
        using var lease = AcquireKeyLease(cancellationToken);
        await _gate.WaitAsync(lease.Token).ConfigureAwait(false);
        try
        {
            var header = await ReadHeaderUnlockedAsync(lease.Token).ConfigureAwait(false);
            var newMaster = _crypto.WrapKey(lease.Key, newMasterPassphrase.AsSpan());
            lease.Token.ThrowIfCancellationRequested();
            await _store.WriteHeaderAsync(JsonSerializer.Serialize(header with { Master = newMaster }, JsonOptions), lease.Token).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteVaultAsync(string masterPassphrase, CancellationToken cancellationToken = default)
    {
        if (!await ReauthenticateAsync(masterPassphrase, cancellationToken).ConfigureAwait(false)) throw new VaultAuthenticationException();
        using var authorizationLease = AcquireKeyLease(cancellationToken);
        await _gate.WaitAsync(authorizationLease.Token).ConfigureAwait(false);
        var sessionCleared = false;
        try
        {
            authorizationLease.Token.ThrowIfCancellationRequested();
            ClearSessionKey();
            sessionCleared = true;
            var attachmentRoot = Path.Combine(Path.GetDirectoryName(_store.DatabasePath)!, "attachments");
            await _store.DeleteDatabaseAsync(CancellationToken.None).ConfigureAwait(false);
            if (Directory.Exists(attachmentRoot)) Directory.Delete(attachmentRoot, recursive: true);
        }
        finally
        {
            _gate.Release();
            if (sessionCleared) LockStateChanged?.Invoke(this, false);
        }
    }

    public async Task LockAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { ClearSessionKey(); }
        finally { _gate.Release(); }
        LockStateChanged?.Invoke(this, false);
    }

    public async Task<IReadOnlyList<VaultItem>> GetItemsAsync(bool includeTrash = false, CancellationToken cancellationToken = default)
    {
        using var lease = AcquireKeyLease(cancellationToken);
        var stored = await _store.ReadAllItemsAsync(lease.Token).ConfigureAwait(false);
        var result = new List<VaultItem>(stored.Count);
        foreach (var row in stored)
        {
            lease.Token.ThrowIfCancellationRequested();
            var item = DecryptItem(row, lease.Key);
            if (includeTrash || item.DeletedUtc is null) result.Add(item);
        }
        lease.Token.ThrowIfCancellationRequested();
        return result.OrderByDescending(static x => x.IsFavorite).ThenBy(static x => x.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    public async Task<VaultItem?> GetItemAsync(Guid id, CancellationToken cancellationToken = default) => (await GetItemsAsync(true, cancellationToken).ConfigureAwait(false)).FirstOrDefault(x => x.Id == id);

    public async Task SaveItemAsync(VaultItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item); var errors = VaultItemValidator.Validate(item); if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors), nameof(item));
        await PersistItemAsync(item.Normalize(_clock.UtcNow), cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkAccessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetItemRequiredAsync(id, cancellationToken).ConfigureAwait(false);
        await PersistItemAsync(item with { LastAccessedUtc = _clock.UtcNow }, cancellationToken).ConfigureAwait(false);
    }

    public async Task MoveToTrashAsync(Guid id, CancellationToken cancellationToken = default) { var item = await GetItemRequiredAsync(id, cancellationToken).ConfigureAwait(false); await SaveItemAsync(item with { DeletedUtc = _clock.UtcNow }, cancellationToken).ConfigureAwait(false); }
    public async Task RestoreFromTrashAsync(Guid id, CancellationToken cancellationToken = default) { var item = await GetItemRequiredAsync(id, cancellationToken).ConfigureAwait(false); await SaveItemAsync(item with { DeletedUtc = null }, cancellationToken).ConfigureAwait(false); }

    public async Task DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var lease = AcquireKeyLease(cancellationToken);
        var item = await GetItemRequiredAsync(id, lease.Token).ConfigureAwait(false);
        var attachmentFiles = item.Attachments.Select(static attachment => attachment.EncryptedFileName).ToArray();
        await _store.DeleteItemAsync(id, lease.Token).ConfigureAwait(false);
        foreach (var attachmentFile in attachmentFiles) TryDeleteAttachment(attachmentFile);
    }

    public async Task<IReadOnlyList<VaultItem>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var items = await GetItemsAsync(false, cancellationToken).ConfigureAwait(false); if (string.IsNullOrWhiteSpace(query)) return items; var q = query.Trim();
        return items.Where(item => item.Title.Contains(q, StringComparison.CurrentCultureIgnoreCase) || item.Username.Contains(q, StringComparison.CurrentCultureIgnoreCase) || item.Url.Contains(q, StringComparison.OrdinalIgnoreCase) || item.Notes.Contains(q, StringComparison.CurrentCultureIgnoreCase) || item.Collection.Contains(q, StringComparison.CurrentCultureIgnoreCase) || item.Tags.Any(tag => tag.Contains(q, StringComparison.CurrentCultureIgnoreCase)) || item.CustomFields.Any(field => field.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase) || field.Value.Contains(q, StringComparison.CurrentCultureIgnoreCase))).ToArray();
    }

    public async Task<AttachmentReference> AddAttachmentAsync(Guid itemId, Stream source, string displayName, string mediaType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source); if (!source.CanRead) throw new ArgumentException("Attachment stream must be readable.", nameof(source));
        var normalizedDisplayName = AttachmentImportPolicy.NormalizeDisplayName(displayName);
        var normalizedMediaType = AttachmentImportPolicy.NormalizeMediaType(mediaType);
        using var lease = AcquireKeyLease(cancellationToken);
        var item = await GetItemRequiredAsync(itemId, lease.Token).ConfigureAwait(false);
        if (item.Attachments.Count >= 25) throw new InvalidOperationException("An item can have at most 25 attachments.");
        var attachmentId = Guid.NewGuid(); var opaque = _attachments.GetOpaqueFileName(attachmentId); var length = await _attachments.EncryptAsync(itemId, attachmentId, source, opaque, lease.Key, lease.Token).ConfigureAwait(false); var reference = new AttachmentReference(attachmentId, normalizedDisplayName, normalizedMediaType, length, opaque, _clock.UtcNow);
        try { await SaveItemAsync(item with { Attachments = item.Attachments.Append(reference).ToArray() }, lease.Token).ConfigureAwait(false); return reference; } catch { TryDeleteAttachment(opaque); throw; }
    }

    public async Task RemoveAttachmentAsync(Guid itemId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        using var lease = AcquireKeyLease(cancellationToken);
        var item = await GetItemRequiredAsync(itemId, lease.Token).ConfigureAwait(false); var reference = item.Attachments.FirstOrDefault(a => a.Id == attachmentId) ?? throw new KeyNotFoundException("Attachment does not exist."); await SaveItemAsync(item with { Attachments = item.Attachments.Where(a => a.Id != attachmentId).ToArray() }, lease.Token).ConfigureAwait(false); TryDeleteAttachment(reference.EncryptedFileName);
    }

    public async Task ExportAttachmentAsync(Guid itemId, Guid attachmentId, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination); if (!destination.CanWrite) throw new ArgumentException("Destination stream must be writable.", nameof(destination));
        using var lease = AcquireKeyLease(cancellationToken);
        var item = await GetItemRequiredAsync(itemId, lease.Token).ConfigureAwait(false); var reference = item.Attachments.FirstOrDefault(a => a.Id == attachmentId) ?? throw new KeyNotFoundException("Attachment does not exist."); await _attachments.DecryptToAsync(itemId, attachmentId, reference.EncryptedFileName, reference.PlaintextLength, destination, lease.Key, lease.Token).ConfigureAwait(false);
    }

    public void Dispose()
    {
        ClearSessionKey();
        _gate.Dispose();
    }

    private VaultKeyLease AcquireKeyLease(CancellationToken cancellationToken)
    {
        lock (_keySync)
        {
            if (_dataKey is not { Length: 32 } || _sessionCancellation is null) throw new VaultLockedException();
            return new VaultKeyLease(_dataKey.ToArray(), _sessionCancellation.Token, cancellationToken);
        }
    }

    private async Task PersistItemAsync(VaultItem item, CancellationToken cancellationToken)
    {
        using var lease = AcquireKeyLease(cancellationToken);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(item, JsonOptions);
        try
        {
            if (plaintext.Length > VaultStorageLimits.MaximumItemPlaintextJsonBytes) throw new InvalidOperationException("Vault item exceeds the supported serialized size limit.");
            lease.Token.ThrowIfCancellationRequested();
            var envelope = _crypto.Encrypt(plaintext, lease.Key, item.Id.ToByteArray());
            lease.Token.ThrowIfCancellationRequested();
            var storedEnvelope = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
            if (storedEnvelope.Length > VaultStorageLimits.MaximumStoredEnvelopeBytes) throw new InvalidOperationException("Encrypted vault item exceeds the supported storage size limit.");
            await _store.UpsertItemAsync(new StoredVaultItem(item.Id, storedEnvelope), lease.Token).ConfigureAwait(false);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private VaultItem DecryptItem(StoredVaultItem row, byte[] key)
    {
        if (row.Envelope is null || row.Envelope.Length is < 1 or > VaultStorageLimits.MaximumStoredEnvelopeBytes)
            throw new CryptographicException("Stored record envelope size is invalid.");
        var envelope = JsonSerializer.Deserialize<EncryptedEnvelope>(row.Envelope, JsonOptions) ?? throw new CryptographicException("Stored record envelope is invalid.");
        var plaintext = _crypto.Decrypt(envelope, key, row.Id.ToByteArray());
        try
        {
            if (plaintext.Length > VaultStorageLimits.MaximumItemPlaintextJsonBytes) throw new CryptographicException("Stored record payload exceeds the supported size limit.");
            var item = JsonSerializer.Deserialize<VaultItem>(plaintext, JsonOptions) ?? throw new CryptographicException("Stored record payload is invalid.");
            if (item.Id != row.Id) throw new CryptographicException("Stored record identifier does not match its authenticated database key.");
            var errors = VaultItemValidator.Validate(item);
            if (errors.Count > 0) throw new CryptographicException("Stored record payload contains invalid metadata.");
            return item;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private async Task<VaultItem> GetItemRequiredAsync(Guid id, CancellationToken cancellationToken) => await GetItemAsync(id, cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException("The requested vault item does not exist.");

    private async Task<VaultHeaderDocument> ReadHeaderAsync(CancellationToken cancellationToken)
    {
        await _store.InitializeAsync(cancellationToken).ConfigureAwait(false); return await ReadHeaderUnlockedAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<VaultHeaderDocument> ReadHeaderUnlockedAsync(CancellationToken cancellationToken)
    {
        var headerJson = await _store.ReadHeaderAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("No local vault exists yet.");
        if (Encoding.UTF8.GetByteCount(headerJson) is < 1 or > VaultStorageLimits.MaximumVaultHeaderUtf8Bytes) throw new VaultAuthenticationException();
        var header = JsonSerializer.Deserialize<VaultHeaderDocument>(headerJson, JsonOptions) ?? throw new VaultAuthenticationException();
        if (header.Version is < MinimumSupportedHeaderVersion or > CurrentHeaderVersion || header.Master is null) throw new VaultAuthenticationException();
        return header;
    }

    private void TryDeleteAttachment(string encryptedFileName)
    {
        try { _attachments.Delete(encryptedFileName); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (InvalidDataException) { }
    }

    private void ClearSessionKey()
    {
        CancellationTokenSource? session;
        lock (_keySync)
        {
            session = _sessionCancellation;
            _sessionCancellation = null;
            if (_dataKey is not null) CryptographicOperations.ZeroMemory(_dataKey);
            _dataKey = null;
        }
        CancelAndDisposeSession(session);
    }

    private void ReplaceDataKey(byte[] next)
    {
        if (next.Length != 32) { CryptographicOperations.ZeroMemory(next); throw new CryptographicException("Invalid vault data key length."); }
        CancellationTokenSource? previousSession;
        lock (_keySync)
        {
            previousSession = _sessionCancellation;
            _sessionCancellation = new CancellationTokenSource();
            if (_dataKey is not null) CryptographicOperations.ZeroMemory(_dataKey);
            _dataKey = next;
        }
        CancelAndDisposeSession(previousSession);
    }

    private static void CancelAndDisposeSession(CancellationTokenSource? session)
    {
        if (session is null) return;
        try { session.Cancel(); }
        catch (AggregateException)
        {
            // Session-key state has already transitioned. Cancellation callback failures must not reverse or mask that transition.
        }
        finally { session.Dispose(); }
    }

    private static string GenerateRecoveryKey() { var bytes = RandomNumberGenerator.GetBytes(32); try { return "CN1-" + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_'); } finally { CryptographicOperations.ZeroMemory(bytes); } }
    private sealed record VaultHeaderDocument(int Version, WrappedKeyEnvelope Master, WrappedKeyEnvelope? Recovery, WrappedKeyEnvelope? Secondary = null);
}
