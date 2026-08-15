from pathlib import Path

path = Path("src/CipherNest.Infrastructure/Services/EncryptedBackupService.cs")
text = path.read_text(encoding="utf-8")

old = """        var hasDatabase = false;\n        var seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);\n        long total = 0;\n        foreach (var entry in archive.Entries)\n"""
new = """        var hasDatabase = false;\n        var seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);\n        long total = 0;\n        var copyBuffer = new byte[128 * 1024];\n        foreach (var entry in archive.Entries)\n"""
if text.count(old) != 1:
    raise SystemExit(f"expected one extraction prelude, found {text.count(old)}")
text = text.replace(old, new, 1)

old = """            total = BackupArchivePolicy.AddEntryLength(total, entry.Length);\n            var normalized = entry.FullName.Replace('\\\\', '/');\n"""
new = """            var normalized = entry.FullName.Replace('\\\\', '/');\n"""
if text.count(old) != 1:
    raise SystemExit(f"expected one metadata-only extraction budget, found {text.count(old)}")
text = text.replace(old, new, 1)

old = """            await using var source = entry.Open();\n            await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);\n            await source.CopyToAsync(output, 128 * 1024, cancellationToken).ConfigureAwait(false);\n"""
new = """            await using var source = entry.Open();\n            await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);\n            total = await BackupArchivePolicy.CopyEntryExactlyAsync(\n                source,\n                output,\n                entry.Length,\n                total,\n                copyBuffer,\n                cancellationToken).ConfigureAwait(false);\n"""
if text.count(old) != 1:
    raise SystemExit(f"expected one unbounded extraction copy, found {text.count(old)}")
text = text.replace(old, new, 1)

path.write_text(text, encoding="utf-8")
