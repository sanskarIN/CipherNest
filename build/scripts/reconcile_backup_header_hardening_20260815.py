from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    (ROOT / path).write_text(text.rstrip() + "\n", encoding="utf-8")


def replace_once(path: str, old: str, new: str) -> None:
    text = read(path)
    if new in text:
        return
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old!r}")
    write(path, text.replace(old, new, 1))


def insert_after(path: str, marker: str, addition: str) -> None:
    text = read(path)
    if addition.strip() in text:
        return
    if marker not in text:
        raise RuntimeError(f"Marker not found in {path}: {marker!r}")
    write(path, text.replace(marker, marker + addition, 1))


replace_once(
    "tests/CipherNest.UiTests/BackupFormatSourceTests.cs",
    "        var schema = source.IndexOf(\"BackupHeaderJsonPolicy.Validate(headerJson)\", StringComparison.Ordinal);\n"
    "        var deserialize = source.IndexOf(\"JsonSerializer.Deserialize<BackupHeader>\", schema, StringComparison.Ordinal);\n"
    "        var validate = source.IndexOf(\"BackupFormatPolicy.ValidateHeader\", deserialize, StringComparison.Ordinal);\n"
    "        var derive = source.IndexOf(\"key = _crypto.DeriveKey\", validate, StringComparison.Ordinal);\n\n"
    "        Assert.True(schema >= 0);",
    "        var restore = source.IndexOf(\"public async Task RestoreEncryptedAsync\", StringComparison.Ordinal);\n"
    "        var schema = source.IndexOf(\"BackupHeaderJsonPolicy.Validate(headerJson)\", restore, StringComparison.Ordinal);\n"
    "        var deserialize = source.IndexOf(\"JsonSerializer.Deserialize<BackupHeader>\", schema, StringComparison.Ordinal);\n"
    "        var validate = source.IndexOf(\"BackupFormatPolicy.ValidateHeader\", deserialize, StringComparison.Ordinal);\n"
    "        var derive = source.IndexOf(\"key = _crypto.DeriveKey\", validate, StringComparison.Ordinal);\n\n"
    "        Assert.True(restore >= 0);\n"
    "        Assert.True(schema > restore);",
)

replace_once(
    "tests/CipherNest.IntegrationTests/BackupHeaderValidationIntegrationTests.cs",
    "            CreatedUtc = DateTimeOffset.Parse(\"2026-08-15T00:00:00+00:00\")",
    "            CreatedUtc = new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero)",
)

insert_after(
    "docs/README.md",
    "- [`verification/SETTINGS_JSON_HARDENING_2026_08_15.md`](verification/SETTINGS_JSON_HARDENING_2026_08_15.md) — source/test/current-head verification contract for bounded settings reads, explicit JSON depth, invalid UTF-8 fallback, normalization, and deterministic adversarial settings coverage.\n",
    "- [`verification/BACKUP_HEADER_HARDENING_2026_08_15.md`](verification/BACKUP_HEADER_HARDENING_2026_08_15.md) — source/test/current-head verification contract for strict bounded version-2 backup-header JSON, pre-Argon2 rejection, and deterministic adversarial header coverage.\n",
)

replace_once(
    "docs/formats/ENCRYPTED_BACKUP.md",
    "Restore header-size framing currently requires:\n\n```text\n16 <= serialized header bytes <= 16,384\n```",
    "Restore header-size framing currently requires:\n\n```text\n16 <= serialized header bytes <= 16,384\nmaximum JSON nesting depth = 16\n```\n\nFor version 2, the root JSON object must contain exactly one each of the case-sensitive `Version`, `Salt`, `Kdf`, `ChunkSize`, and `CreatedUtc` properties. `Kdf` must contain exactly one each of `MemoryKiB`, `Iterations`, and `Parallelism`. Duplicate, unknown, missing, case-variant, or wrong-JSON-type metadata is rejected before typed deserialization/key derivation.",
)

replace_once(
    "docs/formats/ENCRYPTED_BACKUP.md",
    "## 5. Header validation before Argon2\n\nBefore key derivation restore requires:\n",
    "## 5. Header structure and resource validation before Argon2\n\nBefore typed deserialization/key derivation, restore validates the declared header length, parses only the bounded header bytes with comments/trailing commas disallowed and a maximum depth of 16, and enforces the exact version-2 root/KDF property sets described above.\n\nAfter strict JSON structure validation and typed deserialization, restore requires:\n",
)

