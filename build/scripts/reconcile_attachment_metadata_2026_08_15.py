from __future__ import annotations

from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]


def run(*args: str) -> None:
    subprocess.run(args, cwd=ROOT, check=True)


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    (ROOT / path).write_text(text, encoding="utf-8")


def replace_once(path: str, old: str, new: str) -> None:
    text = read(path)
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:120]!r}")
    if text.count(old) != 1:
        raise RuntimeError(f"Expected exactly one match in {path}: {old[:120]!r}")
    write(path, text.replace(old, new, 1))


def append_once(path: str, marker: str, section: str) -> None:
    text = read(path)
    if marker in text:
        return
    write(path, text.rstrip() + "\n\n" + section.strip() + "\n")


def commit(path: str, message: str) -> None:
    run("git", "add", path)
    run("git", "diff", "--cached", "--check")
    result = subprocess.run(["git", "diff", "--cached", "--quiet"], cwd=ROOT)
    if result.returncode == 0:
        return
    run("git", "commit", "-m", message)


replace_once(
    "docs/TEST_PLAN.md",
    "- Opaque encrypted attachment storage names must be GUID-based `.cna` names without path separators; malformed names must be rejected before filesystem access.",
    "- Opaque encrypted attachment storage names must be exactly 36-character GUID-N `.cna` names without path separators. The length check must run before stem parsing/allocation, case variants normalize canonically, attachment-ID binding must hold, and malformed/oversized names must be rejected before filesystem access.",
)
commit("docs/TEST_PLAN.md", "docs(test): strengthen opaque attachment-name matrix")

replace_once(
    "docs/TEST_PLAN.md",
    "- Attachment import metadata must be normalized and bounded before encryption begins: display names are leaf filenames no longer than 240 characters, media types no longer than 256 characters, and control characters are rejected. Missing media types normalize to `application/octet-stream`.",
    "- Attachment import metadata must be normalized and bounded before encryption begins: display names become trimmed leaf filenames no longer than 240 UTF-16 code units, media types no longer than 256 UTF-16 code units, malformed UTF-16 is rejected, and rune-aware Unicode `Control`/`Format` characters (including supplementary-plane formatting code points) are rejected. Missing media types normalize to `application/octet-stream`.",
)
commit("docs/TEST_PLAN.md", "docs(test): add rune-aware attachment metadata cases")

replace_once(
    "docs/TEST_PLAN.md",
    "- Attachment metadata must enforce non-empty/size-bounded names and media types, 100 MB plaintext bounds, non-empty identifiers/storage names, control-character rejection, and per-item uniqueness of attachment IDs and storage names.",
    "- Persisted attachment metadata must reuse the canonical `AttachmentImportPolicy` predicates, reject non-leaf/untrimmed display or media metadata, malformed UTF-16, Unicode Control/Format runes, invalid 100 MB plaintext lengths, empty/mismatched identifiers/storage names, and duplicate attachment IDs/storage names. The deterministic 128-case attachment metadata/storage-name corpus must remain fully rejecting.",
)
commit("docs/TEST_PLAN.md", "docs(test): require attachment hostile corpus")

replace_once(
    "docs/NEXT_STEPS.md",
    "- Extend parser fuzzing beyond the current deterministic CSV-header, settings-JSON, backup-header, and vault-header adversarial corpora to CSV row/import semantics, backup ZIP/archive semantics beyond header metadata, attachment metadata/storage names, TOTP Base32 input, and vault-record/envelope semantics.",
    "- Extend parser fuzzing beyond the current deterministic CSV-header, settings-JSON, backup-header, vault-header, and attachment-metadata/storage-name adversarial corpora to CSV row/import semantics, backup ZIP/archive semantics beyond header metadata, TOTP Base32 input, and vault-record/envelope semantics.",
)
commit("docs/NEXT_STEPS.md", "docs(roadmap): mark attachment metadata corpus complete")

replace_once(
    "docs/NEXT_STEPS.md",
    "- Verify opaque storage names accept only GUID `.cna` names without separators before app-data file access.",
    "- Reconfirm opaque storage names accept only exact 36-character GUID-N `.cna` names without separators before app-data file access; verify the early length bound runs before stem parsing and that mismatched attachment IDs fail. Reconfirm persisted display/media metadata rejects malformed UTF-16 plus rune-aware Unicode Control/Format characters.",
)
commit("docs/NEXT_STEPS.md", "docs(roadmap): refine attachment manual validation")

replace_once(
    "PROJECT_STATUS.md",
    "- Attachment import metadata is normalized before encryption: leaf filenames are limited to 240 characters, media types to 256 characters, control characters are rejected, and absent media type defaults to `application/octet-stream`.",
    "- Attachment import/persisted metadata now uses one canonical rune-aware policy: display names are trimmed leaf names bounded to 240 UTF-16 code units, media types to 256, malformed UTF-16 plus Unicode Control/Format runes (including supplementary-plane formatting code points) are rejected, absent media type defaults to `application/octet-stream`, and `VaultItemValidator` reuses the same predicates. Deterministic 128-case hostile metadata/storage-name coverage and source-regression guards protect this boundary.",
)
commit("PROJECT_STATUS.md", "docs(status): record attachment metadata hardening")

