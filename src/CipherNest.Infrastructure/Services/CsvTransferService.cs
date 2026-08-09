using System.Text;
using CipherNest.Application.Abstractions;
using CipherNest.Domain.Models;

namespace CipherNest.Infrastructure.Services;

public sealed class CsvTransferService : IPlaintextTransferService
{
    private const int MaxColumns = 256;
    private const int MaxRows = 100_000;
    private const int MaxFieldChars = 1_000_000;
    private readonly IVaultService _vault;
    private readonly IClock _clock;

    public CsvTransferService(IVaultService vault, IClock clock)
    {
        _vault = vault;
        _clock = clock;
    }

    public async Task<IReadOnlyList<string>> ReadHeadersAsync(Stream source, CancellationToken cancellationToken = default)
    {
        var parser = new CsvParser(source);
        var row = await parser.ReadRowAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidDataException("CSV is empty.");
        ValidateHeader(row);
        return row;
    }

    public async Task<CsvImportResult> ImportCsvAsync(Stream source, CsvImportMapping mapping, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        if (!_vault.IsUnlocked) throw new InvalidOperationException("Unlock the vault before importing.");
        var parser = new CsvParser(source);
        var headers = await parser.ReadRowAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidDataException("CSV is empty.");
        ValidateHeader(headers);
        var indexes = headers.Select((name, index) => (name, index)).ToDictionary(static x => x.name, static x => x.index, StringComparer.OrdinalIgnoreCase);
        if (!indexes.ContainsKey(mapping.Title)) throw new InvalidDataException("The mapped title column does not exist.");
        ValidateMappedColumns(mapping, indexes);

        var imported = 0;
        var skipped = 0;
        var warnings = new List<string>();
        IReadOnlyList<string>? row;
        while ((row = await parser.ReadRowAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (imported + skipped >= MaxRows) throw new InvalidDataException($"CSV exceeds the {MaxRows:N0}-row safety limit.");
            var logicalRowNumber = imported + skipped + 2;
            var title = Get(row, indexes, mapping.Title).Trim();
            if (title.Length == 0)
            {
                skipped++;
                if (warnings.Count < 20) warnings.Add($"Skipped row {logicalRowNumber}: title is empty.");
                continue;
            }
            var now = _clock.UtcNow;
            var item = new VaultItem
            {
                Id = Guid.NewGuid(), Title = title,
                Username = Get(row, indexes, mapping.Username), Secret = Get(row, indexes, mapping.Secret), Url = Get(row, indexes, mapping.Url), Notes = Get(row, indexes, mapping.Notes), Collection = Get(row, indexes, mapping.Collection),
                Tags = SplitTags(Get(row, indexes, mapping.Tags)), Type = ParseType(Get(row, indexes, mapping.Type)), CreatedUtc = now, ModifiedUtc = now
            };
            try { await _vault.SaveItemAsync(item, cancellationToken).ConfigureAwait(false); imported++; }
            catch (ArgumentException ex) { skipped++; if (warnings.Count < 20) warnings.Add($"Skipped row {logicalRowNumber}: {ex.Message}"); }
        }
        return new CsvImportResult(imported, skipped, warnings);
    }

    public async Task ExportCsvAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        if (!_vault.IsUnlocked) throw new InvalidOperationException("Unlock the vault before exporting.");
        var items = await _vault.GetItemsAsync(includeTrash: false, cancellationToken).ConfigureAwait(false);
        await using var writer = new StreamWriter(destination, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 64 * 1024, leaveOpen: true);
        await writer.WriteLineAsync("Title,Type,Username,Secret,URL,Notes,Tags,Collection,Favorite,ReviewAfterUtc").ConfigureAwait(false);
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fields = new[] { item.Title, item.Type.ToString(), item.Username, item.Secret, item.Url, item.Notes, string.Join(';', item.Tags), item.Collection, item.IsFavorite ? "true" : "false", item.ReviewAfterUtc?.ToString("O") ?? string.Empty };
            await writer.WriteLineAsync(string.Join(',', fields.Select(Escape))).ConfigureAwait(false);
        }
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateHeader(IReadOnlyList<string> row)
    {
        if (row.Count is 0 or > MaxColumns) throw new InvalidDataException("CSV header has an unsupported number of columns.");
        if (row.Any(static h => string.IsNullOrWhiteSpace(h))) throw new InvalidDataException("CSV header contains an empty column name.");
        if (row.Distinct(StringComparer.OrdinalIgnoreCase).Count() != row.Count) throw new InvalidDataException("CSV header contains duplicate column names.");
    }

