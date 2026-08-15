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
        raise RuntimeError(f"Expected text not found in {path}: {old[:180]!r}")
    write(path, text.replace(old, new, 1))


def insert_after(path: str, marker: str, addition: str) -> None:
    text = read(path)
    if addition.strip() in text:
        return
    if marker not in text:
        raise RuntimeError(f"Marker not found in {path}: {marker[:180]!r}")
    write(path, text.replace(marker, marker + addition, 1))


# Documentation hub.
insert_after(
    "docs/README.md",
    "- [`formats/VAULT_RECORDS.md`](formats/VAULT_RECORDS.md) — logical vault item model, encrypted record identity binding, validation, TOTP record parameters, and storage limits.\n",
    "- [`formats/VAULT_HEADER.md`](formats/VAULT_HEADER.md) — strict version-aware local vault-header JSON schema, wrapped-key/KDF metadata contract, parser bounds, compatibility, and pre-unwrap validation order.\n",
)
insert_after(
    "docs/README.md",
    "- [`verification/BACKUP_HEADER_HARDENING_2026_08_15.md`](verification/BACKUP_HEADER_HARDENING_2026_08_15.md) — source/test/current-head verification contract for strict bounded version-2 backup-header JSON, pre-Argon2 rejection, and deterministic adversarial header coverage.\n",
    "- [`verification/VAULT_HEADER_HARDENING_2026_08_15.md`](verification/VAULT_HEADER_HARDENING_2026_08_15.md) — source/test/current-head verification contract for strict v1/v2 local vault-header JSON, pre-unwrap/replacement rejection, legacy compatibility, and the deterministic hostile-header corpus.\n",
)

# Limits/defaults.
replace_once(
    "docs/LIMITS_AND_DEFAULTS.md",
    "| Vault-header UTF-8 bytes | 64 KiB |",
    "| Vault-header UTF-8 bytes | 64 KiB |\n| Vault-header JSON nesting depth | 16 |\n| Vault-header JSON schema | exact case-sensitive v1/v2 root + wrapped-key/KDF property sets |",
)
replace_once(
    "docs/LIMITS_AND_DEFAULTS.md",
    "SQLite and service-level paths enforce overlapping limits so a custom store cannot intentionally bypass every boundary.",
    "Vault-header version 1 is read-compatible only with the exact `version`/`master`/`recovery` root; version 2 is the current write format and additionally requires `secondary` (which may be null). Every non-null wrapped-key object and nested KDF object uses an exact case-sensitive property set, and duplicate/unknown/missing/wrong-kind metadata or nesting beyond 16 is rejected before typed header deserialization/wrapped-key unwrap. Replacement-database validation applies the same strict header policy before active DB/WAL/SHM mutation.\n\nSQLite and service-level paths enforce overlapping limits so a custom store cannot intentionally bypass every boundary.",
)

# Database architecture.
replace_once(
    "docs/architecture/DATABASE.md",
    "- vault-header JSON: maximum 64 KiB UTF-8;",
    "- vault-header JSON: maximum 64 KiB UTF-8, maximum depth 16, exact version-aware root/wrapped-key/KDF schema;",
)
replace_once(
    "docs/architecture/DATABASE.md",
    "`ReadHeaderAsync` reads the UTF-8 byte length before materializing header text. `ReadAllItemsAsync` checks aggregate count/bytes and each `length(Envelope)` before reading the BLOB. Writes enforce the corresponding limits as well. Stored item IDs must be canonical lower-case GUID `D` strings; after decryption the payload ID must still equal the authenticated row ID.",
    "`ReadHeaderAsync` reads the UTF-8 byte length before materializing header text. `VaultService` then applies `VaultHeaderJsonPolicy` before typed header deserialization or wrapped-key unwrap: v1/v2 roots, non-null wrappers, nested KDF objects, duplicate/unknown/missing/wrong-kind metadata, and depth are all validated explicitly. `ReadAllItemsAsync` checks aggregate count/bytes and each `length(Envelope)` before reading the BLOB. Writes enforce the corresponding limits as well. Stored item IDs must be canonical lower-case GUID `D` strings; after decryption the payload ID must still equal the authenticated row ID.",
)
replace_once(
    "docs/architecture/DATABASE.md",
    "5. require a bounded vault header;",
    "5. require a byte-bounded vault header and validate its strict supported v1/v2 JSON schema/depth before active mutation;",
)
insert_after(
    "docs/architecture/DATABASE.md",
    "- `../formats/VAULT_RECORDS.md` — logical/encrypted row representation and identity binding.\n",
    "- `../formats/VAULT_HEADER.md` — local vault-header schema, version compatibility, parser bounds, and pre-unwrap/pre-replacement validation.\n",
)

