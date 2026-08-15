from pathlib import Path

path = Path("src/CipherNest.Infrastructure/Services/VaultService.cs")
text = path.read_text(encoding="utf-8")

replacements = [
    ("using System.Text;\n", ""),
    (
        "    private const int MinimumSupportedHeaderVersion = 1;\n    private const int CurrentHeaderVersion = 2;\n",
        "",
    ),
    (
        "await _store.WriteHeaderAsync(JsonSerializer.Serialize(new VaultHeaderDocument(CurrentHeaderVersion, masterWrapped, recoveryWrapped, null), JsonOptions), cancellationToken).ConfigureAwait(false);",
        "await _store.WriteHeaderAsync(SerializeHeader(new VaultHeaderDocument(VaultHeaderJsonPolicy.CurrentVersion, masterWrapped, recoveryWrapped, null)), cancellationToken).ConfigureAwait(false);",
    ),
    (
        "await _store.WriteHeaderAsync(JsonSerializer.Serialize(header with { Version = CurrentHeaderVersion, Secondary = wrapped }, JsonOptions), authorizationLease.Token).ConfigureAwait(false);",
        "await _store.WriteHeaderAsync(SerializeHeader(header with { Version = VaultHeaderJsonPolicy.CurrentVersion, Secondary = wrapped }), authorizationLease.Token).ConfigureAwait(false);",
    ),
    (
        "await _store.WriteHeaderAsync(JsonSerializer.Serialize(header with { Version = CurrentHeaderVersion, Secondary = null }, JsonOptions), authorizationLease.Token).ConfigureAwait(false);",
        "await _store.WriteHeaderAsync(SerializeHeader(header with { Version = VaultHeaderJsonPolicy.CurrentVersion, Secondary = null }), authorizationLease.Token).ConfigureAwait(false);",
    ),
    (
        "await _store.WriteHeaderAsync(JsonSerializer.Serialize(header with { Master = newMaster }, JsonOptions), authorizationLease.Token).ConfigureAwait(false);",
        "await _store.WriteHeaderAsync(SerializeHeader(header with { Version = VaultHeaderJsonPolicy.CurrentVersion, Master = newMaster }), authorizationLease.Token).ConfigureAwait(false);",
    ),
    (
        '''    private async Task<VaultHeaderDocument> ReadHeaderUnlockedAsync(CancellationToken cancellationToken)\n    {\n        var headerJson = await _store.ReadHeaderAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("No local vault exists yet.");\n        if (Encoding.UTF8.GetByteCount(headerJson) is < 1 or > VaultStorageLimits.MaximumVaultHeaderUtf8Bytes) throw new VaultAuthenticationException();\n        VaultHeaderDocument header;\n        try\n        {\n            header = JsonSerializer.Deserialize<VaultHeaderDocument>(headerJson, JsonOptions) ?? throw new VaultAuthenticationException();\n        }\n        catch (JsonException ex)\n        {\n            throw new VaultAuthenticationException(ex);\n        }\n        if (header.Version is < MinimumSupportedHeaderVersion or > CurrentHeaderVersion || header.Master is null) throw new VaultAuthenticationException();\n        return header;\n    }\n''',
        '''    private async Task<VaultHeaderDocument> ReadHeaderUnlockedAsync(CancellationToken cancellationToken)\n    {\n        try\n        {\n            var headerJson = await _store.ReadHeaderAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("No local vault exists yet.");\n            VaultHeaderJsonPolicy.Validate(headerJson);\n            var header = JsonSerializer.Deserialize<VaultHeaderDocument>(headerJson, JsonOptions) ?? throw new VaultAuthenticationException();\n            if (header.Version is < VaultHeaderJsonPolicy.MinimumSupportedVersion or > VaultHeaderJsonPolicy.CurrentVersion || header.Master is null)\n                throw new VaultAuthenticationException();\n            return header;\n        }\n        catch (VaultAuthenticationException)\n        {\n            throw;\n        }\n        catch (Exception ex) when (ex is JsonException or InvalidDataException)\n        {\n            throw new VaultAuthenticationException(ex);\n        }\n    }\n\n    private static string SerializeHeader(VaultHeaderDocument header)\n    {\n        var headerJson = JsonSerializer.Serialize(header, JsonOptions);\n        VaultHeaderJsonPolicy.Validate(headerJson);\n        return headerJson;\n    }\n''',
    ),
]

for old, new in replacements:
    if old not in text:
        raise RuntimeError(f"Expected VaultService marker not found: {old[:120]!r}")
    text = text.replace(old, new, 1)

path.write_text(text, encoding="utf-8")
