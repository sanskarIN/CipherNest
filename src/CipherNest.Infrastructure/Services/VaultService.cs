using System.Security.Cryptography;
using System.Text.Json;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Exceptions;
using CipherNest.Application.Validation;
using CipherNest.Domain.Models;

namespace CipherNest.Infrastructure.Services;

public sealed class VaultService : IVaultService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IVaultStore _store;
    private readonly ICryptoService _crypto;
    private readonly IClock _clock;
    private readonly EncryptedAttachmentStore _attachments;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private byte[]? _dataKey;

    public VaultService(IVaultStore store, ICryptoService crypto, IClock clock)
    {
        _store = store; _crypto = crypto; _clock = clock;
        var root = Path.GetDirectoryName(store.DatabasePath) ?? throw new InvalidOperationException("Vault data directory is unavailable.");
        _attachments = new EncryptedAttachmentStore(Path.Combine(root, "attachments"), crypto);
    }

    public bool IsUnlocked => _dataKey is { Length: 32 };
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
                await _store.WriteHeaderAsync(JsonSerializer.Serialize(new VaultHeaderDocument(2, masterWrapped, recoveryWrapped, null), JsonOptions), cancellationToken).ConfigureAwait(false);
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
        var header = await ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
        byte[] key;
        try { key = _crypto.UnwrapKey(masterPassphraseOrRecoveryKey.AsSpan(), header.Master); }
        catch (VaultAuthenticationException) when (header.Recovery is not null) { key = _crypto.UnwrapKey(masterPassphraseOrRecoveryKey.AsSpan(), header.Recovery); }
        ReplaceDataKey(key); LockStateChanged?.Invoke(this, true);
    }

    public async Task UnlockWithSecondarySecretAsync(string secondarySecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secondarySecret);
        var header = await ReadHeaderAsync(cancellationToken).ConfigureAwait(false);
        if (header.Secondary is null) throw new VaultAuthenticationException();
        var key = _crypto.UnwrapKey(secondarySecret.AsSpan(), header.Secondary);
        ReplaceDataKey(key);
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
            return _dataKey is not null && CryptographicOperations.FixedTimeEquals(candidate, _dataKey);
        }
        catch (VaultAuthenticationException) { return false; }
        finally { if (candidate is not null) CryptographicOperations.ZeroMemory(candidate); }
    }

    public async Task EnableSecondaryUnlockAsync(string masterPassphrase, string secondarySecret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secondarySecret);
        if (secondarySecret.Length < 32) throw new ArgumentException("Secondary unlock secret is too short.", nameof(secondarySecret));
        if (!await ReauthenticateAsync(masterPassphrase, cancellationToken).ConfigureAwait(false)) throw new VaultAuthenticationException();
        var key = RequireKey();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var header = await ReadHeaderUnlockedAsync(cancellationToken).ConfigureAwait(false);
            var wrapped = _crypto.WrapKey(key, secondarySecret.AsSpan());
            await _store.WriteHeaderAsync(JsonSerializer.Serialize(header with { Version = 2, Secondary = wrapped }, JsonOptions), cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task DisableSecondaryUnlockAsync(string masterPassphrase, CancellationToken cancellationToken = default)
    {
        if (!await ReauthenticateAsync(masterPassphrase, cancellationToken).ConfigureAwait(false)) throw new VaultAuthenticationException();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var header = await ReadHeaderUnlockedAsync(cancellationToken).ConfigureAwait(false);
            await _store.WriteHeaderAsync(JsonSerializer.Serialize(header with { Version = 2, Secondary = null }, JsonOptions), cancellationToken).ConfigureAwait(false);
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
        var key = RequireKey();
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var header = await ReadHeaderUnlockedAsync(cancellationToken).ConfigureAwait(false);
            var newMaster = _crypto.WrapKey(key, newMasterPassphrase.AsSpan());
            await _store.WriteHeaderAsync(JsonSerializer.Serialize(header with { Master = newMaster }, JsonOptions), cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public async Task DeleteVaultAsync(string masterPassphrase, CancellationToken cancellationToken = default)
    {
        if (!await ReauthenticateAsync(masterPassphrase, cancellationToken).ConfigureAwait(false)) throw new VaultAuthenticationException();
        await LockAsync(cancellationToken).ConfigureAwait(false);
        var attachmentRoot = Path.Combine(Path.GetDirectoryName(_store.DatabasePath)!, "attachments");
        await _store.DeleteDatabaseAsync(cancellationToken).ConfigureAwait(false);
        if (Directory.Exists(attachmentRoot)) Directory.Delete(attachmentRoot, recursive: true);
    }

    public Task LockAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (_dataKey is not null) { CryptographicOperations.ZeroMemory(_dataKey); _dataKey = null; } LockStateChanged?.Invoke(this, false); return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<VaultItem>> GetItemsAsync(bool includeTrash = false, CancellationToken cancellationToken = default)
    {
        var key = RequireKey(); var stored = await _store.ReadAllItemsAsync(cancellationToken).ConfigureAwait(false); var result = new List<VaultItem>(stored.Count);
        foreach (var row in stored) { cancellationToken.ThrowIfCancellationRequested(); var item = DecryptItem(row, key); if (includeTrash || item.DeletedUtc is null) result.Add(item); }
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
        _ = RequireKey(); var item = await GetItemRequiredAsync(id, cancellationToken).ConfigureAwait(false); foreach (var attachment in item.Attachments) _attachments.Delete(attachment.EncryptedFileName); await _store.DeleteItemAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<VaultItem>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var items = await GetItemsAsync(false, cancellationToken).ConfigureAwait(false); if (string.IsNullOrWhiteSpace(query)) return items; var q = query.Trim();
        return items.Where(item => item.Title.Contains(q, StringComparison.CurrentCultureIgnoreCase) || item.Username.Contains(q, StringComparison.CurrentCultureIgnoreCase) || item.Url.Contains(q, StringComparison.OrdinalIgnoreCase) || item.Notes.Contains(q, StringComparison.CurrentCultureIgnoreCase) || item.Collection.Contains(q, StringComparison.CurrentCultureIgnoreCase) || item.Tags.Any(tag => tag.Contains(q, StringComparison.CurrentCultureIgnoreCase)) || item.CustomFields.Any(field => field.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase) || field.Value.Contains(q, StringComparison.CurrentCultureIgnoreCase))).ToArray();
    }

    public async Task<AttachmentReference> AddAttachmentAsync(Guid itemId, Stream source, string displayName, string mediaType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source); if (!source.CanRead) throw new ArgumentException("Attachment stream must be readable.", nameof(source)); var item = await GetItemRequiredAsync(itemId, cancellationToken).ConfigureAwait(false); if (item.Attachments.Count >= 25) throw new InvalidOperationException("An item can have at most 25 attachments.");
        displayName = Path.GetFileName(displayName?.Trim()); if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 240) throw new ArgumentException("Attachment name is invalid.", nameof(displayName)); mediaType = string.IsNullOrWhiteSpace(mediaType) ? "application/octet-stream" : mediaType.Trim();
        var attachmentId = Guid.NewGuid(); var opaque = _attachments.GetOpaqueFileName(attachmentId); var length = await _attachments.EncryptAsync(itemId, attachmentId, source, opaque, RequireKey(), cancellationToken).ConfigureAwait(false); var reference = new AttachmentReference(attachmentId, displayName, mediaType, length, opaque, _clock.UtcNow);
        try { await SaveItemAsync(item with { Attachments = item.Attachments.Append(reference).ToArray() }, cancellationToken).ConfigureAwait(false); return reference; } catch { _attachments.Delete(opaque); throw; }
    }

    public async Task RemoveAttachmentAsync(Guid itemId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var item = await GetItemRequiredAsync(itemId, cancellationToken).ConfigureAwait(false); var reference = item.Attachments.FirstOrDefault(a => a.Id == attachmentId) ?? throw new KeyNotFoundException("Attachment does not exist."); await SaveItemAsync(item with { Attachments = item.Attachments.Where(a => a.Id != attachmentId).ToArray() }, cancellationToken).ConfigureAwait(false); _attachments.Delete(reference.EncryptedFileName);
    }

    public async Task ExportAttachmentAsync(Guid itemId, Guid attachmentId, Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination); if (!destination.CanWrite) throw new ArgumentException("Destination stream must be writable.", nameof(destination)); var item = await GetItemRequiredAsync(itemId, cancellationToken).ConfigureAwait(false); var reference = item.Attachments.FirstOrDefault(a => a.Id == attachmentId) ?? throw new KeyNotFoundException("Attachment does not exist."); await _attachments.DecryptToAsync(itemId, attachmentId, reference.EncryptedFileName, reference.PlaintextLength, destination, RequireKey(), cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() { if (_dataKey is not null) CryptographicOperations.ZeroMemory(_dataKey); _dataKey = null; _gate.Dispose(); }
    private byte[] RequireKey() => _dataKey ?? throw new VaultLockedException();

    private async Task PersistItemAsync(VaultItem item, CancellationToken cancellationToken)
    {
        var key = RequireKey();
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(item, JsonOptions);
        try
        {
            var envelope = _crypto.Encrypt(plaintext, key, item.Id.ToByteArray());
            await _store.UpsertItemAsync(new StoredVaultItem(item.Id, JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions)), cancellationToken).ConfigureAwait(false);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private VaultItem DecryptItem(StoredVaultItem row, byte[] key)
    {
        var envelope = JsonSerializer.Deserialize<EncryptedEnvelope>(row.Envelope, JsonOptions) ?? throw new CryptographicException("Stored record envelope is invalid."); var plaintext = _crypto.Decrypt(envelope, key, row.Id.ToByteArray());
        try { return JsonSerializer.Deserialize<VaultItem>(plaintext, JsonOptions) ?? throw new CryptographicException("Stored record payload is invalid."); } finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    private async Task<VaultItem> GetItemRequiredAsync(Guid id, CancellationToken cancellationToken) => await GetItemAsync(id, cancellationToken).ConfigureAwait(false) ?? throw new KeyNotFoundException("The requested vault item does not exist.");

    private async Task<VaultHeaderDocument> ReadHeaderAsync(CancellationToken cancellationToken)
    {
        await _store.InitializeAsync(cancellationToken).ConfigureAwait(false); return await ReadHeaderUnlockedAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<VaultHeaderDocument> ReadHeaderUnlockedAsync(CancellationToken cancellationToken)
    {
        var headerJson = await _store.ReadHeaderAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("No local vault exists yet."); return JsonSerializer.Deserialize<VaultHeaderDocument>(headerJson, JsonOptions) ?? throw new VaultAuthenticationException();
    }

    private void ReplaceDataKey(byte[] next) { if (next.Length != 32) { CryptographicOperations.ZeroMemory(next); throw new CryptographicException("Invalid vault data key length."); } if (_dataKey is not null) CryptographicOperations.ZeroMemory(_dataKey); _dataKey = next; }
    private static string GenerateRecoveryKey() { var bytes = RandomNumberGenerator.GetBytes(32); try { return "CN1-" + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_'); } finally { CryptographicOperations.ZeroMemory(bytes); } }
    private sealed record VaultHeaderDocument(int Version, WrappedKeyEnvelope Master, WrappedKeyEnvelope? Recovery, WrappedKeyEnvelope? Secondary = null);
}