# Cryptographic design.
replace_once(
    "docs/security/CRYPTOGRAPHIC_DESIGN.md",
    "Vault-header JSON is also bounded to 64 KiB UTF-8. The SQLite store checks byte length before materializing header text, and `VaultService` applies the same bound before deserialization for alternate store implementations. Malformed JSON is mapped to vault authentication failure at the service boundary. Future header expansion must fit that budget or deliberately version/review it.",
    "Vault-header JSON is also bounded to 64 KiB UTF-8 with a maximum JSON depth of 16. The SQLite store checks byte length before materializing header text. Before typed header deserialization or any wrapped-key unwrap, `VaultHeaderJsonPolicy` requires the exact case-sensitive version-aware root schema (v1: `version/master/recovery`; v2: `version/master/recovery/secondary`), exact non-null wrapped-key properties (`version/salt/kdf/nonce/ciphertext/tag`), and exact integer KDF properties (`memoryKiB/iterations/parallelism`). Duplicate, unknown, missing, case-variant, or wrong-kind metadata is rejected. Malformed/schema-invalid input is mapped to vault authentication failure at the service boundary. Replacement-database validation applies the same strict policy before active DB/WAL/SHM mutation. Future header expansion must deliberately version/review this compatibility contract.",
)
replace_once(
    "docs/security/CRYPTOGRAPHIC_DESIGN.md",
    "- Vault-header document versioning is separate and explicitly range/size/JSON checked.",
    "- Vault-header document versioning is separate and explicitly range/size/depth/schema checked; current writes self-validate as version 2 while valid historical version 1 remains readable.",
)

# Threat model.
replace_once(
    "docs/security/THREAT_MODEL.md",
    "- **Malformed vault-header input:** header UTF-8 length is bounded before deserialization, future/unknown versions are rejected, and malformed header JSON is normalized to a vault-authentication failure rather than propagating parser details through unlock.",
    "- **Malformed vault-header input:** persisted UTF-8 length is bounded before materialization; JSON depth is capped at 16; exact case-sensitive v1/v2 root, wrapped-key, and KDF property sets reject duplicate/unknown/missing/case-variant/wrong-kind metadata before typed deserialization/wrapped-key unwrap; malformed input is normalized to vault-authentication failure. Replacement candidates must pass the same strict header policy before active DB/WAL/SHM mutation.",
)
replace_once(
    "docs/security/THREAT_MODEL.md",
    "CipherNest accepts only explicitly supported vault-header versions. A future/unknown version is rejected before key unwrap instead of being interpreted as if it were a current structure. Header JSON is additionally bounded to 64 KiB UTF-8 before deserialization at the SQLite/service boundaries, and malformed JSON maps to authentication failure in unlock flows.",
    "CipherNest accepts only explicitly supported vault-header versions. Version 1 remains readable only with its historical three-property root; version 2 is the current four-property write schema. A future/unknown version or undocumented v1/v2 hybrid is rejected before key unwrap instead of being interpreted as current structure. Header JSON is bounded to 64 KiB UTF-8 and depth 16, with exact case-sensitive root/wrapper/KDF property sets enforced before typed deserialization; malformed/schema-invalid JSON maps to authentication failure in unlock flows. Header mutations self-validate and upgrade supported legacy v1 metadata to the current v2 write shape.",
)
replace_once(
    "docs/security/THREAT_MODEL.md",
    "- **Malicious import/backup:** strict CSV parsing, per-field/per-row/column/logical-row bounds, final-field column enforcement, deterministic CSV/settings/backup-header adversarial corpora, strict bounded version-2 backup-header schema/depth checks before Argon2, temporary staging, format/version checks, encrypted chunk-count bounds, authenticated backup validation, duplicate-entry/attachment-container bounds, and pre-replacement SQLite/schema/resource validation reduce risk; parser/runtime flaws remain possible.",
    "- **Malicious import/backup:** strict CSV parsing, per-field/per-row/column/logical-row bounds, final-field column enforcement, deterministic CSV/settings/backup-header/vault-header adversarial corpora, strict bounded backup/vault-header schema/depth checks before Argon2 or wrapped-key unwrap, temporary staging, format/version checks, encrypted chunk-count bounds, authenticated backup validation, duplicate-entry/attachment-container bounds, and pre-replacement SQLite/schema/resource/strict-vault-header validation reduce risk; parser/runtime flaws remain possible.",
)

