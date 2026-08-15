using System.Globalization;
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

    [Theory]
    [InlineData("Title,\0Secret")]
    [InlineData("Title,\tSecret")]
    [InlineData("Title,\u200BSecret")]
    [InlineData("Title,\u202ESecret")]
    [InlineData("\"Title\ncontinued\",Secret")]
    public async Task ReadHeaders_RejectsControlAndInvisibleFormattingCharacters(string csv)
    {
        var service = CreateService();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv), writable: false);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => service.ReadHeadersAsync(stream));

        Assert.Equal("CSV header contains an unsafe control or formatting character.", error.Message);
    }

    [Fact]
    public async Task ReadHeaders_RejectsHeaderNameBeyondDedicatedLimit()
    {
        var service = CreateService();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(new string('a', 257)), writable: false);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => service.ReadHeadersAsync(stream));

        Assert.Equal("CSV header contains an oversized column name.", error.Message);
    }

    [Fact]
    public async Task ReadHeaders_AcceptsHeaderNameAtDedicatedLimit()
    {
        var service = CreateService();
        var header = new string('a', 256);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(header), writable: false);

        var headers = await service.ReadHeadersAsync(stream);

        Assert.Equal([header], headers);
    }

    [Fact]
    public async Task ReadHeaders_DeterministicAdversarialCorpusStaysWithinPublicContract()
    {
        var service = CreateService();
        var random = new Random(0xC1F3);
        char[] alphabet = ['A', 'b', '0', ' ', ',', '"', '\r', '\n', '\t', '\0', '-', '_', 'é', '中', '\u200B', '\u202E'];
        var corpus = new List<string> { "Title,Secret", "\0" };

        for (var caseIndex = 0; caseIndex < 256; caseIndex++)
        {
            var length = random.Next(0, 129);
            var builder = new StringBuilder(length);
            for (var index = 0; index < length; index++) builder.Append(alphabet[random.Next(alphabet.Length)]);
            corpus.Add(builder.ToString());
        }

        var accepted = 0;
        var rejected = 0;
        foreach (var csv in corpus)
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv), writable: false);
            try
            {
                var headers = await service.ReadHeadersAsync(stream);
                accepted++;
                Assert.InRange(headers.Count, 1, 256);
                Assert.Equal(headers.Count, headers.Distinct(StringComparer.OrdinalIgnoreCase).Count());
                Assert.All(headers, static header =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(header));
                    Assert.InRange(header.Length, 1, 256);
                    Assert.False(header.Any(static ch => char.IsControl(ch) || char.GetUnicodeCategory(ch) == UnicodeCategory.Format));
                });
            }
            catch (InvalidDataException)
            {
                rejected++;
            }
        }

        Assert.True(accepted > 0);
        Assert.True(rejected > 0);
    }

    [Fact]
    public async Task ReadHeaders_RejectsTruncatedUtf8Sequence()
    {
        var service = CreateService();
        var malformed = new byte[] { (byte)'T', (byte)'i', (byte)'t', (byte)'l', (byte)'e', (byte)',', 0xC3 };
        await using var stream = new MemoryStream(malformed, writable: false);

        var error = await Assert.ThrowsAsync<InvalidDataException>(() => service.ReadHeadersAsync(stream));

        Assert.Equal("CSV contains invalid UTF-8 text.", error.Message);
    }

    [Fact]
    public async Task ReadHeaders_AcceptsOptionalUtf8Bom()
    {
        var service = CreateService();
        var content = Encoding.UTF8.GetBytes("Title,Secret\nExample,value");
        var bytes = Encoding.UTF8.GetPreamble().Concat(content).ToArray();
        await using var stream = new MemoryStream(bytes, writable: false);

        var headers = await service.ReadHeadersAsync(stream);

        Assert.Equal(["Title", "Secret"], headers);
    }

    [Fact]
    public async Task ReadHeaders_RejectsUtf16Input()
    {
        var service = CreateService();
        var content = Encoding.Unicode.GetBytes("Title,Secret\nExample,value");
        var bytes = Encoding.Unicode.GetPreamble().Concat(content).ToArray();
        await using var stream = new MemoryStream(bytes, writable: false);

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
    public async Task ReadHeaders_AcceptsEscapedQuoteInsideQuotedHeader()
    {
        var service = CreateService();
        const string csv = "\"Account \"\"name\"\"\",Secret\nExample,value";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv), writable: false);

        var headers = await service.ReadHeadersAsync(stream);

        Assert.Equal(["Account \"name\"", "Secret"], headers);
    }

    [Fact]
    public async Task ReadHeaders_RejectsMoreThanMaximumColumns()
    {
        var service = CreateService();
        var header = string.Join(',', Enumerable.Range(0, 257).Select(static index => $"C{index}"));
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(header), writable: false);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.ReadHeadersAsync(stream));
    }

    [Fact]
    public async Task ReadHeaders_RejectsAggregateRowBeyondSafetyBudget()
    {
        var service = CreateService();
        var first = new string('a', 1_000_000);
        var second = new string('b', 1_000_000);
        var header = $"{first},{second},x";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(header), writable: false);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.ReadHeadersAsync(stream));
    }

    [Fact]
    public async Task ReadHeaders_RejectsUnreadableStream()
    {
        var service = CreateService();
        await using var stream = new MemoryStream();
        stream.Close();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ReadHeadersAsync(stream));
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
