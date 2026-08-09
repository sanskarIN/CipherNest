using System.Text;
using CipherNest.Infrastructure.Crypto;
using CipherNest.Infrastructure.Persistence;
using CipherNest.Infrastructure.Services;

namespace CipherNest.IntegrationTests;

public sealed class CsvParserRobustnessTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "CipherNestCsvRobustness", Guid.NewGuid().ToString("N"));

    public CsvParserRobustnessTests() => Directory.CreateDirectory(_directory);

    [Theory]
    [InlineData("\"")]
    [InlineData("\"unterminated,title")]
    [InlineData("Title,Title\nA,B")]
    [InlineData("Title,,Secret\nA,B,C")]
    [InlineData("\"Title\"garbage,Secret")]
    [InlineData("Title,\"Secret\" trailing")]
    public async Task ReadHeaders_RejectsMalformedOrUnsafeHeaderForms(string csv)
    {
        var service = CreateService();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv), writable: false);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.ReadHeadersAsync(stream));
    }

    [Fact]
    public async Task ReadHeaders_AcceptsQuotedHeaderWithEmbeddedComma()
    {
        var service = CreateService();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("\"Account, name\",Secret\nExample,value"), writable: false);

        var headers = await service.ReadHeadersAsync(stream);

        Assert.Equal(["Account, name", "Secret"], headers);
    }

    [Fact]
    public async Task ReadHeaders_RejectsMoreThanMaximumColumns()
    {
        var service = CreateService();
        var header = string.Join(',', Enumerable.Range(0, 257).Select(static index => $"C{index}"));
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(header), writable: false);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.ReadHeadersAsync(stream));
    }

    private CsvTransferService CreateService()
    {
        var store = new SqliteVaultStore(Path.Combine(_directory, $"{Guid.NewGuid():N}.db"));
        var vault = new VaultService(store, new CryptoService(), new SystemClock());
        return new CsvTransferService(vault, new SystemClock());
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true); }
        catch (IOException) { }
    }
}