# Data flow.
replace_once(
    "docs/architecture/DATA_FLOW.md",
    "bounded versioned vault header JSON\n        |\n        v\nSQLite VaultHeader",
    "self-validated current-v2 vault header JSON\n(exact root/wrapper/KDF schema; <=64 KiB; depth <=16)\n        |\n        v\nSQLite VaultHeader",
)
replace_once(
    "docs/architecture/DATA_FLOW.md",
    "      +--> read bounded vault header\n      +--> validate header version/shape/resource limits",
    "      +--> read byte-bounded vault header\n      +--> strict v1/v2 root/wrapper/KDF JSON validation (depth <=16)\n      +--> typed header/version/resource validation",
)
replace_once(
    "docs/architecture/DATA_FLOW.md",
    "Invalid credential or malformed wrapper authentication maps to vault authentication failure. Future/unsupported vault-header versions are rejected before unwrap.",
    "Invalid credential or malformed wrapper authentication maps to vault authentication failure. Duplicate/unknown/missing/wrong-kind/deep vault-header JSON and future/unsupported versions are rejected before typed deserialization/wrapped-key unwrap. Historical v1 remains readable; current header mutations self-validate and write v2.",
)
replace_once(
    "docs/architecture/DATA_FLOW.md",
    "      +--> bounded header\n      +--> canonical item IDs",
    "      +--> byte/depth-bounded strict supported vault-header schema\n      +--> canonical item IDs",
)

# Test plan.
replace_once(
    "docs/TEST_PLAN.md",
    "- Vault headers must accept only explicitly supported versions and reject future/unknown versions before key unwrap. Header metadata must remain within the 64 KiB UTF-8 storage budget before deserialization.",
    "- Vault headers must accept the exact historical v1 root and current v2 root only, cap UTF-8 storage at 64 KiB and JSON depth at 16, enforce exact case-sensitive root/wrapped-key/KDF property sets, and reject duplicate/unknown/missing/case-variant/wrong-kind/future/hybrid metadata before typed deserialization or wrapped-key unwrap. The deterministic 120-case hostile-header corpus must leave the vault locked with zero unwrap calls; replacement-database validation must reject a bounded-but-schema-invalid header before active DB/WAL/SHM mutation while retaining valid v1 compatibility.",
)
replace_once(
    "docs/TEST_PLAN.md",
    "replacement validation/component-aware rollback ordering, primary-before-sidecar database deletion, encrypted-record resource checks, cancellable zeroing key leases,",
    "replacement validation/component-aware rollback ordering, strict vault-header validation/upgrade/pre-swap ordering, primary-before-sidecar database deletion, encrypted-record resource checks, cancellable zeroing key leases,",
)

# Testing guide.
replace_once(
    "docs/TESTING_GUIDE.md",
    "- future/unsupported vault-header rejection;\n- bounded/malformed header rejection;",
    "- exact v1/v2 vault-header compatibility and v1-to-v2 mutation upgrade;\n- future/unsupported/hybrid vault-header rejection;\n- 64 KiB byte, 16-level depth, strict root/wrapper/KDF schema, duplicate/unknown/missing/wrong-kind rejection before unwrap;\n- deterministic hostile-header corpus with zero unwrap calls;",
)
replace_once(
    "docs/TESTING_GUIDE.md",
    "- header/resource limits before BLOB/text materialization;",
    "- header/resource limits before BLOB/text materialization;\n- strict vault-header schema validation on replacement candidates before active database mutation, including legacy-v1 acceptance;",
)