    private static void ValidateMappedColumns(CsvImportMapping mapping, IReadOnlyDictionary<string, int> indexes)
    {
        foreach (var name in new[] { mapping.Username, mapping.Secret, mapping.Url, mapping.Notes, mapping.Tags, mapping.Collection, mapping.Type })
            if (!string.IsNullOrEmpty(name) && !indexes.ContainsKey(name)) throw new InvalidDataException($"Mapped column '{name}' does not exist.");
    }

    private static string Get(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> indexes, string? name) => string.IsNullOrEmpty(name) || !indexes.TryGetValue(name, out var index) || index >= row.Count ? string.Empty : row[index];
    private static IReadOnlyList<string> SplitTags(string value) => value.Split(new[] { ';', ',' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    private static VaultItemType ParseType(string value) => Enum.TryParse<VaultItemType>(value, true, out var parsed) ? parsed : VaultItemType.Login;
    private static string Escape(string value) => value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private sealed class CsvParser
    {
        private readonly StreamReader _reader;
        private bool _finished;

        public CsvParser(Stream source) => _reader = new StreamReader(source, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 64 * 1024, leaveOpen: true);

        public async Task<IReadOnlyList<string>?> ReadRowAsync(CancellationToken cancellationToken)
        {
            if (_finished) return null;
            var fields = new List<string>();
            var field = new StringBuilder();
            var quoted = false;
            var quoteClosed = false;
            var atFieldStart = true;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
                if (read < 0)
                {
                    _finished = true;
                    if (quoted) throw new InvalidDataException("CSV ended inside a quoted field.");
                    if (fields.Count == 0 && field.Length == 0 && atFieldStart) return null;
                    fields.Add(field.ToString());
                    return fields;
                }
                var ch = (char)read;
                if (quoted)
                {
                    if (ch == '"')
                    {
                        var next = _reader.Peek();
                        if (next == '"') { _ = _reader.Read(); field.Append('"'); }
                        else { quoted = false; quoteClosed = true; }
                    }
                    else field.Append(ch);
                }
                else if (quoteClosed)
                {
                    if (ch == ',') { AddField(fields, field); atFieldStart = true; quoteClosed = false; }
                    else if (ch == '\r' || ch == '\n') { if (ch == '\r' && _reader.Peek() == '\n') _ = _reader.Read(); fields.Add(field.ToString()); return fields; }
                    else throw new InvalidDataException("Characters after a closing CSV quote are not allowed before the delimiter.");
                }
                else if (atFieldStart && ch == '"')
                {
                    quoted = true;
                    atFieldStart = false;
                }
                else if (ch == ',')
                {
                    AddField(fields, field);
                    atFieldStart = true;
                }
                else if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && _reader.Peek() == '\n') _ = _reader.Read();
                    fields.Add(field.ToString());
                    return fields;
                }
                else
                {
                    field.Append(ch);
                    atFieldStart = false;
                }
                if (field.Length > MaxFieldChars) throw new InvalidDataException("CSV field exceeds the safety limit.");
            }
        }

        private static void AddField(List<string> fields, StringBuilder field)
        {
            fields.Add(field.ToString());
            if (fields.Count > MaxColumns) throw new InvalidDataException("CSV contains too many columns.");
            field.Clear();
        }

        private async Task<int> ReadCharAsync(CancellationToken cancellationToken)
        {
            var chars = new char[1];
            var count = await _reader.ReadAsync(chars.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
            return count == 0 ? -1 : chars[0];
        }
    }
}
