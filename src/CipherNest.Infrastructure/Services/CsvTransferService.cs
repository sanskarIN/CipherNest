using System.Text;
using CipherNest.Application.Abstractions;
using CipherNest.Application.Validation;
using CipherNest.Domain.Models;

namespace CipherNest.Infrastructure.Services;

public sealed class CsvTransferService : IPlaintextTransferService
{
    private const int MaxColumns = 256;
    private const int MaxRows = 100_000;
    private const int MaxFieldChars = 1_000_000;
    private const int MaxRowChars = 2_000_000;
    private readonly IVaultService _vault;
    private readonly IClock _clock;

    public CsvTransferService(IVaultService vault, IClock clock)
    {
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<IReadOnlyList<string>> ReadHeadersAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead) throw new ArgumentException("CSV source stream must be readable.", nameof(source));
        await using var parser = new CsvParser(source, 1);
        var row = await parser.ReadRowAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidDataException("CSV is empty.");
        ValidateHeader(row);
        return row;
    }

    public async Task<CsvImportResult> ImportCsvAsync(Stream source, CsvImportMapping mapping, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead) throw new ArgumentException("CSV source stream must be readable.", nameof(source));
        ArgumentNullException.ThrowIfNull(mapping);
        ArgumentException.ThrowIfNullOrWhiteSpace(mapping.Title);
        if (!_vault.IsUnlocked) throw new InvalidOperationException("Unlock the vault before importing.");
        await using var parser = new CsvParser(source, checked(MaxRows + 1));
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
            var logicalRowNumber = imported + skipped + 2;
            var title = Get(row, indexes, mapping.Title).Trim();
            if (title.Length == 0)
            {
                skipped++;
                AddWarning(warnings, logicalRowNumber, "title is empty.");
                continue;
            }
            var now = _clock.UtcNow;
            var item = new VaultItem
            {
                Id = Guid.NewGuid(),
                Title = title,
                Username = Get(row, indexes, mapping.Username),
                Secret = Get(row, indexes, mapping.Secret),
                Url = Get(row, indexes, mapping.Url),
                Notes = Get(row, indexes, mapping.Notes),
                Collection = Get(row, indexes, mapping.Collection),
                Tags = SplitTags(Get(row, indexes, mapping.Tags)),
                Type = ParseType(Get(row, indexes, mapping.Type)),
                CreatedUtc = now,
                ModifiedUtc = now
            };
            var validationErrors = VaultItemValidator.Validate(item);
            if (validationErrors.Count > 0)
            {
                skipped++;
                AddWarning(warnings, logicalRowNumber, validationErrors[0]);
                continue;
            }
            try
            {
                await _vault.SaveItemAsync(item, cancellationToken).ConfigureAwait(false);
                imported++;
            }
            catch (ArgumentException)
            {
                skipped++;
                AddWarning(warnings, logicalRowNumber, "item could not be saved because it failed a local validation rule.");
            }
        }
        return new CsvImportResult(imported, skipped, warnings);
    }

    public async Task ExportCsvAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException("CSV destination stream must be writable.", nameof(destination));
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

    private static void AddWarning(ICollection<string> warnings, int logicalRowNumber, string reason)
    {
        if (warnings.Count < 20) warnings.Add($"Skipped row {logicalRowNumber}: {reason}");
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

    private sealed class CsvParser : IAsyncDisposable
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private readonly StreamReader _reader;
        private readonly char[] _charBuffer = new char[1];
        private readonly int _maxRows;
        private int _rowsRead;
        private bool _finished;
        private bool _atStart = true;
        private int? _pendingChar;

        public CsvParser(Stream source, int maxRows)
        {
            _reader = new StreamReader(source, StrictUtf8, detectEncodingFromByteOrderMarks: false, 64 * 1024, leaveOpen: true);
            _maxRows = maxRows > 0 ? maxRows : throw new ArgumentOutOfRangeException(nameof(maxRows));
        }

        public async Task<IReadOnlyList<string>?> ReadRowAsync(CancellationToken cancellationToken)
        {
            if (_finished) return null;
            if (_rowsRead >= _maxRows)
            {
                var extra = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
                if (extra < 0)
                {
                    _finished = true;
                    return null;
                }
                throw new InvalidDataException($"CSV exceeds the {MaxRows:N0}-row safety limit.");
            }

            var fields = new List<string>();
            var field = new StringBuilder();
            var quoted = false;
            var quoteClosed = false;
            var atFieldStart = true;
            var rowCharacters = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
                if (read < 0)
                {
                    _finished = true;
                    if (quoted) throw new InvalidDataException("CSV ended inside a quoted field.");
                    if (fields.Count == 0 && field.Length == 0 && atFieldStart) return null;
                    AddField(fields, field);
                    _rowsRead++;
                    return fields;
                }
                var ch = (char)read;
                if (quoted)
                {
                    if (ch == '"')
                    {
                        var next = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
                        if (next == '"')
                        {
                            field.Append('"');
                            IncrementRowCharacters(ref rowCharacters);
                        }
                        else
                        {
                            quoted = false;
                            quoteClosed = true;
                            PushBack(next);
                        }
                    }
                    else
                    {
                        field.Append(ch);
                        IncrementRowCharacters(ref rowCharacters);
                    }
                }
                else if (quoteClosed)
                {
                    if (ch == ',') { AddField(fields, field); atFieldStart = true; quoteClosed = false; }
                    else if (ch == '\r' || ch == '\n')
                    {
                        await ConsumeOptionalLineFeedAsync(ch, cancellationToken).ConfigureAwait(false);
                        AddField(fields, field);
                        _rowsRead++;
                        return fields;
                    }
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
                    await ConsumeOptionalLineFeedAsync(ch, cancellationToken).ConfigureAwait(false);
                    AddField(fields, field);
                    _rowsRead++;
                    return fields;
                }
                else
                {
                    field.Append(ch);
                    IncrementRowCharacters(ref rowCharacters);
                    atFieldStart = false;
                }
                if (field.Length > MaxFieldChars) throw new InvalidDataException("CSV field exceeds the safety limit.");
            }
        }

        public ValueTask DisposeAsync()
        {
            _reader.Dispose();
            return ValueTask.CompletedTask;
        }

        private async Task ConsumeOptionalLineFeedAsync(char current, CancellationToken cancellationToken)
        {
            if (current != '\r') return;
            var next = await ReadCharAsync(cancellationToken).ConfigureAwait(false);
            if (next != '\n') PushBack(next);
        }

        private void PushBack(int value)
        {
            if (value < 0) return;
            if (_pendingChar is not null) throw new InvalidOperationException("CSV parser pushback state is already occupied.");
            _pendingChar = value;
        }

        private static void IncrementRowCharacters(ref int rowCharacters)
        {
            rowCharacters++;
            if (rowCharacters > MaxRowChars) throw new InvalidDataException("CSV row exceeds the aggregate character safety limit.");
        }

        private static void AddField(List<string> fields, StringBuilder field)
        {
            fields.Add(field.ToString());
            if (fields.Count > MaxColumns) throw new InvalidDataException("CSV contains too many columns.");
            field.Clear();
        }

        private async Task<int> ReadCharAsync(CancellationToken cancellationToken)
        {
            if (_pendingChar is int pending)
            {
                _pendingChar = null;
                return pending;
            }

            while (true)
            {
                try
                {
                    var count = await _reader.ReadAsync(_charBuffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
                    if (count == 0) return -1;
                    var value = _charBuffer[0];
                    if (_atStart)
                    {
                        _atStart = false;
                        if (value == '\uFEFF') continue;
                    }
                    return value;
                }
                catch (DecoderFallbackException ex)
                {
                    throw new InvalidDataException("CSV contains invalid UTF-8 text.", ex);
                }
            }
        }
    }
}
