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
        "### Changed\n",
        "### Changed\n- CSV header parsing now enforces the dedicated 256-character header ceiling while streaming and classifies unsafe Unicode by rune/code point, including supplementary-plane `Format` characters.\n",
    )
    replace_once(
        "PROJECT_STATUS.md",
        "- CSV import header metadata has a dedicated 256-character ceiling and rejects Unicode control/`Format` characters before mapping; fixed malformed cases, a deterministic adversarial corpus, and source-regression guards protect this trust boundary.",
        "- CSV import header metadata has a dedicated 256-character ceiling enforced during streaming parse and again after parsing, and rune-aware Unicode category checks reject control/`Format` characters including supplementary-plane formatting controls before mapping; fixed malformed cases, a deterministic adversarial corpus, aggregate-row coverage, and source-regression guards protect this trust boundary.",
    )
    replace_once(
        "docs/TEST_PLAN.md",
        "- CSV valid quoting, duplicate/empty headers, unterminated quotes, characters after closing quotes, excessive columns/fields/rows, malformed UTF-8, dedicated 256-character header-name bounds, Unicode control/`Format` header rejection, and deterministic adversarial-corpus coverage. The maximum-column rule must also apply to the final field at newline/EOF, not only delimiter-terminated fields, and every accepted adversarial header set must satisfy the same published mapping invariants.",
        "- CSV valid quoting, duplicate/empty headers, unterminated quotes, characters after closing quotes, excessive columns/fields/rows, malformed UTF-8, dedicated 256-character header-name bounds enforced during header parsing, rune-aware Unicode control/`Format` rejection including supplementary-plane formatting controls, independent aggregate-row-budget coverage, and deterministic adversarial-corpus coverage. The maximum-column rule must also apply to the final field at newline/EOF, not only delimiter-terminated fields, and every accepted adversarial header set must satisfy the same published mapping invariants.",
    )

    ledger = Path("what_changed.md")
    text = ledger.read_text(encoding="utf-8")
    marker = "### CSV streaming/rune-aware follow-up"
    if marker not in text:
        entry = """
### CSV streaming/rune-aware follow-up

- Tightened `CsvParser.ReadRowAsync` so header-preview and real-import header reads pass the dedicated 256-character field ceiling directly into the streaming parser; oversized quoted or unquoted headers now stop accumulating immediately after the limit is exceeded rather than first reaching the generic 1,000,000-character field ceiling.
- Kept the post-parse 256-character header validation as defense in depth and centralized stable oversized-header/field messages.
- Replaced UTF-16-code-unit Unicode-category inspection with `EnumerateRunes()` plus `Rune.GetUnicodeCategory(...)`, so supplementary-plane Unicode `Format` code points cannot bypass the misleading-header rejection rule.
- Extended integration coverage with a supplementary-plane formatting-control case, an oversized quoted-header streaming-bound case, rune-aware accepted-corpus invariants, and a real unlocked-vault import whose multiple individually valid-sized fields exceed the aggregate 2,000,000-character row budget.
- Extended source-regression and verification/format documentation so early streaming enforcement, rune-aware category classification, aggregate-row coverage, and the remaining non-audit/platform limitations are explicit.
"""
        ledger.write_text(text.rstrip() + "\n\n" + entry.strip() + "\n", encoding="utf-8", newline="\n")

    Path(".github/workflows/csv-followup-ledger-2026-08-15.yml").unlink()
    Path("build/scripts/reconcile_csv_followup_20260815.py").unlink()


if __name__ == "__main__":
    main()
