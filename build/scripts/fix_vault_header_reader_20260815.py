from pathlib import Path

path = Path("src/CipherNest.Infrastructure/Persistence/SqliteVaultStore.cs")
text = path.read_text(encoding="utf-8")
old = '''            await using var reader = await header.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidDataException("Replacement vault database does not contain a vault header.");
            var headerBytes = reader.GetInt64(0);
            if (headerBytes is < 1 or > VaultStorageLimits.MaximumVaultHeaderUtf8Bytes)
                throw new InvalidDataException("Replacement vault header exceeds the supported size limit.");
            var headerJson = reader.GetString(1);
'''
new = '''            await using var headerReader = await header.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await headerReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidDataException("Replacement vault database does not contain a vault header.");
            var headerBytes = headerReader.GetInt64(0);
            if (headerBytes is < 1 or > VaultStorageLimits.MaximumVaultHeaderUtf8Bytes)
                throw new InvalidDataException("Replacement vault header exceeds the supported size limit.");
            var headerJson = headerReader.GetString(1);
'''
if old not in text:
    raise RuntimeError("Expected replacement-header reader block was not found")
path.write_text(text.replace(old, new, 1), encoding="utf-8")
