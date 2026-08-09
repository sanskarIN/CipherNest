# Secure Notes and Safe Preview

CipherNest stores note text inside the encrypted `VaultItem` payload. Note content is decrypted only while the vault is unlocked.

## Supported markup subset

The in-app preview deliberately supports a small, non-HTML subset:

- headings using one to three `#` characters;
- bullet items beginning with `- ` or `* `;
- checklists using `- [ ]` and `- [x]`;
- fenced code blocks;
- ordinary paragraphs.

Raw HTML is not rendered. Angle brackets are neutralized in preview output. The preview has explicit character and line limits so malicious imported text cannot request unbounded work.

## Checklist editing

A checklist item can be appended locally from the item editor. The source remains plaintext Markdown-like text only inside the decrypted item object and is re-encrypted when saved.

## Attachment text preview

Small UTF-8 TXT, Markdown, CSV, JSON, and LOG attachments can be previewed without exporting a plaintext file. CipherNest decrypts at most 512 KiB into an in-memory buffer, validates UTF-8, strips unsafe control characters, neutralizes angle brackets, limits displayed text, and zeroes the owned byte buffer afterward where practical.

The resulting .NET `string` cannot be deterministically erased. This is an explicit managed-runtime limitation.

Other formats are not rendered inside the app. They remain encrypted until the user chooses explicit plaintext export and accepts the export warning.
