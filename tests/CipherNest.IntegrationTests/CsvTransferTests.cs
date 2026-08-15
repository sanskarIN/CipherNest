using System.Text;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Validation;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class CsvTransferTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestCsvTests", Guid.NewGuid().ToString("N"));
    private VaultService _vault = null!;
    private CsvTransferService _transfer = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        var clock = new SystemClock();
        _vault = new VaultService(new SqliteVaultStore(Path.Combine(_directory, "vault.db")), new CryptoService(), clock);
        await _vault.CreateAsync("Very Strong Master Passphrase 2026!", false);
        _transfer = new CsvTransferService(_vault, clock);
    }

    public Task DisposeAsync()
    {
        _vault.Dispose();
        try { Directory.Delete(_directory, true); } catch (IOException) { }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Import_UsesExplicitMapping_AndHandlesQuotedFields()
    {
        const string csv = "Name,Login,Password,Notes\r\n\"Example, Inc\",alice,secret,\"line one\nline two\"\r\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var result = await _transfer.ImportCsvAsync(stream, new CsvImportMapping("Name", "Login", "Password", Notes: "Notes"));
        Assert.Equal(1, result.Imported);
        var item = Assert.Single(await _vault.GetItemsAsync());
        Assert.Equal("Example, Inc", item.Title);
        Assert.Equal("line one\nline two", item.Notes);
    }

    [Fact]
    public async Task Import_LeavesCallerOwnedSourceStreamOpen()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Title\nExample\n"));

        var result = await _transfer.ImportCsvAsync(stream, new CsvImportMapping("Title"));

        Assert.Equal(1, result.Imported);
        Assert.True(stream.CanRead);
        Assert.True(stream.CanSeek);
    }

    [Fact]
    public async Task Import_RejectsMissingRequiredTitleMapping()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Title\nExample\n"));

        await Assert.ThrowsAsync<ArgumentException>(() => _transfer.ImportCsvAsync(stream, new CsvImportMapping("   ")));
    }

    [Fact]
    public async Task Headers_RejectDuplicateNames()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Title,title\nA,B"));
        await Assert.ThrowsAsync<InvalidDataException>(() => _transfer.ReadHeadersAsync(stream));
    }

    [Fact]
    public async Task Parser_RejectsCharactersAfterClosingQuote()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Title\n\"abc\"x\n"));
        await Assert.ThrowsAsync<InvalidDataException>(() => _transfer.ImportCsvAsync(stream, new CsvImportMapping("Title")));
    }

    [Fact]
    public async Task Import_AcceptsExactlyMaximumSupportedTagCount()
    {
        var tags = string.Join(';', Enumerable.Range(0, VaultItemValidator.MaximumTags).Select(index => $"tag{index}"));
        var csv = $"Title,Tags\nExample,\"{tags}\"\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _transfer.ImportCsvAsync(stream, new CsvImportMapping("Title", Tags: "Tags"));

        Assert.Equal(1, result.Imported);
        Assert.Equal(0, result.Skipped);
        var item = Assert.Single(await _vault.GetItemsAsync());
        Assert.Equal(VaultItemValidator.MaximumTags, item.Tags.Count);
    }

    [Fact]
    public async Task Import_SkipsHostileHighCardinalityTagsWithoutSavingItem()
    {
        var tags = string.Join(';', Enumerable.Repeat("x", 10_000));
        var csv = $"Title,Tags\nExample,\"{tags}\"\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _transfer.ImportCsvAsync(stream, new CsvImportMapping("Title", Tags: "Tags"));

        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Skipped);
        Assert.Empty(await _vault.GetItemsAsync());
        Assert.Contains(result.Warnings, warning => warning.Contains("tag", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Import_SkipsOversizedTagBeforeItemConstruction()
    {
        var oversizedTag = new string('x', VaultItemValidator.MaximumTagCharacters + 1);
        var csv = $"Title,Tags\nExample,{oversizedTag}\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _transfer.ImportCsvAsync(stream, new CsvImportMapping("Title", Tags: "Tags"));

        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Skipped);
        Assert.Empty(await _vault.GetItemsAsync());
    }
}
