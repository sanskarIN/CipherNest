from pathlib import Path

root = Path(__file__).resolve().parents[2]
path = root / "docs" / "COMPLETE_PROJECT_DOCUMENTATION.md"
text = path.read_text(encoding="utf-8")

old_row = "| Settings JSON | 64 KiB |"
new_row = "| Settings JSON | 64 KiB; actual reads use a 64 KiB + 1 sentinel boundary; maximum nesting depth 16 |"
if new_row not in text:
    if old_row not in text:
        raise RuntimeError("Settings JSON limits row was not found in consolidated documentation.")
    text = text.replace(old_row, new_row, 1)

old_settings = "Settings persistence validates size, normalizes enum/numeric values, restores safe generator defaults, falls back to defaults on malformed/unreadable non-secret settings files, and uses unique sibling staging."
new_settings = "Settings persistence rejects files already above 64 KiB and independently bounds the actual read to a fixed 64 KiB + 1 sentinel byte before bounded-memory JSON deserialization. JSON nesting is capped at 16; invalid UTF-8, over-depth, malformed, or unreadable non-secret settings fall back to defaults, while cancellation continues to propagate. Valid parses are normalized for enum/numeric bounds and safe generator defaults, UTF-8 BOM compatibility is preserved, serialized output is checked against the 64 KiB ceiling, and saves use unique sibling staging."
if new_settings not in text:
    if old_settings not in text:
        raise RuntimeError("Settings persistence paragraph was not found in consolidated documentation.")
    text = text.replace(old_settings, new_settings, 1)

path.write_text(text.rstrip() + "\n", encoding="utf-8")
