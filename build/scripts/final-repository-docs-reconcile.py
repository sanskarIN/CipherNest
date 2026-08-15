from pathlib import Path


def read(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


def write(path: str, text: str) -> None:
    Path(path).write_text(text.rstrip() + "\n", encoding="utf-8")


def append_section(path: str, marker: str, section: str) -> None:
    text = read(path)
    if marker in text:
        return
    write(path, text.rstrip() + "\n\n" + section.strip())


def replace_once(path: str, old: str, new: str) -> None:
    text = read(path)
    if new in text:
        return
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{path}: expected one old fragment, found {count}")
    write(path, text.replace(old, new, 1))


# Documentation hub: keep the final verification record discoverable.
hub = "docs/README.md"
old_hub = "- [`verification/ATTACHMENT_METADATA_HARDENING_2026_08_15.md`](verification/ATTACHMENT_METADATA_HARDENING_2026_08_15.md) — source/test/current-head contract for rune-aware attachment display/media metadata, exact opaque `.cna` storage-name bounds, validator reuse, and the deterministic 128-input hostile corpus."
new_hub = old_hub + "\n- [`verification/FINAL_REPOSITORY_HARDENING_2026_08_15.md`](verification/FINAL_REPOSITORY_HARDENING_2026_08_15.md) — final repository-side defect-sweep contract covering TOTP Base32/lifetime/timestamp fixes, bounded CSV tag materialization, exact backup ZIP extraction accounting, checkpoint-discovered defects, and the remaining external release gates."
replace_once(hub, old_hub, new_hub)

append_section(
    "docs/security/TOTP.md",
    "## Final repository-side TOTP hardening — 2026-08-15",
    """
## Final repository-side TOTP hardening — 2026-08-15

The final repository pass adds three implementation guarantees without changing the RFC 6238 code-generation algorithm:

- the mutable normalization `char[]` owned by `TotpPolicy.NormalizeSecret(...)` is cleared in a `finally` block on success and failure;
- Base32 normalization/decoding still completes before HMAC construction, including impossible-length, supplied-padding, invalid alphabet, and non-zero residual-bit rejection;
- a validity window that would extend beyond `DateTimeOffset.MaxValue` is clamped to `DateTimeOffset.MaxValue` instead of throwing after a valid code has already been computed.

`TotpBase32AdversarialTests` now contains exactly 128 deterministic hostile seeds with explicit numeric case IDs so every row is independently discoverable by xUnit. The corpus covers malformed length/padding, invalid digits and punctuation, Unicode control/format/non-ASCII forms, isolated UTF-16 surrogates, oversized normalized/formatted input, and non-zero residual bits. A source-regression test preserves parser-before-HMAC ordering and cleanup of owned mutable key/counter/hash/scratch buffers.

These cleanups narrow the lifetime of mutable buffers owned by CipherNest. They do **not** make immutable managed `string` copies of a seed deterministically erasable, and they do not provide independent second-factor separation when the TOTP seed and login secret live in the same unlocked vault.
""",
)

append_section(
    "docs/formats/CSV_TRANSFER.md",
    "## Final mapped-tag row hardening — 2026-08-15",
    """
## Final mapped-tag row hardening — 2026-08-15

Mapped Tags values are bounded before `VaultItem` construction. CipherNest reuses the canonical vault-item limits of **100 tags per item** and **128 UTF-16 code units per tag**.

The importer scans comma/semicolon delimiters with spans rather than calling `string.Split(...)` across the entire mapped field. Empty trimmed segments are ignored as before, but import rejects the row before materializing a 101st non-empty tag or a tag longer than 128 characters. At most 100 accepted tag strings are materialized. This prevents a parser-valid field near the general 1,000,000-character field ceiling from forcing hundreds of thousands of tag-substring allocations before normal item validation runs.

Integration coverage includes exact-100-tag acceptance, a 10,000-short-tag hostile row that is skipped without saving an item, and an oversized-tag row that is skipped before item construction. The broader CSV field/row/column ceilings and explicit user mapping rules remain unchanged.
""",
)

append_section(
    "docs/formats/ENCRYPTED_BACKUP.md",
    "## Final ZIP extraction accounting hardening — 2026-08-15",
    """
## Final ZIP extraction accounting hardening — 2026-08-15

Restore no longer treats `ZipArchiveEntry.Length` as sufficient proof of extraction cost. `BackupArchivePolicy.CopyEntryExactlyAsync(...)` validates the declared entry length against the remaining **1 GiB aggregate archive budget before reading**, then streams through a reusable 128 KiB buffer and independently counts actual decompressed output.

The copy rejects an input chunk before writing it if the chunk would make actual output exceed the declared uncompressed length. End-of-entry is accepted only when the actual copied byte count exactly equals the declared length. A shorter stream is rejected as truncated/inconsistent, and aggregate accounting remains overflow-safe through the shared archive policy.

This closes the specific declared-metadata-versus-actual-output accounting gap while preserving the current encrypted backup format and ZIP path/entry-count/attachment-container policies. It is deterministic resource-bound hardening, not a claim of exhaustive ZIP fuzzing or protection against every runtime/decompressor defect.
""",
)

append_section(
    "docs/LIMITS_AND_DEFAULTS.md",
    "## Final parser/extraction bounds synchronization — 2026-08-15",
    """
## Final parser/extraction bounds synchronization — 2026-08-15

The final repository-side hardening reuses and enforces these existing limits earlier at untrusted-input boundaries:

- Tags: at most **100 per vault item** and **128 UTF-16 code units per tag**. CSV mapped Tags parsing enforces these bounds before `VaultItem` construction and materializes at most 100 accepted tag strings.
- TOTP Base32: normalized seed remains **16..1,024 characters**, formatted input remains capped at **4,096 characters**, and the final validity timestamp is clamped at `DateTimeOffset.MaxValue` when the next period boundary is not representable.
- Backup ZIP restore: aggregate uncompressed archive content remains capped at **1 GiB** and entry count at `VaultStorageLimits.MaximumAttachmentCountTotal + 1`; actual extracted bytes must now exactly equal each entry's declared uncompressed length, using a reusable **128 KiB** extraction buffer.

These are resource/safety ceilings, not recommended target sizes for ordinary data.
""",
)

append_section(
    "docs/TESTING_GUIDE.md",
    "## Final repository-side defect-sweep coverage — 2026-08-15",
    """
## Final repository-side defect-sweep coverage — 2026-08-15

The final source-side hardening adds focused regression coverage for three remaining input/resource boundaries:

1. **TOTP Base32 and time boundary** — RFC vectors remain intact; `DateTimeOffset.MaxValue` no longer overflows result construction; normalization scratch storage is cleared; a deterministic 128-case hostile Base32 corpus is fully executable with unique theory case IDs; source tests keep validation/decoding before HMAC work.
2. **CSV mapped tags** — exact 100-tag input is accepted, while high-cardinality and oversized-tag rows are rejected before item construction; source tests prevent reintroduction of whole-field `string.Split(...)` materialization.
3. **Backup ZIP extraction** — unit tests cover exact-length extraction, over-declared expansion rejection before the extra chunk is written, truncated output, and aggregate-budget rejection before source reads; source tests require the exact bounded-copy path.

The first hosted checkpoint also caught a repository-formatting newline violation and an xUnit duplicate-theory-ID diagnostic that caused one intended surrogate corpus case not to execute independently. Both were corrected before the documentation freeze. The corrected checkpoint at `483428a0146e5e086a03c9356217139712d1ea1c` completed 346 Unit, 98 Integration, and 110 UI/source tests: **554 passed, 0 failed, 0 skipped**, with analyzer builds and configured core formatting checks successful.

That checkpoint is historical once later documentation commits exist; release evidence must always be taken from the exact final candidate head.
""",
)

append_section(
    "docs/TEST_PLAN.md",
    "## Final repository-side parser and extraction regression requirements",
    """
## Final repository-side parser and extraction regression requirements

Before treating a final source candidate as repository-clean, additionally require:

- all 128 deterministic hostile TOTP Base32 rows to execute as unique test cases; malformed/oversized/impossible/padded/non-zero-residual inputs must fail before HMAC work, mutable normalization scratch must be cleared, and `DateTimeOffset.MaxValue` must not overflow validity-window construction;
- mapped CSV Tags parsing to enforce the canonical 100-tag/128-character limits before `VaultItem` construction and without whole-field `string.Split(...)`; exact-limit input must remain accepted while high-cardinality/oversized-tag rows are skipped without saving an item;
- backup ZIP restore to validate declared length against the remaining aggregate budget before reading, reject actual output that expands beyond the declared length before writing the over-limit chunk, reject truncated output, and require actual copied bytes to equal each declared uncompressed length;
- any formatter/analyzer/test-discovery diagnostic found during a checkpoint to be treated as a defect even when ordinary pass/fail test totals look green.

The vault-record/envelope boundary remains covered by existing size-before-materialization, envelope shape, AEAD row-ID binding, decrypted-byte ceiling, decrypted-ID equality, and full item-validation gates. A compatibility-breaking strict unknown-property rule must not be added without an explicit format/migration decision and tests.
""",
)

# Roadmap: distinguish completed deterministic work from still-useful deeper fuzzing/external gates.
roadmap = "docs/NEXT_STEPS.md"
old_fuzz = "- Extend parser fuzzing beyond the current deterministic CSV-header, settings-JSON, backup-header, vault-header, and attachment-metadata/storage-name adversarial corpora to CSV row/import semantics, backup ZIP/archive semantics beyond header metadata, TOTP Base32 input, and vault-record/envelope semantics."
new_fuzz = "- Extend coverage-guided/deeper parser fuzzing beyond the current deterministic CSV-header, settings-JSON, backup-header, vault-header, attachment-metadata/storage-name, and **TOTP Base32** hostile corpora. The final repository pass also bounds mapped CSV tag materialization and exact backup ZIP extraction bytes; broader remaining fuzz targets include other CSV row/import semantics, ZIP/archive structures beyond the current path/count/size/exact-copy checks, and vault-record/envelope semantics."
replace_once(roadmap, old_fuzz, new_fuzz)
append_section(
    roadmap,
    "## Final repository-side closure status — 2026-08-15",
    """
## Final repository-side closure status — 2026-08-15

The final repository pass completed the remaining concrete source defects found in TOTP Base32/time handling, CSV mapped-tag materialization, and backup ZIP actual-byte extraction accounting. The corrected pre-freeze checkpoint executed **554/554** tests with clean analyzer builds and formatting. The exact release candidate must still rerun the full configured Windows/Android/Apple CI and CodeQL gates after the documentation freeze.

What remains is intentionally not represented as ordinary repository implementation debt that can be declared complete from hosted compilation alone: target-device behavior, accessibility/localization/performance observation, historical cross-version backup/migration fixtures, independent professional security review, release dependency/secret scanning evidence, signing/provenance, packaging, and store privacy/policy/submission work require the corresponding external environments or reviewers.
""",
)

append_section(
    "docs/COMPLETE_PROJECT_DOCUMENTATION.md",
    "## Final repository-side hardening and defect sweep — 2026-08-15",
    """
## Final repository-side hardening and defect sweep — 2026-08-15

The final source pass closed three concrete remaining resource/correctness gaps without changing CipherNest's cryptographic format versions:

- TOTP normalization clears its owned mutable scratch buffer, malformed Base32 is covered by a uniquely executable 128-case deterministic corpus, and validity-window arithmetic clamps safely at `DateTimeOffset.MaxValue`.
- CSV mapped Tags parsing enforces the canonical 100-tag/128-character limits with a span scan before item construction instead of materializing an unbounded number of split substrings.
- Backup ZIP restore enforces the 1 GiB aggregate budget before entry reads and requires actual decompressed bytes to equal each declared uncompressed entry length; expansion and truncation are rejected through a reusable 128 KiB streaming buffer.

The first hosted checkpoint exposed and led to fixes for a missing source-file final newline and an xUnit duplicate-theory-ID condition that silently prevented one intended hostile-surrogate TOTP row from executing independently. The corrected checkpoint at `483428a0146e5e086a03c9356217139712d1ea1c` recorded **554 passed, 0 failed, 0 skipped**, zero analyzer warnings/errors in the three test builds, and successful core formatting verification.

The vault-record/envelope boundary was reviewed and retains its compatibility-preserving defense stack: pre-materialization storage bounds, crypto version/nonce/tag checks, AES-GCM authentication with row-ID associated data, decrypted-byte bounds, authenticated row/decrypted-item ID equality, and `VaultItemValidator` before records leave infrastructure.

This repository-side completion is not an independent audit and does not replace physical-device/simulator behavior testing, accessibility/performance observation, historical migration/backup compatibility evidence, signing, packaging, store-policy/privacy work, or professional security review.
""",
)

append_section(
    "PROJECT_STATUS.md",
    "## Final repository-side hardening pass — 2026-08-15",
    """
## Final repository-side hardening pass — 2026-08-15

Completed in source/tests:

- fixed TOTP result-window overflow at `DateTimeOffset.MaxValue` and clear the owned normalization scratch buffer on all exits;
- added a deterministic 128-case TOTP Base32 hostile corpus with unique theory case IDs so every intended malformed seed is executed;
- centralized 100-tag/128-character item limits and bound CSV mapped-tag materialization before item construction;
- require backup ZIP actual extracted output to exactly match declared uncompressed lengths while staying inside the shared 1 GiB aggregate budget;
- fixed the checkpoint-discovered missing final newline and duplicate xUnit theory-ID condition;
- reviewed the vault-record/envelope validation chain and preserved the existing compatibility-safe authenticated/bounded validation design.

Corrected pre-documentation checkpoint `483428a0146e5e086a03c9356217139712d1ea1c`: **346 Unit + 98 Integration + 110 UI/source = 554/554 passed**, with zero failed/skipped, clean analyzer builds, and successful configured core formatting verification. This checkpoint becomes historical after documentation commits; exact final-head CI/CodeQL evidence is required before release-candidate claims.

Still external/not proven by repository automation: physical-device security/lifecycle/biometric/clipboard/screenshot behavior, complete accessibility/localization/performance observation, historical release migration/backup compatibility, independent professional security review, signing/package provenance, and store privacy/policy/submission validation.
""",
)

# Changelog: insert concise final-hardening entry under the first Unreleased Changed section.
changelog = "CHANGELOG.md"
old_changed = "### Changed\n"
new_changed = "### Changed\n- Final repository-side hardening now clears the owned TOTP normalization scratch buffer, safely clamps TOTP validity at the maximum representable timestamp, bounds CSV mapped-tag materialization to the canonical 100-tag/128-character policy before item construction, and requires actual backup ZIP extraction bytes to exactly match declared uncompressed lengths within the shared 1 GiB budget. The final defect sweep also corrected a missing source final newline and an xUnit duplicate-theory-ID condition that had prevented one intended hostile TOTP surrogate case from executing independently.\n"
replace_once(changelog, old_changed, new_changed)

append_section(
    "what_changed.md",
    "### Final repository-side defect sweep: TOTP, CSV rows, and backup extraction",
    """
### Final repository-side defect sweep: TOTP, CSV rows, and backup extraction

- Reviewed the remaining fully testable parser/resource boundaries after the earlier CSV-header, settings-JSON, backup-header, vault-header, and attachment-metadata work instead of redoing completed milestones.
- Fixed a TOTP correctness bug at `DateTimeOffset.MaxValue`: code generation could succeed and then `ValidUntilUtc` construction could overflow when adding the remaining period. `TotpService` now clamps the final validity boundary to `DateTimeOffset.MaxValue` only when the next period is not representable.
- Reduced TOTP seed lifetime for owned mutable memory: `TotpPolicy.NormalizeSecret(...)` now clears its temporary normalization `char[]` in `finally`. This does not claim deterministic erasure of immutable managed strings.
- Added `TotpBase32AdversarialTests` with exactly 128 deterministic hostile seeds covering empty/short/oversized input, impossible Base32 lengths, invalid/midstream padding, non-zero residual bits, invalid digits/punctuation, Unicode control/format/non-ASCII values, and isolated UTF-16 surrogates.
- Added explicit numeric IDs to every hostile TOTP theory row after the first hosted checkpoint reported that xUnit assigned the same discovery ID to the isolated high- and low-surrogate display values. Without that correction, one intended adversarial input was not independently executed even though the ordinary test summary reported zero skipped tests.
- Added `TotpSafetySourceTests` to preserve bounds, scratch/key/hash cleanup, validation/decode-before-HMAC ordering, residual-bit checks, and maximum-timestamp handling.
- Fixed CSV mapped Tags resource behavior. The general CSV field bound can be much larger than an item's tag budget, and the old whole-field `string.Split(...)` could allocate a huge number of substrings before validation rejected >100 tags. CSV import now uses a span-based parser that rejects before a 101st non-empty tag or a >128-character tag is materialized.
- Exposed `VaultItemValidator.MaximumTags = 100` and `MaximumTagCharacters = 128` as the canonical limits so CSV import and item validation cannot silently drift apart.
- Added integration coverage for exact-100-tag acceptance, a 10,000-short-tag hostile row that is skipped without saving, and oversized-tag rejection. Added `CsvRowSafetySourceTests` to keep bounded parsing before `VaultItem` construction and prevent reintroduction of whole-field `.Split(...)`.
- Fixed backup ZIP restore accounting. Restore previously budgeted `ZipArchiveEntry.Length` and then used unbounded `CopyToAsync(...)`; actual decompressed output was not required to stop exactly at that declaration during streaming. `BackupArchivePolicy.CopyEntryExactlyAsync(...)` now validates the declared length against the remaining 1 GiB budget before reads, rejects expansion before writing an over-limit chunk, rejects truncated output, and requires actual bytes copied to equal the declared uncompressed length.
- `EncryptedBackupService` now extracts through the exact bounded-copy policy with one reusable 128 KiB buffer. The temporary patch helper self-removed after its focused production commit.
- Added backup policy tests for exact-copy success, over-declared expansion rejection, truncation rejection, and aggregate-overflow rejection before reading; source tests require the bounded extraction path and forbid the old restore-side `source.CopyToAsync(...)` path.
- Reviewed the vault-record/envelope chain. No equally concrete compatibility-safe defect was found: stored envelopes are bounded before materialization, crypto validates version/nonce/tag shape, AES-GCM authenticates against the row ID, decrypted plaintext is independently bounded, decrypted item IDs must match authenticated row IDs, and full item validation runs before records leave infrastructure. No speculative strict-JSON compatibility break was introduced.
- Repository searches during the final sweep found no open GitHub issue, current production `NotImplementedException` gap, TODO/FIXME/HACK implementation marker requiring a final patch, or synchronous `.Result` usage. `async void` lifecycle/event code was reviewed contextually rather than rejected mechanically.
- The first hosted checkpoint compiled all changed tests with 0 warnings/0 errors and passed all runtime tests, but correctly failed the repository on formatting because `VaultItemValidator.cs` lacked its required final newline. That newline was fixed.
- Corrected checkpoint `483428a0146e5e086a03c9356217139712d1ea1c` executed **346 Unit + 98 Integration + 110 UI/source = 554/554 passing**, 0 failed, 0 skipped; all three analyzer builds reported 0 warnings/0 errors and all configured core formatting checks passed.
- Added `docs/verification/FINAL_REPOSITORY_HARDENING_2026_08_15.md` and synchronized the TOTP, CSV, backup-format, limits, testing, roadmap, complete-documentation, status, changelog, and documentation-coverage surfaces.
- This final repository pass does not claim that unknown bugs cannot exist and does not mark external gates as complete. Device behavior, historical migration/backup compatibility, real-device accessibility/performance, independent professional security review, signing/provenance, packaging, and store privacy/policy/submission still require their actual environments/evidence.
""",
)

# Documentation source test: require the final verification record and hub link/disclaimers.
doc_test = "tests/CipherNest.UiTests/DocumentationCoverageSourceTests.cs"
text = read(doc_test)
if '["docs", "verification", "FINAL_REPOSITORY_HARDENING_2026_08_15.md"]' not in text:
    old = '        ["docs", "verification", "ATTACHMENT_METADATA_HARDENING_2026_08_15.md"],\n'
    new = old + '        ["docs", "verification", "FINAL_REPOSITORY_HARDENING_2026_08_15.md"],\n'
    if text.count(old) != 1:
        raise SystemExit("DocumentationCoverageSourceTests.cs: attachment required-doc anchor changed")
    text = text.replace(old, new, 1)
if '"verification/FINAL_REPOSITORY_HARDENING_2026_08_15.md"' not in text:
    old = '                     "verification/ATTACHMENT_METADATA_HARDENING_2026_08_15.md",\n'
    new = old + '                     "verification/FINAL_REPOSITORY_HARDENING_2026_08_15.md",\n'
    if text.count(old) != 1:
        raise SystemExit("DocumentationCoverageSourceTests.cs: hub-link anchor changed")
    text = text.replace(old, new, 1)
if 'var finalVerification = File.ReadAllText' not in text:
    old = '        var attachmentVerification = File.ReadAllText(PathAt("docs", "verification", "ATTACHMENT_METADATA_HARDENING_2026_08_15.md"));\n'
    new = old + '        var finalVerification = File.ReadAllText(PathAt("docs", "verification", "FINAL_REPOSITORY_HARDENING_2026_08_15.md"));\n'
    if text.count(old) != 1:
        raise SystemExit("DocumentationCoverageSourceTests.cs: final verification variable anchor changed")
    text = text.replace(old, new, 1)
if 'Assert.Contains("554 passed", finalVerification' not in text:
    old = '        Assert.Contains("not an independent security audit", attachmentVerification, StringComparison.OrdinalIgnoreCase);\n'
    new = old + '        Assert.Contains("exactly 128 deterministic hostile inputs", finalVerification, StringComparison.OrdinalIgnoreCase);\n        Assert.Contains("554 passed", finalVerification, StringComparison.OrdinalIgnoreCase);\n        Assert.Contains("independent security audit", finalVerification, StringComparison.OrdinalIgnoreCase);\n'
    if text.count(old) != 1:
        raise SystemExit("DocumentationCoverageSourceTests.cs: disclaimer assertion anchor changed")
    text = text.replace(old, new, 1)
write(doc_test, text)
