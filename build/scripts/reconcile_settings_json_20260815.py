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
    "docs/TESTING_GUIDE.md",
    "- 64 KiB input/output file ceiling;\n- no stale staging after success;",
    "- 64 KiB input/output file ceiling;\n"
    "- actual input reads bounded by a fixed 64 KiB + 1 sentinel byte before JSON deserialization;\n"
    "- 16-level JSON nesting ceiling;\n"
    "- invalid UTF-8 fallback;\n"
    "- UTF-8 BOM compatibility through the bounded-memory path;\n"
    "- deterministic adversarial JSON corpus returning only normalized preferences;\n"
    "- no stale staging after success;",
)

replace_once(
    "docs/TEST_PLAN.md",
    "- Settings JSON must be rejected before deserialization when the file exceeds 64 KiB; serialized output must be checked against the same limit before replacing the active settings file.",
    "- Settings JSON must be rejected before deserialization when the file exceeds 64 KiB; serialized output must be checked against the same limit before replacing the active settings file.\n"
    "- Settings loading must also bound the actual read to a fixed 64 KiB + 1 sentinel byte, cap JSON nesting at 16, preserve UTF-8 BOM compatibility, fall back on invalid UTF-8/over-depth input, and keep deterministic adversarial-corpus outputs inside `AppPreferencesPolicy.Normalize(...)` invariants.",
)

insert_after(
    "docs/NEXT_STEPS.md",
    "- Verify malformed/unreadable settings files fall back to defaults while cancellation is still propagated.\n",
    "- Reconfirm the implemented 64 KiB + 1 sentinel read boundary using exact-limit, oversized, invalid UTF-8, and over-depth JSON fixtures when the settings schema changes.\n"
    "- Keep the explicit JSON depth ceiling synchronized with the flat `AppPreferences` schema; increasing it requires matching tests and limits documentation.\n",
)

replace_once(
    "docs/NEXT_STEPS.md",
    "- Extend parser fuzzing beyond the current deterministic CSV-header adversarial corpus to CSV row/import semantics, backup archives/header metadata, attachment metadata/storage names, settings JSON, TOTP Base32 input, vault records, and vault-header deserialization.",
    "- Extend parser fuzzing beyond the current deterministic CSV-header and settings-JSON adversarial corpora to CSV row/import semantics, backup archives/header metadata, attachment metadata/storage names, TOTP Base32 input, vault records, and vault-header deserialization.",
)

insert_after(
    "PROJECT_STATUS.md",
    "### Completed in source\n",
    "- Settings JSON loading now enforces both the 64 KiB file ceiling and a fixed 64 KiB + 1 actual-read sentinel boundary before bounded-memory deserialization, caps nesting at 16, falls back safely on invalid UTF-8/over-depth input, preserves UTF-8 BOM compatibility, and has deterministic adversarial-corpus plus source-regression coverage.\n",
)

replace_once(
    "PROJECT_STATUS.md",
    "- Settings persistence normalizes supported enum/numeric bounds on load/save, restores a valid password character group when password mode has none, falls back to defaults on malformed/unreadable non-secret settings files, rejects files above 64 KiB before JSON parsing, checks serialized output against the same 64 KiB ceiling, uses unique sibling `CreateNew` staging, and best-effort cleans staging without swallowing cancellation.",
    "- Settings persistence normalizes supported enum/numeric bounds on load/save, restores a valid password character group when password mode has none, falls back to defaults on malformed/unreadable non-secret settings files, rejects files above 64 KiB before parsing, independently bounds the actual read to 64 KiB + 1 sentinel byte, caps JSON depth at 16, preserves UTF-8 BOM compatibility, checks serialized output against the same 64 KiB ceiling, uses unique sibling `CreateNew` staging, and best-effort cleans staging without swallowing cancellation.",
)

insert_after(
    "CHANGELOG.md",
    "### Added\n",
    "- Bounded settings-JSON ingestion with a 64 KiB + 1 sentinel read path, explicit 16-level nesting ceiling, invalid UTF-8/over-depth fallback, UTF-8 BOM compatibility coverage, deterministic adversarial JSON corpus, and source/documentation regression guards.\n",
)

ledger = """

### Settings JSON bounded-read and adversarial hardening

- Hardened `JsonSettingsStore.LoadAsync` so the existing 64 KiB file-size snapshot is no longer the only input resource check; the actual read now goes through a fixed 64 KiB + 1 sentinel buffer before JSON deserialization.
- Deserialization now occurs from the bounded in-memory byte range rather than directly from the mutable filesystem stream, preventing a settings file that grows after the initial length observation from feeding unbounded parser input.
- Added `MaximumSettingsJsonDepth = 16` and applied it to the shared settings `JsonSerializerOptions`; the persisted `AppPreferences` schema is flat and does not require deep nesting.
- Preserved cancellation propagation: malformed/unreadable/unauthorized settings fall back to defaults, while caller cancellation is not converted into a successful fallback result.
- Preserved successful-parse normalization through `AppPreferencesPolicy.Normalize(...)` so syntactically valid JSON still cannot publish undefined enums, out-of-range numeric values, or an unusable all-disabled password-generator configuration.
- Extended `JsonSettingsStoreBoundsTests` with the exact 64 KiB accepted boundary, 64 KiB + 1 rejection, invalid UTF-8 fallback, excessive-depth fallback, and UTF-8 BOM compatibility through the bounded-memory path.
- Added `JsonSettingsAdversarialTests` with a fixed-seed corpus covering valid/default roots, malformed roots, extreme values, duplicate properties, malformed dates, unknown nested content, generator repair, controls, Unicode, escapes, and randomized JSON-like byte-safe text; every result must satisfy the normalized settings contract.
- Added `SettingsJsonSafetySourceTests` so the actual-read sentinel, depth ceiling, bounded-memory deserialization, normalization call, and absence of an unbounded `ReadToEnd` path cannot silently disappear.
- Added `docs/verification/SETTINGS_JSON_HARDENING_2026_08_15.md`, linked it from the documentation hub, required it through `DocumentationCoverageSourceTests`, and synchronized `docs/LIMITS_AND_DEFAULTS.md` with the new parser/resource contract.
- Reconciled the testing guide, test plan, next-steps roadmap, project status, and changelog so the implemented deterministic settings corpus is no longer listed as wholly outstanding parser-fuzzing work.
- This remains deterministic adversarial regression coverage rather than exhaustive coverage-guided fuzzing or an independent security audit. Broader parser fuzzing, device behavior, signing/store validation, and independent review remain separate gates.
"""
what_changed = read("what_changed.md")
if "### Settings JSON bounded-read and adversarial hardening" not in what_changed:
    write("what_changed.md", what_changed + ledger)
