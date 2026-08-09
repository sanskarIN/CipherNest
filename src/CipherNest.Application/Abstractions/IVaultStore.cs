namespace CipherNest.Application.Abstractions;

public sealed record StoredVaultItem(Guid Id, byte[] Envelope);

public interface IVaultStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<bool> HasVaultAsync(CancellationToken cancellationToken = default);
    Task<string?> ReadHeaderAsync(CancellationToken cancellationToken = default);
    Task WriteHeaderAsync(string headerJson, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredVaultItem>> ReadAllItemsAsync(CancellationToken cancellationToken = default);
    Task UpsertItemAsync(StoredVaultItem item, CancellationToken cancellationToken = default);
    Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateConsistentSnapshotAsync(string destinationDatabasePath, CancellationToken cancellationToken = default);
    Task ReplaceDatabaseAsync(string sourceDatabasePath, CancellationToken cancellationToken = default);
    Task DeleteDatabaseAsync(CancellationToken cancellationToken = default);
    string DatabasePath { get; }
}
