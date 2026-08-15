from pathlib import Path


def replace_once(path: str, old: str, new: str) -> None:
    target = Path(path)
    text = target.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}")
    target.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


def main() -> None:
    replace_once(
        "CHANGELOG.md",
        "### Added\n",
        "### Added\n- Deterministic adversarial CSV-header corpus coverage plus source-regression guards for the imported-header trust boundary.\n",
    )
    replace_once(
        "CHANGELOG.md",
        "### Changed\n",
        "### Changed\n- CSV import now bounds each header name to 256 characters and rejects Unicode control/`Format` characters before headers reach mapping UI or mapped-column lookup.\n",
    )
    replace_once(
        "PROJECT_STATUS.md",
        "### Completed in source\n",
        "### Completed in source\n- CSV import header metadata has a dedicated 256-character ceiling and rejects Unicode control/`Format` characters before mapping; fixed malformed cases, a deterministic adversarial corpus, and source-regression guards protect this trust boundary.\n",
    )
    replace_once(
        "docs/NEXT_STEPS.md",
        "- Review parser fuzzing opportunities for CSV, backup archives/header metadata, attachment metadata/storage names, settings JSON, TOTP Base32 input, vault records, and vault-header deserialization.",
        "- Extend parser fuzzing beyond the current deterministic CSV-header adversarial corpus to CSV row/import semantics, backup archives/header metadata, attachment metadata/storage names, settings JSON, TOTP Base32 input, vault records, and vault-header deserialization.",
    )
    replace_once(
        "docs/TEST_PLAN.md",
        "- CSV valid quoting, duplicate/empty headers, unterminated quotes, characters after closing quotes, excessive columns/fields/rows, and malformed-parser corpus coverage. The maximum-column rule must also apply to the final field at newline/EOF, not only delimiter-terminated fields.",
        "- CSV valid quoting, duplicate/empty headers, unterminated quotes, characters after closing quotes, excessive columns/fields/rows, malformed UTF-8, dedicated 256-character header-name bounds, Unicode control/`Format` header rejection, and deterministic adversarial-corpus coverage. The maximum-column rule must also apply to the final field at newline/EOF, not only delimiter-terminated fields, and every accepted adversarial header set must satisfy the same published mapping invariants.",
    )

    ledger = Path("what_changed.md")
    text = ledger.read_text(encoding="utf-8")
    marker = "## 2026-08-15 — CSV import trust-boundary hardening and deterministic adversarial coverage"
    if marker not in text:
        entry = """

## 2026-08-15 — CSV import trust-boundary hardening and deterministic adversarial coverage

- Added a dedicated 256-character ceiling for each imported CSV header name before mapping dictionaries or mapping UI consume it.
- CSV header validation now rejects Unicode control characters and Unicode `Format` category characters, covering NUL/tab/embedded-line-break forms plus invisible and bidirectional formatting controls that could create misleading mapping labels.
- Preserved strict UTF-8 parsing, explicit single initial UTF-8 BOM handling, case-insensitive header uniqueness, 256-column limit, 100,000-row limit, 1,000,000-character field limit, and 2,000,000-character row budget.
- Added boundary tests proving 256-character header names remain accepted and 257-character names are rejected.
- Added fixed malformed-header coverage for NUL, tab, embedded newline, zero-width formatting, and bidirectional formatting characters.
- Added a deterministic 256-case adversarial header corpus using a fixed pseudo-random seed; each generated case must either fail through the public invalid-data boundary or satisfy every published accepted-header invariant.
- Added `CsvSafetySourceTests` assertions so the dedicated header ceiling and control/Unicode-format rejection cannot silently disappear from production source.
- Corrected `docs/formats/CSV_TRANSFER.md` encoding wording to match the implementation: strict UTF-8 without alternate-encoding auto-detection, with one explicitly accepted initial UTF-8 BOM.
- Updated `docs/formats/CSV_TRANSFER.md` and `docs/LIMITS_AND_DEFAULTS.md` with the header trust-boundary rules and dedicated limit.
- Added `docs/verification/CSV_IMPORT_HARDENING_2026_08_15.md`, linked it from the documentation hub, and made documentation source coverage require the record and its audit/current-head caveats.
- Reconciled `docs/TEST_PLAN.md`, `docs/NEXT_STEPS.md`, `PROJECT_STATUS.md`, and `CHANGELOG.md` so the implemented deterministic CSV-header corpus is distinguished from broader parser-fuzzing work that remains open.
- This continuation does not treat deterministic adversarial tests as exhaustive fuzzing or as an independent security audit. Platform file-provider behavior, UI accessibility/layout, packaging/signing, and independent professional review remain separate release gates.
"""
        ledger.write_text(text.rstrip() + entry + "\n", encoding="utf-8", newline="\n")

    Path(".github/workflows/csv-ledger-2026-08-15.yml").unlink()
    Path("build/scripts/reconcile_csv_ledger_20260815.py").unlink()


if __name__ == "__main__":
    main()
