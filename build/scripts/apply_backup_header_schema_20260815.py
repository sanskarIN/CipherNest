from pathlib import Path

root = Path(__file__).resolve().parents[2]
path = root / "src/CipherNest.Infrastructure/Services/EncryptedBackupService.cs"
text = path.read_text(encoding="utf-8")

replacements = [
    (
        "            var headerJson = JsonSerializer.SerializeToUtf8Bytes(header);\n            await using var output = new FileStream(tempOutput, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);",
        "            var headerJson = JsonSerializer.SerializeToUtf8Bytes(header);\n            BackupHeaderJsonPolicy.Validate(headerJson);\n            await using var output = new FileStream(tempOutput, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);",
    ),
    (
        "                var headerLength = await ReadInt32Async(input, cancellationToken).ConfigureAwait(false);\n                if (headerLength is < 16 or > 16_384) throw new InvalidDataException(\"Invalid backup header size.\");\n                var headerJson = new byte[headerLength];",
        "                var headerLength = await ReadInt32Async(input, cancellationToken).ConfigureAwait(false);\n                BackupFormatPolicy.ValidateHeaderLength(headerLength);\n                var headerJson = new byte[headerLength];",
    ),
    (
        "                await ReadExactlyAsync(input, headerJson, cancellationToken).ConfigureAwait(false);\n                var header = JsonSerializer.Deserialize<BackupHeader>(headerJson) ?? throw new InvalidDataException(\"Invalid backup header.\");",
        "                await ReadExactlyAsync(input, headerJson, cancellationToken).ConfigureAwait(false);\n                BackupHeaderJsonPolicy.Validate(headerJson);\n                var header = JsonSerializer.Deserialize<BackupHeader>(headerJson) ?? throw new InvalidDataException(\"Invalid backup header.\");",
    ),
]

for old, new in replacements:
    if new in text:
        continue
    if old not in text:
        raise RuntimeError(f"Expected EncryptedBackupService text not found: {old!r}")
    text = text.replace(old, new, 1)

path.write_text(text.rstrip() + "\n", encoding="utf-8")

(root / ".github/workflows/backup-header-schema-2026-08-15.yml").unlink()
Path(__file__).unlink()