# Roadmap reconciliation.
replace_once(
    "docs/NEXT_STEPS.md",
    "- Verify current vault headers remain readable while an unsupported future header or >64 KiB UTF-8 header is rejected before unwrap/deserialization.",
    "- Reconfirm strict vault-header compatibility with the exact historical v1/current v2 shapes, 64 KiB byte and 16-level depth boundaries, duplicate/unknown/missing/wrong-kind metadata, v1-to-v2 mutation upgrade, the deterministic 120-case hostile corpus, and malformed replacement-header pre-swap rejection; invalid structures must never reach wrapped-key unwrap.",
)
replace_once(
    "docs/NEXT_STEPS.md",
    "- Extend parser fuzzing beyond the current deterministic CSV-header, settings-JSON, and backup-header adversarial corpora to CSV row/import semantics, backup ZIP/archive semantics beyond header metadata, attachment metadata/storage names, TOTP Base32 input, vault records, and vault-header deserialization.",
    "- Extend parser fuzzing beyond the current deterministic CSV-header, settings-JSON, backup-header, and vault-header adversarial corpora to CSV row/import semantics, backup ZIP/archive semantics beyond header metadata, attachment metadata/storage names, TOTP Base32 input, and vault-record/envelope semantics.",
)

# Release checklist.
replace_once(
    "docs/RELEASE_CHECKLIST.md",
    "- [ ] Vault header compatibility tests reject future/unknown versions and malformed header JSON as authentication failures before key unwrap while current supported headers remain unlockable; header UTF-8 storage above 64 KiB is rejected before deserialization/materialization where implemented.",
    "- [ ] Vault header tests retain exact historical-v1/current-v2 schema compatibility, 64 KiB UTF-8 and 16-level JSON-depth bounds, exact case-sensitive root/wrapped-key/KDF property sets, duplicate/unknown/missing/wrong-kind rejection before deserialization/unwrap, v1-to-v2 mutation upgrade, and the deterministic 120-case zero-unwrap corpus. Replacement candidates with a bounded but schema-invalid header are rejected before active DB/WAL/SHM mutation.",
)
replace_once(
    "docs/RELEASE_CHECKLIST.md",
    "- [ ] Candidate replacement databases pass SQLite `quick_check`, exact supported schema version, required schema shape, required/bounded vault header, canonical item IDs, and encrypted-record count/per-record/aggregate budgets before active DB/WAL/SHM mutation. Invalid replacements preserve the active vault.",
    "- [ ] Candidate replacement databases pass SQLite `quick_check`, exact supported schema version, required schema shape, byte/depth-bounded strict supported vault-header schema, canonical item IDs, and encrypted-record count/per-record/aggregate budgets before active DB/WAL/SHM mutation. Invalid replacements preserve the active vault.",
)

# Backup/recovery operational validation.
replace_once(
    "docs/operations/BACKUP_RECOVERY_RUNBOOK.md",
    "10. validate the replacement DB through store integrity/schema/resource checks before active mutation;",
    "10. validate the replacement DB through store integrity/schema/resource checks, including strict supported v1/v2 vault-header JSON, before active mutation;",
)
insert_after(
    "docs/operations/BACKUP_RECOVERY_RUNBOOK.md",
    "Archive bytes:        max 1 GiB\n",
    "Restored vault header: max 64 KiB UTF-8; depth 16; exact supported v1/v2 root/wrapper/KDF schemas\n",
)

# Consolidated documentation.
replace_once(
    "docs/COMPLETE_PROJECT_DOCUMENTATION.md",
    "Replacement databases are validated before active mutation. Validation includes SQLite integrity checks, exact supported schema version, required table/column shape, bounded vault header, canonical item IDs, and encrypted-record resource budgets.",
    "Replacement databases are validated before active mutation. Validation includes SQLite integrity checks, exact supported schema version, required table/column shape, byte/depth-bounded strict supported v1/v2 vault-header schema, canonical item IDs, and encrypted-record resource budgets. Normal unlock likewise validates the exact vault-header root/wrapped-key/KDF JSON structure before typed deserialization or wrapped-key unwrap; valid historical v1 remains readable while current mutations write self-validated v2.",
)
replace_once(
    "docs/COMPLETE_PROJECT_DOCUMENTATION.md",
    "| Vault header | 64 KiB UTF-8 |",
    "| Vault header | 64 KiB UTF-8; maximum JSON depth 16; exact supported v1/v2 root/wrapped-key/KDF schemas |",
)
insert_after(
    "docs/COMPLETE_PROJECT_DOCUMENTATION.md",
    "The canonical logical-record contract is `docs/formats/VAULT_RECORDS.md`.\n",
    "The local header compatibility/parser contract is `docs/formats/VAULT_HEADER.md`.\n",
)

