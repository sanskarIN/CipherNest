using CipherNest.Domain.Models;

namespace CipherNest.Application.Abstractions;

public interface IVaultService
{
    bool IsUnlocked { get; }
    event EventHandler<bool>? LockStateChanged;
    Task<bool> HasVaultAsync(CancellationToken cancellationToken = default);
    Task<string?> CreateAsync(string masterPassphrase, bool createRecoveryKey = true, CancellationToken cancellationToken = default);
    Task UnlockAsync(string masterPassphraseOrRecoveryKey, CancellationToken cancellationToken = default);
    Task LockAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VaultItem>> GetItemsAsync(bool includeTrash = false, CancellationToken cancellationToken = default);
    Task<VaultItem?> GetItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveItemAsync(VaultItem item, CancellationToken cancellationToken = default);
    Task MoveToTrashAsync(Guid id, CancellationToken cancellationToken = default);
    Task RestoreFromTrashAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VaultItem>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
