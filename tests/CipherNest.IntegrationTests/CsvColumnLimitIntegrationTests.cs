using System.Text;
using CipherNest.Application.Abstractions;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class CsvColumnLimitIntegrationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestCsvColumnLimit", Guid.NewGuid().ToString("N"));

    public CsvColumnLimitIntegrationTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public async Task Import_RejectsDataRowWhoseFinalFieldExceedsColumnLimit()
    {
        var databasePath = Path.Combine(_directory, "vault.db");
        var store = new SqliteVaultStore(databasePath);
        using var vault = new VaultService(store, new CryptoService(), new SystemClock());
        await vault.CreateAsync("CSV Column Limit Master Passphrase 2026!", createRecoveryKey: false);
        var transfer = new CsvTransferService(vault, new SystemClock());
        var excessiveRow = string.Join(',', Enumerable.Repeat("value", 257));
        var csv = $"Title\n{excessiveRow}\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => transfer.ImportCsvAsync(stream, new CsvImportMapping("Title")));

        Assert.Contains("too many columns", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await vault.GetItemsAsync());
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