# Project status and changelog.
insert_after(
    "PROJECT_STATUS.md",
    "### Completed in source\n",
    "- Local vault-header JSON now has strict version-aware validation before typed deserialization/wrapped-key unwrap: 64 KiB UTF-8, maximum depth 16, exact case-sensitive v1/v2 root + wrapped-key/KDF property sets, duplicate/unknown/missing/wrong-kind rejection, writer self-validation, deliberate v1-to-v2 mutation upgrade, strict replacement-database pre-swap validation, deterministic 120-case hostile corpus, and source/documentation regression guards.\n",
)
replace_once(
    "PROJECT_STATUS.md",
    "- Supported vault-header versions are explicit; future/unknown or malformed JSON headers are rejected as authentication failures before key unwrap.",
    "- Supported vault-header versions are explicit: exact historical v1 remains readable, v2 is the current self-validated write format, hybrid/future/malformed structures are rejected before key unwrap, and current header mutations deliberately upgrade legacy v1 metadata to v2.",
)
insert_after(
    "CHANGELOG.md",
    "### Added\n",
    "- Strict local vault-header JSON policy with a 64 KiB byte ceiling, 16-level depth bound, exact case-sensitive historical-v1/current-v2 root and nested wrapped-key/KDF schemas, deterministic 120-case hostile corpus, replacement-database pre-swap validation, compatibility fixtures, source guards, and canonical format/verification documentation.\n",
)
insert_after(
    "CHANGELOG.md",
    "### Changed\n",
    "- Vault header reads now validate strict structure before typed deserialization/wrapped-key unwrap; current mutations self-validate and deliberately upgrade valid legacy v1 headers to v2, preventing an undocumented v1-plus-`secondary` hybrid shape.\n",
)

# Exact format doc gets replacement boundary too.
insert_after(
    "docs/formats/VAULT_HEADER.md",
    "Malformed JSON/schema/storage data is normalized to `VaultAuthenticationException` at the vault-service unlock boundary. No invalid structural header is allowed to reach wrapped-key unwrap.\n",
    "\nReplacement-database validation uses the same byte/depth/schema policy while the candidate SQLite database is still read-only and before active DB/WAL/SHM mutation. A malformed-but-small candidate header therefore fails the pre-swap boundary rather than replacing the active vault and failing only at a later unlock.\n",
)

# Documentation coverage/source gate.
replace_once(
    "tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs",
    "        [\"docs\", \"formats\", \"VAULT_RECORDS.md\"],\n        [\"docs\", \"formats\", \"ATTACHMENTS.md\"],",
    "        [\"docs\", \"formats\", \"VAULT_RECORDS.md\"],\n        [\"docs\", \"formats\", \"VAULT_HEADER.md\"],\n        [\"docs\", \"formats\", \"ATTACHMENTS.md\"],",
)
replace_once(
    "tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs",
    "        [\"docs\", \"verification\", \"BACKUP_HEADER_HARDENING_2026_08_15.md\"],\n        [\"docs\", \"operations\", \"BACKUP_RECOVERY_RUNBOOK.md\"],",
    "        [\"docs\", \"verification\", \"BACKUP_HEADER_HARDENING_2026_08_15.md\"],\n        [\"docs\", \"verification\", \"VAULT_HEADER_HARDENING_2026_08_15.md\"],\n        [\"docs\", \"operations\", \"BACKUP_RECOVERY_RUNBOOK.md\"],",
)
replace_once(
    "tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs",
    "                     \"formats/VAULT_RECORDS.md\",\n                     \"formats/ATTACHMENTS.md\",",
    "                     \"formats/VAULT_RECORDS.md\",\n                     \"formats/VAULT_HEADER.md\",\n                     \"formats/ATTACHMENTS.md\",",
)
replace_once(
    "tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs",
    "                     \"verification/BACKUP_HEADER_HARDENING_2026_08_15.md\",\n                     \"operations/BACKUP_RECOVERY_RUNBOOK.md\",",
    "                     \"verification/BACKUP_HEADER_HARDENING_2026_08_15.md\",\n                     \"verification/VAULT_HEADER_HARDENING_2026_08_15.md\",\n                     \"operations/BACKUP_RECOVERY_RUNBOOK.md\",",
)
replace_once(
    "tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs",
    "        var backupHeaderVerification = File.ReadAllText(PathAt(\"docs\", \"verification\", \"BACKUP_HEADER_HARDENING_2026_08_15.md\"));\n",
    "        var backupHeaderVerification = File.ReadAllText(PathAt(\"docs\", \"verification\", \"BACKUP_HEADER_HARDENING_2026_08_15.md\"));\n        var vaultHeaderVerification = File.ReadAllText(PathAt(\"docs\", \"verification\", \"VAULT_HEADER_HARDENING_2026_08_15.md\"));\n",
)
insert_after(
    "tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs",
    "        Assert.Contains(\"not an independent security audit\", backupHeaderVerification, StringComparison.OrdinalIgnoreCase);\n",
    "        Assert.Contains(\"exactly 120 fixed hostile cases\", vaultHeaderVerification, StringComparison.OrdinalIgnoreCase);\n        Assert.Contains(\"Version 1 remains accepted\", vaultHeaderVerification, StringComparison.OrdinalIgnoreCase);\n        Assert.Contains(\"not an independent security audit\", vaultHeaderVerification, StringComparison.OrdinalIgnoreCase);\n",
)

