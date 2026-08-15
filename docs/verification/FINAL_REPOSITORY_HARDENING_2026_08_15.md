# Final Repository-Side Hardening Verification — 2026-08-15

This record defines the final repository-side hardening and defect-sweep contract for the August 15, 2026 CipherNest source candidate. It records concrete source defects found and corrected during the final pass, the deterministic regression coverage added for those defects, and the limits of what repository automation can prove.

This document is **not** an independent security audit, penetration test, physical-device certification, store-policy approval, or proof that unknown bugs cannot exist. Device-specific behavior, signing, packaging, store submission, professional security review, and real-device accessibility/performance validation remain separate release gates.

## Scope of the final pass

The final repository-side sweep focused on remaining fully testable trust boundaries and defect patterns after the earlier CSV-header, settings-JSON, backup-header, vault-header, and attachment-metadata hardening work:

- TOTP Base32 normalization/decoding and validity-window arithmetic;
- CSV mapped-row tag materialization;
- encrypted-backup ZIP extraction accounting;
- vault-record/envelope validation review;
- common unfinished/synchronous-blocking markers and repository issue state;
- exact hosted analyzer/test/format/platform/CodeQL verification.

The vault-record/envelope review did not identify an equally concrete defect requiring a breaking format change: stored envelopes are size-bounded before parsing, `CryptoService` validates version/nonce/tag shape, AES-GCM authenticates ciphertext with the row ID as associated data, decrypted plaintext has an independent byte ceiling, the decrypted item ID must equal the authenticated row ID, and `VaultItemValidator` runs before the item leaves the infrastructure boundary. Strict unknown/duplicate-property rejection was therefore not added speculatively during this compatibility-sensitive final pass.

## Defect 1 — TOTP validity-window overflow at the maximum timestamp

### Previous behavior

`TotpService.Generate(...)` computed the next validity boundary with:

```text
DateTimeOffset.FromUnixTimeSeconds(unixSeconds + remaining)
```

At `DateTimeOffset.MaxValue`, the current Unix second is representable, but adding the final remaining second can exceed the representable `DateTimeOffset` range. The TOTP HMAC/code could be generated successfully and then the result construction could throw an out-of-range exception.

### Corrected behavior

The service caches `DateTimeOffset.MaxValue.ToUnixTimeSeconds()` and clamps `ValidUntilUtc` to `DateTimeOffset.MaxValue` when adding the remaining period would exceed that boundary. Ordinary RFC 6238 timestamps retain the existing next-period calculation.

A direct runtime regression test covers `DateTimeOffset.MaxValue` and requires a valid six-digit code, one remaining second for the configured 30-second period, and a clamped maximum validity timestamp.

## Defect 2 — TOTP normalization scratch data outlived the owned operation

`TotpPolicy.NormalizeSecret(...)` previously used an owned mutable `char[]` scratch buffer containing the normalized seed but did not clear that buffer before release.

The scratch array is now cleared in a `finally` block on success and every exception path. This does not claim deterministic erasure of managed input/output `string` instances; .NET strings remain immutable managed objects and are documented as such. The change only narrows lifetime for the mutable temporary buffer owned by the normalization routine.

## TOTP deterministic hostile Base32 corpus

`TotpBase32AdversarialTests` contains exactly 128 deterministic hostile inputs covering:

- empty, whitespace-only, and separator-only inputs;
- normalized secrets below 16 or above 1,024 characters;
- formatted input above 4,096 characters;
- Base32 lengths with impossible remainders;
- invalid/end/midstream padding forms;
- non-zero residual Base32 padding bits;
- invalid digits `0`, `1`, `8`, and `9`;
- punctuation substitutions at every character position;
- Unicode control/format/full-width/non-ASCII characters;
- isolated high and low UTF-16 surrogates.

Each theory row includes an explicit numeric case identifier. This is intentional: an intermediate checkpoint revealed that xUnit generated the same theory test ID for the isolated high- and low-surrogate display values, causing one hostile case to be silently omitted from execution. The corpus was corrected so all 128 inputs are independently discoverable and executable.

Source regression coverage additionally requires the safety ordering:

1. validate algorithm/digits/period;
2. normalize/validate the seed;
3. Base32-decode and validate residual bits;
4. only then construct/execute HMAC work.

It also guards scratch/key/counter/hash cleanup and the maximum-timestamp clamp.

## Defect 3 — CSV mapped tags could cause excessive substring allocation

### Previous behavior

The CSV parser correctly bounded a field to 1,000,000 characters, but mapped Tags values were then processed with `string.Split(..., RemoveEmptyEntries | TrimEntries)`. A hostile field containing a very large number of short comma/semicolon-separated tags could therefore allocate a very large substring array before `VaultItemValidator` rejected the item for exceeding the 100-tag limit.

### Corrected behavior

The tag limits are now canonical public constants on `VaultItemValidator`:

- maximum tags per item: **100**;
- maximum characters per tag: **128 UTF-16 code units**.

CSV import uses a span-based `TrySplitTags(...)` before constructing the `VaultItem`. It:

- scans delimiters without `string.Split`;
- trims each span before materialization;
- ignores empty trimmed segments, preserving existing behavior;
- rejects before materializing the 101st non-empty tag;
- rejects before materializing a tag longer than 128 characters;
- creates at most 100 accepted tag strings.

Integration coverage requires exact-100-tag acceptance, high-cardinality hostile tag rejection without saving an item, and oversized-tag rejection. Source coverage requires the bounded parser to run before `VaultItem` construction and prevents reintroduction of `.Split(...)` in that path.

## Defect 4 — backup ZIP extraction trusted declared lengths without enforcing actual output

