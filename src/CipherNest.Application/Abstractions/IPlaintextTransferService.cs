namespace CipherNest.Application.Abstractions;

public sealed record CsvImportMapping(
    string Title,
    string? Username = null,
    string? Secret = null,
    string? Url = null,
    string? Notes = null,
    string? Tags = null,
    string? Collection = null,
    string? Type = null);

public sealed record CsvImportResult(int Imported, int Skipped, IReadOnlyList<string> Warnings);

public interface IPlaintextTransferService
{
    Task<IReadOnlyList<string>> ReadHeadersAsync(Stream source, CancellationToken cancellationToken = default);
    Task<CsvImportResult> ImportCsvAsync(Stream source, CsvImportMapping mapping, CancellationToken cancellationToken = default);
    Task ExportCsvAsync(Stream destination, CancellationToken cancellationToken = default);
}