replace_once(
    "PROJECT_STATUS.md",
    "- Opaque encrypted attachment storage names are validated as non-empty GUID-based `.cna` filenames without path separators and are bound to the actual attachment ID before filesystem access.",
    "- Opaque encrypted attachment storage names are exactly 36-character GUID-N `.cna` filenames; hostile lengths are rejected before stem parsing/allocation, path separators are rejected, case variants normalize canonically, and names are bound to the actual attachment ID before filesystem access.",
)
commit("PROJECT_STATUS.md", "docs(status): record early opaque-name bound")

replace_once(
    "CHANGELOG.md",
    "- Pre-encryption attachment import metadata normalization/bounds plus control-character rejection.",
    "- Rune-aware attachment metadata normalization/validation with malformed UTF-16 and Unicode Control/Format rejection, canonical persisted-metadata predicate reuse, supplementary-plane coverage, and an exactly 128-input deterministic hostile metadata/storage-name corpus.",
)
commit("CHANGELOG.md", "docs(changelog): add attachment metadata corpus")

replace_once(
    "CHANGELOG.md",
    "- Opaque attachment storage-name validation requiring GUID-based `.cna` names without path separators, plus attachment metadata/uniqueness validation.",
    "- Opaque attachment storage-name validation requiring an exact 36-character GUID-N `.cna` shape, early length rejection before stem parsing, canonical normalization, attachment-ID binding, plus attachment metadata/uniqueness validation.",
)
commit("CHANGELOG.md", "docs(changelog): record opaque-name early bound")

append_once(
    "docs/TESTING_GUIDE.md",
    "## Attachment metadata adversarial boundary — 2026-08-15",
    """
## Attachment metadata adversarial boundary — 2026-08-15

Attachment metadata/storage-name parser regression coverage now includes `AttachmentImportPolicyTests`, `AttachmentStorageNamePolicyTests`, `VaultItemValidatorTests`, `AttachmentMetadataAdversarialTests`, and `AttachmentMetadataSafetySourceTests`.

The deterministic hostile corpus contains exactly 128 inputs across display names, media types, and opaque storage names. It covers ASCII controls, BMP/supplementary Unicode Format characters, malformed isolated UTF-16 surrogates, path separators, dot/whitespace forms, oversized metadata, wrong-length storage names, invalid GUID hex, wrong extensions, and fixed-length separator-bearing names.

This corpus is deterministic regression coverage, not coverage-guided fuzzing or an independent security audit. Device/filesystem validation is still required for OS-specific path, share/export, reparse/link, and cleanup behavior.
""",
)
commit("docs/TESTING_GUIDE.md", "docs(testing): document attachment hostile corpus")

append_once(
    "docs/formats/VAULT_RECORDS.md",
    "## Attachment metadata validation addendum — 2026-08-15",
    """
## Attachment metadata validation addendum — 2026-08-15

`AttachmentReference` values inside decrypted item JSON are validated before records leave the infrastructure boundary. Display names and media types now reuse `AttachmentImportPolicy` persisted-metadata predicates: outer whitespace/non-leaf display forms, malformed UTF-16, and Unicode Control/Format runes are rejected. Opaque storage names remain bound to their attachment IDs and use the exact 36-character GUID-N `.cna` form.

The encrypted vault-record JSON format itself is unchanged by this policy hardening. Existing correctly normalized metadata remains compatible; newly rejected malformed/invisible-format metadata is treated as invalid decrypted record content.
""",
)
commit("docs/formats/VAULT_RECORDS.md", "docs(records): document attachment metadata validation")

append_once(
    "docs/security/THREAT_MODEL.md",
    "### Attachment metadata spoofing/parser boundary — 2026-08-15",
    """
### Attachment metadata spoofing/parser boundary — 2026-08-15

Attachment display/media metadata is attacker-controlled after database/file tampering even though valid records are authenticated. The application now rejects malformed UTF-16 plus Unicode Control/Format runes, including supplementary-plane formatting characters, before treating decrypted attachment metadata as valid. Stored display names must be trimmed leaf names; opaque encrypted filenames are exact GUID-N `.cna` identities and are length-bounded before stem parsing/path construction.

These checks reduce parser/resource and invisible-directional-metadata risk; they do not make arbitrary filesystem/display behavior trustworthy on a compromised OS and do not replace independent review or target-device testing.
""",
)
commit("docs/security/THREAT_MODEL.md", "docs(threat): add attachment metadata spoofing boundary")