replace_once(
    "docs/formats/ENCRYPTED_BACKUP.md",
    "4. reads/deserializes the header;\n5. validates version/salt/KDF/chunk resources;",
    "4. reads the bounded header bytes and validates strict version-2 JSON structure/depth before typed deserialization;\n5. deserializes the header and validates version/salt/KDF/chunk resources;",
)

replace_once(
    "docs/formats/ENCRYPTED_BACKUP.md",
    "- invalid magic/version/header size;\n- hostile salt/KDF/chunk metadata rejected before Argon2;",
    "- invalid magic/version/header size;\n- strict version-2 header schema: duplicate/unknown/missing/wrong-type properties and excessive nesting rejected before Argon2;\n- deterministic adversarial backup-header corpus with zero key-derivation calls for hostile inputs;\n- hostile salt/KDF/chunk metadata rejected before Argon2;",
)

replace_once(
    "docs/LIMITS_AND_DEFAULTS.md",
    "| Format version | — | exactly `2` |\n| Salt | 16 bytes | 64 bytes |",
    "| Format version | — | exactly `2` |\n| Header JSON bytes | 16 bytes | 16,384 bytes |\n| Header JSON nesting depth | — | 16 |\n| Salt | 16 bytes | 64 bytes |",
)

replace_once(
    "docs/LIMITS_AND_DEFAULTS.md",
    "The header bounds are validated before Argon2 derivation during restore.",
    "The declared and actual header byte bounds are validated before Argon2 derivation during restore. Version-2 header JSON additionally uses an explicit 16-level parser depth ceiling and exact case-sensitive root/KDF property sets; duplicate, unknown, missing, case-variant, and wrong-type metadata is rejected before typed deserialization/key derivation.",
)

replace_once(
    "docs/TEST_PLAN.md",
    "- Backup headers must validate format version, salt length, KDF resource bounds, and chunk size before any Argon2 derivation. Unsupported/malformed metadata must fail as invalid backup data.",
    "- Backup headers must validate the 16..16,384-byte framing ceiling, strict case-sensitive version-2 root/KDF JSON property sets, duplicate/unknown/missing/wrong-type metadata, 16-level JSON depth, format version, salt length, KDF resource bounds, and chunk size before any Argon2 derivation. Unsupported/malformed/adversarial metadata must fail as invalid backup data with zero key-derivation calls.",
)

replace_once(
    "docs/TEST_PLAN.md",
    "backup-header/path/archive resource behavior,",
    "strict backup-header schema/adversarial/path/archive resource behavior,",
)

replace_once(
    "docs/TESTING_GUIDE.md",
    "- header length;\n- salt bounds;",
    "- 16..16,384-byte header length boundaries;\n- strict version-2 root/KDF property allowlists and required-property sets;\n- duplicate/unknown/case-variant/wrong-type header metadata;\n- 16-level JSON depth and invalid UTF-8/malformed JSON normalization;\n- deterministic adversarial header corpus rejected before key derivation;\n- salt bounds;",
)

insert_after(
    "docs/NEXT_STEPS.md",
    "- Test unsupported backup version, too-short/too-long salt, hostile KDF parameters, and invalid chunk-size metadata; rejection must happen before Argon2 key derivation.\n",
    "- Reconfirm strict version-2 backup-header parsing with duplicate/unknown/missing/wrong-type properties, excessive nesting, exact 16,384-byte boundary input, and the deterministic hostile-header corpus; every invalid case must fail before key derivation.\n",
)

replace_once(
    "docs/NEXT_STEPS.md",
    "- Extend parser fuzzing beyond the current deterministic CSV-header and settings-JSON adversarial corpora to CSV row/import semantics, backup archives/header metadata, attachment metadata/storage names, TOTP Base32 input, vault records, and vault-header deserialization.",
    "- Extend parser fuzzing beyond the current deterministic CSV-header, settings-JSON, and backup-header adversarial corpora to CSV row/import semantics, backup ZIP/archive semantics beyond header metadata, attachment metadata/storage names, TOTP Base32 input, vault records, and vault-header deserialization.",
)

insert_after(
    "PROJECT_STATUS.md",
    "- Settings JSON loading now enforces both the 64 KiB file ceiling and a fixed 64 KiB + 1 actual-read sentinel boundary before bounded-memory deserialization, caps nesting at 16, falls back safely on invalid UTF-8/over-depth input, preserves UTF-8 BOM compatibility, and has deterministic adversarial-corpus plus source-regression coverage.\n",
    "- Encrypted backup version-2 header JSON is now strict and bounded before Argon2: 16..16,384 bytes, maximum depth 16, exact case-sensitive root/KDF property sets, duplicate/unknown/missing/wrong-type rejection, exporter self-validation, restore-order source guards, and a deterministic hostile-header corpus that requires zero key-derivation calls.\n",
)

