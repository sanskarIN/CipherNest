from pathlib import Path

path = Path("src/CipherNest.Infrastructure/Persistence/SqliteVaultStore.cs")
text = path.read_text(encoding="utf-8")

old_using = "using CipherNest.Application.Abstractions;\nusing CipherNest.Shared;\n"
new_using = "using CipherNest.Application.Abstractions;\nusing CipherNest.Infrastructure.Services;\nusing CipherNest.Shared;\n"
if old_using not in text:
    raise RuntimeError("SqliteVaultStore using marker not found")
text = text.replace(old_using, new_using, 1)

old = '''        await using (var header = connection.CreateCommand())
        {
            header.CommandText = "SELECT length(CAST(HeaderJson AS BLOB)) FROM VaultHeader WHERE Id = 1;";
            var result = await header.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            if (result is null or DBNull) throw new InvalidDataException("Replacement vault database does not contain a vault header.");
            var headerBytes = Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
            if (headerBytes is < 1 or > VaultStorageLimits.MaximumVaultHeaderUtf8Bytes) throw new InvalidDataException("Replacement vault header exceeds the supported size limit.");
        }
'''
new = '''        await using (var header = connection.CreateCommand())
        {
            header.CommandText = "SELECT length(CAST(HeaderJson AS BLOB)), HeaderJson FROM VaultHeader WHERE Id = 1;";
            await using var reader = await header.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidDataException("Replacement vault database does not contain a vault header.");
            var headerBytes = reader.GetInt64(0);
            if (headerBytes is < 1 or > VaultStorageLimits.MaximumVaultHeaderUtf8Bytes)
                throw new InvalidDataException("Replacement vault header exceeds the supported size limit.");
            var headerJson = reader.GetString(1);
            if (Encoding.UTF8.GetByteCount(headerJson) != headerBytes)
                throw new InvalidDataException("Replacement vault header length is inconsistent.");
            VaultHeaderJsonPolicy.Validate(headerJson);
        }
'''
if old not in text:
    raise RuntimeError("Replacement vault-header validation block not found")
text = text.replace(old, new, 1)
path.write_text(text, encoding="utf-8")