append_once(
    "docs/DEVELOPER_GUIDE.md",
    "## Attachment metadata policy rule — 2026-08-15",
    """
## Attachment metadata policy rule — 2026-08-15

Do not add a second attachment display/media metadata validator. Import normalization and decrypted/programmatic item validation must reuse `AttachmentImportPolicy` so rune-aware malformed-UTF-16 and Unicode Control/Format behavior cannot drift. Opaque encrypted storage names remain an Infrastructure filesystem boundary and must pass `AttachmentStorageNamePolicy` before `Path.Combine`/file access.

If the metadata acceptance contract changes, update the attachment/vault-record format docs, limits, deterministic hostile corpus, source-regression tests, threat model, test plan, changelog/status, and verification record in the same candidate.
""",
)
commit("docs/DEVELOPER_GUIDE.md", "docs(dev): codify attachment metadata policy reuse")

append_once(
    "docs/COMPLETE_PROJECT_DOCUMENTATION.md",
    "### Attachment metadata/storage-name hardening — 2026-08-15",
    """
### Attachment metadata/storage-name hardening — 2026-08-15

The current attachment trust boundary uses rune-aware metadata validation shared between import normalization and `VaultItemValidator`. Display names are trimmed leaf names bounded to 240 UTF-16 code units; media types are bounded to 256; malformed UTF-16 and Unicode Control/Format runes are rejected, including supplementary-plane formatting characters. Opaque encrypted storage names are exactly 36-character non-empty GUID-N `.cna` identities and fail length validation before stem parsing/path construction. An exactly 128-input deterministic hostile corpus plus source-regression tests protects these rules.

This does not alter `CNAT0001` framing or constitute an independent security audit. See `formats/ATTACHMENTS.md` and `verification/ATTACHMENT_METADATA_HARDENING_2026_08_15.md`.
""",
)
commit("docs/COMPLETE_PROJECT_DOCUMENTATION.md", "docs(complete): add attachment metadata hardening")

append_once(
    "what_changed.md",
    "### Attachment metadata/storage-name hardening continuation",
    """
### Attachment metadata/storage-name hardening continuation
- Replaced UTF-16 code-unit-only attachment display/media control checks with canonical rune-aware validation using `Rune.DecodeFromUtf16` and `Rune.GetUnicodeCategory`.
- Persisted attachment display/media metadata now rejects malformed UTF-16 plus Unicode `Control` and `Format` runes, including supplementary-plane formatting code points.
- Stored display names must already be trimmed leaf names and cannot be `.`, `..`, or contain `/`/`\\`; media types must already be trimmed. Missing import media type still normalizes to `application/octet-stream`.
- `VaultItemValidator` now reuses `AttachmentImportPolicy.IsValidStoredDisplayName` / `IsValidStoredMediaType` instead of maintaining weaker duplicate checks.
- Opaque encrypted attachment names now require the exact 36-character GUID-N `.cna` shape before stem parsing, avoiding a potentially large substring allocation for hostile oversized names; case variants normalize canonically and attachment-ID binding remains required.
- Added boundary coverage for exact display/media limits, BMP and supplementary-plane Format characters, malformed isolated UTF-16 surrogates, non-leaf/untrimmed persisted metadata, 35/36/37-character storage names, and a one-million-character hostile storage name.
- Added `AttachmentMetadataAdversarialTests` with exactly 128 deterministic hostile display/media/storage-name inputs and `AttachmentMetadataSafetySourceTests` to guard rune-aware validation, canonical policy reuse, and early storage-name length ordering.
- Added `docs/verification/ATTACHMENT_METADATA_HARDENING_2026_08_15.md` and synchronized attachment/record/limits/testing/threat/developer/complete documentation, roadmap, project status, changelog, and documentation-coverage guards.
- This deterministic corpus is regression coverage, not exhaustive coverage-guided fuzzing or an independent professional security audit. Remaining parser/adversarial targets include CSV row/import semantics, backup ZIP/archive semantics beyond header metadata, TOTP Base32 input, and vault-record/envelope semantics.
""",
)
commit("what_changed.md", "docs(ledger): record attachment metadata hardening")

workflow = ROOT / ".github/workflows/attachment-metadata-reconcile-2026-08-15.yml"
script = ROOT / "build/scripts/reconcile_attachment_metadata_2026_08_15.py"
if workflow.exists():
    workflow.unlink()
if script.exists():
    script.unlink()
run("git", "add", "-A", ".github/workflows/attachment-metadata-reconcile-2026-08-15.yml", "build/scripts/reconcile_attachment_metadata_2026_08_15.py")
run("git", "diff", "--cached", "--check")
result = subprocess.run(["git", "diff", "--cached", "--quiet"], cwd=ROOT)
if result.returncode != 0:
    run("git", "commit", "-m", "build(docs): remove attachment reconciliation helper")