# Append implementation ledger safely.
ledger_path = "what_changed.md"
ledger = read(ledger_path).rstrip()
entry = '''### Local vault-header schema, compatibility, and adversarial hardening

- Added `VaultHeaderJsonPolicy` and `VaultStorageLimits.MaximumVaultHeaderJsonDepth = 16` so local vault-header JSON is bounded to 64 KiB UTF-8 and 16 nesting levels before typed header deserialization.
- Preserved historical version-1 compatibility with the exact `version`/`master`/`recovery` root while making version 2 the exact current `version`/`master`/`recovery`/`secondary` write schema; undocumented hybrids and future versions fail closed.
- Enforced exact ordinal/case-sensitive wrapped-key (`version`, `salt`, `kdf`, `nonce`, `ciphertext`, `tag`) and KDF (`memoryKiB`, `iterations`, `parallelism`) property sets, rejecting duplicate, unknown, missing, case-variant, wrong-kind, and non-integer metadata before unwrap.
- `VaultService.ReadHeaderUnlockedAsync` now applies strict structural validation before `VaultHeaderDocument` deserialization and normalizes structural/storage parser failures to `VaultAuthenticationException`; invalid structures cannot reach `_crypto.UnwrapKey(...)`.
- All current vault-header writes now pass through `SerializeHeader(...)`, which validates freshly serialized JSON before persistence. Master-passphrase and secondary-wrapper mutations explicitly write version 2, fixing the legacy-v1 mutation path that could otherwise serialize an undocumented v1 header carrying the v2-only `secondary` field.
- Hardened replacement-database validation so a candidate must pass persisted header byte-length consistency plus the same strict v1/v2 JSON schema/depth policy while still read-only and before active DB/WAL/SHM mutation.
- Added unit coverage for accepted v1/v2 shapes, schema violations, exact 64 KiB/first-over-limit boundaries, and depth rejection.
- Added runtime integration coverage for historical v1 unlock, deliberate v1-to-v2 mutation upgrade, exactly-one-unwrap reachability for structurally valid v2, oversized persisted-header zero-unwrap rejection, malformed replacement-header pre-swap preservation, and valid-v1 replacement compatibility.
- Added an exactly 120-case fixed-seed hostile vault-header corpus; every member must fail as vault authentication, keep the vault locked, and make zero wrapped-key unwrap calls.
- Added source-regression guards for strict policy constants/allowlists, validation-before-deserialization/unwrap ordering, writer self-validation, v1-to-v2 mutation upgrade, and no direct serialized-header write bypass.
- Added `docs/formats/VAULT_HEADER.md` and `docs/verification/VAULT_HEADER_HARDENING_2026_08_15.md`, then synchronized limits, database/data-flow architecture, cryptographic design, threat model, testing, roadmap, release checklist, backup/recovery runbook, consolidated docs, project status, changelog, documentation hub, and documentation coverage gates.
- The deterministic corpus is reproducible regression coverage, not exhaustive coverage-guided fuzzing or an independent professional security audit. Device behavior, signing/store validation, broader record/archive/parser fuzzing, and independent review remain separate gates.'''
if entry.splitlines()[0] not in ledger:
    write(ledger_path, ledger + "\n\n" + entry + "\n")
