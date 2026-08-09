using CipherNest.Domain.Models;

namespace CipherNest.Application.Abstractions;

public interface IVaultService
{
    bool IsUnlocked { get; }
    event EventHandler<bool>? LockStateChanged;
    Task<bool> HasVaultAsync(CancellationToken cancellationToken = default);
    Task<string?> CreateAsync(string masterPassphrase, bool createRecoveryKey = true, CancellationToken cancellationToken = default);
    Task UnlockAsync(string masterPassphraseOrRecoveryKey, CancellationToken cancellationToken = default);
    Task UnlockWithSecondarySecretAsync(string secondarySecret, CancellationToken cancellationToken = default);
    Task<bool> ReauthenticateAsync(string masterPassphrase, CancellationToken cancellationToken = default);
    Task EnableSecondaryUnlockAsync(string masterPassphrase, string secondarySecret, CancellationToken cancellationToken = default);
    Task DisableSecondaryUnlockAsync(string masterPassphrase, CancellationToken cancellationToken = default);
    Task<bool> IsSecondaryUnlockConfiguredAsync(CancellationToken cancellationToken = default);
    Task ChangeMasterPassphraseAsync(string currentMasterPassphrase, string newMasterPassphrase, CancellationToken cancellationToken = default);
    Task DeleteVaultAsync(string masterPassphrase, CancellationToken cancellationToken = default);
    Task LockAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VaultItem>> GetItemsAsync(bool includeTrash = false, CancellationToken cancellationToken = default);
    Task<VaultItem?> GetItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveItemAsync(VaultItem item, CancellationToken cancellationToken = default);
    Task MarkAccessedAsync(Guid id, CancellationToken cancellationToken = default);
    Task MoveToTrashAsync(Guid id, CancellationToken cancellationToken = default);
    Task RestoreFromTrashAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VaultItem>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<AttachmentReference> AddAttachmentAsync(Guid itemId, Stream source, string displayName, string mediaType, CancellationToken cancellationToken = default);
    Task RemoveAttachmentAsync(Guid itemId, Guid attachmentId, CancellationToken cancellationToken = default);
    Task ExportAttachmentAsync(Guid itemId, Guid attachmentId, Stream destination, CancellationToken cancellationToken = default);
}