### Previous behavior

Restore pre-accounted ZIP archive size using `ZipArchiveEntry.Length`, then extracted an entry with unbounded `CopyToAsync(...)`. The declared uncompressed size was therefore used as a budget but the actual decompressed byte stream was not independently required to stop at that declaration during the copy operation.

### Corrected behavior

`BackupArchivePolicy.CopyEntryExactlyAsync(...)` now owns restore extraction accounting. Before reading it validates that the declared entry length fits the remaining 1 GiB aggregate budget. During streaming it:

- reads through a reusable 128 KiB buffer;
- rejects a read that would push actual output beyond the declared length **before writing that over-limit chunk**;
- requires the final number of copied bytes to equal the declared uncompressed length;
- keeps aggregate accounting overflow-safe through `AddEntryLength(...)`;
- propagates cancellation.

`EncryptedBackupService.ExtractAndValidateArchiveAsync(...)` now uses this bounded exact-copy routine instead of `source.CopyToAsync(...)`.

Unit tests cover exact-length success, expansion rejection before over-limit output is written, truncated-entry rejection, and aggregate-overflow rejection before reading. Source tests require the bounded copy routine and its ordering.

This closes the specific metadata-versus-actual-extraction accounting gap. It does not claim exhaustive protection against every implementation/runtime defect in ZIP decompression or every possible malicious archive; broader coverage-guided archive fuzzing remains a professional/internal review opportunity.

## Defects discovered by the hosted checkpoint

The first hosted checkpoint after the new runtime tests produced two non-runtime defects that were also corrected before finalization:

1. `dotnet format --verify-no-changes` detected a missing final newline in `VaultItemValidator.cs`; the file was corrected to satisfy repository formatting policy.
2. xUnit reported a duplicate theory case ID between the isolated high-surrogate and low-surrogate hostile TOTP inputs. Although the test summary showed no skipped tests, xUnit's discovery diagnostic proved one intended corpus input was not independently executed. Explicit case IDs were added and the next checkpoint executed all 128 hostile rows.

These findings are retained here because final verification must include failures found during verification, not only successful end-state results.

## Defect-sweep observations

The final repository review also checked for common incomplete/error-prone indicators:

- no open GitHub issues were present at the time of the sweep;
- no current production `NotImplementedException` implementation gap was identified by repository search;
- TODO/FIXME/HACK searches did not reveal an unresolved production implementation marker requiring a final code change;
- no synchronous `.Result` usage was found in the searched repository code; the `.Wait(...)` hit was test-only;
- MAUI `async void` usage was reviewed in event/lifecycle contexts rather than rejected mechanically;
- vault-record/envelope decryption/validation ordering was reviewed and left compatibility-preserving as described above.

These searches are supporting evidence only, not a proof that an unknown logical bug cannot exist.

## Checkpoint evidence before documentation freeze

Candidate `483428a0146e5e086a03c9356217139712d1ea1c` completed the corrected core checkpoint successfully:

- Unit: **346 passed, 0 failed, 0 skipped**;
- Integration: **98 passed, 0 failed, 0 skipped**;
- UI/source: **110 passed, 0 failed, 0 skipped**;
- total: **554 passed, 0 failed, 0 skipped**;
- all three test-project analyzer builds: **0 warnings, 0 errors**;
- configured core `dotnet format --verify-no-changes` checks: success.

This checkpoint is not the final immutable-candidate evidence because documentation commits follow it. The exact final `main` head must rerun the complete configured CI and CodeQL gates.

## Documentation freeze synchronization

Before this final direct candidate-contract revision, the canonical documentation suite was synchronized with the completed fixes and test evidence: the documentation hub, TOTP security reference, CSV and encrypted-backup format references, limits/defaults, testing guide, test plan, roadmap, consolidated documentation, project status, changelog, and `what_changed.md` ledger all describe the same final repository-side behavior. `DocumentationCoverageSourceTests` now requires this verification record and its hub link.

The temporary documentation reconciliation script and workflow removed themselves after producing focused permanent commits. This revision is intended to be the final direct repository mutation for the completion candidate; no further source or documentation changes should be made unless exact-head verification exposes a concrete defect. The configured CI and CodeQL gates below must therefore succeed against this revision's exact commit SHA before it can be treated as the repository-side completion candidate.

## Required exact-final-candidate gates

The immutable final source candidate must complete all of the following on the exact same SHA:

- UnitTests analyzer build and tests;
- IntegrationTests analyzer build and tests;
- UiTests/source-regression analyzer build and tests;
- all configured core formatting checks;
- Windows Release analyzer build;
- Windows Release with `CipherNestEnableFundingLink=false`;
- Android Release analyzer build;
- iOS simulator Release analyzer build;
- Mac Catalyst Release analyzer build;
- CodeQL initialization, analyzable core build, MAUI workload setup, analyzable MAUI application build, and analysis.

Any code or documentation commit after that evidence invalidates exact-head status and requires the configured candidate gates again.

## Remaining external release gates

Repository completion does not replace:

- Android/Windows/iOS/Mac Catalyst physical-device and simulator behavior validation where applicable;
- biometric enrollment/cancellation/lockout/platform integration testing;
- screenshot/privacy/clipboard/lifecycle/share/open-in behavior validation on target OS versions;
- real-device accessibility, localization rendering, and performance profiling;
- cross-version backup/migration fixture validation across shipped historical releases;
- independent professional cryptographic/security review;
- dependency/secret scanning evidence tied to the release process where external services are required;
- signing identities, release packaging, provenance, store privacy declarations, store-policy review, and submission.

No repository document should describe those external gates as completed until corresponding evidence exists.