replace_once(
    "PROJECT_STATUS.md",
    "- Backup restore validates backup format version, salt length, KDF bounds, and chunk size before Argon2 key derivation.",
    "- Backup restore additionally validates format version, salt length, KDF bounds, and chunk size after strict header-structure validation and still before Argon2 key derivation.",
)

insert_after(
    "CHANGELOG.md",
    "### Added\n",
    "- Strict encrypted-backup version-2 header JSON validation with 16..16,384-byte framing, 16-level depth, exact case-sensitive root/KDF property sets, duplicate/unknown/missing/wrong-type rejection before Argon2, exporter self-validation, deterministic hostile-header corpus coverage, and source/documentation regression guards.\n",
)

replace_once(
    "docs/COMPLETE_PROJECT_DOCUMENTATION.md",
    "| Backup ZIP entries | 10,001 maximum (`vault.db` plus attachment slots) |\n| Settings JSON | 64 KiB |",
    "| Backup ZIP entries | 10,001 maximum (`vault.db` plus attachment slots) |\n| Backup header JSON | 16–16,384 bytes; maximum depth 16; exact version-2 root/KDF property sets |\n| Settings JSON | 64 KiB |",
)

replace_once(
    "docs/COMPLETE_PROJECT_DOCUMENTATION.md",
    "- validates container version, salt length, KDF bounds, and chunk metadata before Argon2 derivation;",
    "- validates the bounded strict version-2 header JSON (16..16,384 bytes, depth 16, exact root/KDF property sets, duplicate/unknown/missing/wrong-type rejection) before typed deserialization/resource checks and Argon2 derivation;",
)

replace_once(
    "docs/operations/BACKUP_RECOVERY_RUNBOOK.md",
    "3. validate bounded header/KDF/chunk metadata before Argon2;",
    "3. validate the 16..16,384-byte header boundary, strict version-2 JSON schema/depth, and bounded KDF/chunk metadata before Argon2;",
)

replace_once(
    "docs/operations/BACKUP_RECOVERY_RUNBOOK.md",
    "Header JSON:          16..16,384 bytes framing\nSalt:",
    "Header JSON:          16..16,384 bytes framing\nHeader JSON depth:    max 16; exact version-2 root/KDF property sets\nSalt:",
)

replace_once(
    "docs/security/CRYPTOGRAPHIC_DESIGN.md",
    "Encrypted backup headers additionally require backup format version `2`, salt length 16–64 bytes, chunk size 64 KiB–4 MiB, and the same bounded Argon2 resource ranges. `BackupFormatPolicy.ValidateHeader` executes before backup key derivation.",
    "Encrypted backup headers additionally require 16..16,384 bounded JSON bytes, maximum nesting depth 16, and the exact case-sensitive version-2 root/KDF property sets before typed deserialization. Duplicate, unknown, missing, case-variant, or wrong-type metadata is rejected before key derivation. After that structural gate, backup format version `2`, salt length 16–64 bytes, chunk size 64 KiB–4 MiB, and the same bounded Argon2 resource ranges are enforced by `BackupFormatPolicy.ValidateHeader` before backup key derivation.",
)

replace_once(
    "docs/security/CRYPTOGRAPHIC_DESIGN.md",
    "9. Restore validates magic/header-size framing and backup version/salt/KDF/chunk bounds before Argon2, authenticates encrypted chunks, enforces the chunk-count ceiling, bounds total archive size/entry count/paths, rejects duplicate normalized ZIP paths, and rejects attachment entries outside the implemented encrypted-container size envelope.",
    "9. Restore validates magic/header-size framing, strict version-2 JSON schema/depth, and backup version/salt/KDF/chunk bounds before Argon2, authenticates encrypted chunks, enforces the chunk-count ceiling, bounds total archive size/entry count/paths, rejects duplicate normalized ZIP paths, and rejects attachment entries outside the implemented encrypted-container size envelope.",
)

