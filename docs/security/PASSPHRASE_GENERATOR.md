# Password and Passphrase Generator

CipherNest uses `System.Security.Cryptography.RandomNumberGenerator` for generated credentials. It does not use `System.Random`, timestamps, device identifiers, or predictable seeds.

## Password mode

Password mode can include lowercase letters, uppercase letters, digits, and symbols. Selected character groups are represented at least once before a cryptographically random Fisher-Yates shuffle. Ambiguous characters can be excluded. Length is bounded to avoid invalid or unexpectedly large values.

## Memorable passphrase mode

Passphrase mode selects independently from a bundled list containing exactly 256 unique lowercase words. The implementation validates the list invariants at startup. Because 256 is 2^8, each independent uniformly selected word contributes 8 bits of random-selection entropy. Eight words therefore represent approximately 64 bits of selection entropy before user edits.

The UI recommends eight or more words for high-value secrets and clearly states that editing a generated result can reduce that estimate. The selectable range is 6–16 words.

The local list is reviewable source data, not a claim that the vocabulary has received an independent linguistic or security audit. A future word-list change should be reviewed for uniqueness, confusing words, offensive content, localization impact, and compatibility with entropy claims.

## Strength estimate

The generic strength label is heuristic. It considers length, character variety, common patterns, predictable sequences, and low character diversity. It is guidance, not a proof of resistance to password cracking.

## Clipboard

Generation does not place a secret on the clipboard automatically. Copy requires explicit user action and uses the configured clipboard-clear interval where the platform permits reliable clearing. Clipboard history or synchronization may retain prior values outside CipherNest's control.
