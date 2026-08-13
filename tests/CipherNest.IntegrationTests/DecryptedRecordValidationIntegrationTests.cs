using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using CipherNest.Application.Abstractions;
using CipherNest.Domain.Models;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class DecryptedRecordValidationIntegrationTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestRecordValidation", Guid.NewGuid().ToString("N"));
    private string DatabasePath => Path.Combine(_directory, "vault.db");

    public DecryptedRecordValidationIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task AuthenticatedPayloadWithMismatchedItemId_IsRejected()
    {
        const string master = "Authenticated Record Identity Test 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        var crypto = new CryptoService();
        using var vault = new VaultService(store, crypto, new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);

        var rowId = Guid.NewGuid();
        var payload = new VaultItem { Id = Guid.NewGuid(), Title = "Synthetic mismatch" };
        await WriteAuthenticatedPayloadAsync(store, crypto, master, rowId, payload);

        await Assert.ThrowsAsync<CryptographicException>(() => vault.GetItemsAsync());
    }

    [Fact]
    public async Task AuthenticatedPayloadWithInvalidMetadata_IsRejected()
    {
        const string master = "Authenticated Record Metadata Test 2026!";
        var store = new SqliteVaultStore(DatabasePath);
        var crypto = new CryptoService();
        using var vault = new VaultService(store, crypto, new SystemClock());
        await vault.CreateAsync(master, createRecoveryKey: false);

        var rowId = Guid.NewGuid();
        var payload = new VaultItem { Id = rowId, Type = (VaultItemType)999, Title = "Synthetic invalid metadata" };
        await WriteAuthenticatedPayloadAsync(store, crypto, master, rowId, payload);

        await Assert.ThrowsAsync<CryptographicException>(() => vault.GetItemsAsync());
    }

    private static async Task WriteAuthenticatedPayloadAsync(SqliteVaultStore store, CryptoService crypto, string master, Guid rowId, VaultItem payload)
    {
        var headerJson = await store.ReadHeaderAsync() ?? throw new InvalidDataException("Synthetic test vault header is missing.");
        var masterNode = JsonNode.Parse(headerJson)?["master"] ?? throw new InvalidDataException("Synthetic test master wrapper is missing.");
        var masterEnvelope = masterNode.Deserialize<WrappedKeyEnvelope>(JsonOptions) ?? throw new InvalidDataException("Synthetic test master wrapper is invalid.");
        var dataKey = crypto.UnwrapKey(master.AsSpan(), masterEnvelope);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        try
        {
            var envelope = crypto.Encrypt(plaintext, dataKey, rowId.ToByteArray());
            var storedEnvelope = JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);
            await store.UpsertItemAsync(new StoredVaultItem(rowId, storedEnvelope));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(dataKey);
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