replace_once(
    "docs/security/THREAT_MODEL.md",
    "- **Tampered records/backups:** AES-GCM authentication causes altered envelopes to be rejected. Backup header version/salt/KDF/chunk metadata is resource-validated before Argon2 work.",
    "- **Tampered records/backups:** AES-GCM authentication causes altered envelopes to be rejected. Backup header JSON is byte/depth-bounded and restricted to the exact version-2 root/KDF schema before typed deserialization; version/salt/KDF/chunk resource metadata is then validated before Argon2 work.",
)

replace_once(
    "docs/security/THREAT_MODEL.md",
    "- **Malicious import/backup:** strict CSV parsing, per-field/per-row/column/logical-row bounds, final-field column enforcement, temporary staging, format/version checks, encrypted chunk-count bounds, authenticated backup validation, duplicate-entry/attachment-container bounds, and pre-replacement SQLite/schema/resource validation reduce risk; parser/runtime flaws remain possible.",
    "- **Malicious import/backup:** strict CSV parsing, per-field/per-row/column/logical-row bounds, final-field column enforcement, deterministic CSV/settings/backup-header adversarial corpora, strict bounded version-2 backup-header schema/depth checks before Argon2, temporary staging, format/version checks, encrypted chunk-count bounds, authenticated backup validation, duplicate-entry/attachment-container bounds, and pre-replacement SQLite/schema/resource validation reduce risk; parser/runtime flaws remain possible.",
)

replace_once(
    "docs/security/THREAT_MODEL.md",
    "Header version/salt/KDF/chunk metadata is validated before Argon2. Encrypted framing has an explicit maximum chunk count in addition to the 1 GiB aggregate archive budget.",
    "Header JSON first passes the 16..16,384-byte framing limit, 16-level depth ceiling, and exact case-sensitive version-2 root/KDF property checks; duplicate/unknown/missing/wrong-type metadata is rejected before typed deserialization. Version/salt/KDF/chunk resource metadata is then validated before Argon2. Encrypted framing has an explicit maximum chunk count in addition to the 1 GiB aggregate archive budget.",
)

ledger = """

### Encrypted backup header schema and adversarial hardening

- Centralized encrypted-backup header framing limits in `BackupFormatPolicy`: 16-byte minimum, 16,384-byte maximum, and explicit JSON depth ceiling 16.
- Added `BackupHeaderJsonPolicy` to parse only bounded header bytes with trailing commas/comments disabled and to require the exact case-sensitive version-2 root properties (`Version`, `Salt`, `Kdf`, `ChunkSize`, `CreatedUtc`) and exact KDF properties (`MemoryKiB`, `Iterations`, `Parallelism`).
- Duplicate, unknown, missing, case-variant, and wrong-JSON-type root/KDF metadata now fails before typed deserialization and before any backup key derivation.
- `EncryptedBackupService.RestoreEncryptedAsync` now validates declared header length before allocation/read, validates strict header structure before `BackupHeader` deserialization, then validates version/salt/KDF/chunk resources before `_crypto.DeriveKey(...)`.
- Export now self-validates its freshly serialized version-2 header before writing so a future internal record change cannot silently emit an incompatible same-version schema.
- Added unit coverage for framing boundaries, strict property allowlists/required sets, duplicate metadata, wrong JSON kinds, and over-depth input.
- Extended restore integration coverage for duplicate/unknown/deep headers, exact 16,384-byte accepted JSON-whitespace padding, 16,385-byte rejection, truncated/malformed input, and zero-derive behavior for invalid metadata.
- Added `BackupHeaderAdversarialIntegrationTests` with a fixed-seed corpus of 90 hostile headers spanning malformed roots, missing/type-invalid fields, invalid Base64, hostile KDF values, duplicate/unknown properties, randomized unexpected metadata, and invalid UTF-8/random byte sequences; every member must fail as invalid data with zero key-derivation calls.
- Strengthened `BackupFormatSourceTests` so ordering is anchored inside `RestoreEncryptedAsync` and requires strict schema validation before deserialization/resource validation/derivation.
- Added `docs/verification/BACKUP_HEADER_HARDENING_2026_08_15.md` and synchronized the backup format, limits, cryptographic design, threat model, testing guide/plan, recovery runbook, roadmap, consolidated documentation, project status, changelog, and documentation hub.
- The deterministic corpus is regression coverage, not exhaustive coverage-guided fuzzing or an independent professional security audit. Broader backup ZIP/archive fuzzing, target-device behavior, release signing/store validation, and independent review remain separate gates.
"""
what_changed = read("what_changed.md")
if "### Encrypted backup header schema and adversarial hardening" not in what_changed:
    write("what_changed.md", what_changed.rstrip() + ledger)
