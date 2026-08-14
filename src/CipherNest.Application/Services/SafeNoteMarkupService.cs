using CipherNest.Application.Abstractions;
using CipherNest.Application.Models;
using CipherNest.Application.Validation;

namespace CipherNest.Application.Services;

public sealed class SafeNoteMarkupService : ISafeNoteMarkupService
{
    public SafeNotePreview Parse(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return SafeNotePreview.Empty;
        ValidateLength(markdown);

        var lines = SplitLines(markdown);
        if (lines.Length > SafeNoteLimits.MaximumLines) throw new ArgumentException($"Secure note exceeds the {SafeNoteLimits.MaximumLines:N0}-line safety limit.", nameof(markdown));

        var output = new List<SafeNotePreviewLine>(lines.Length);
        var inCodeFence = false;
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
                continue;
            }

            if (inCodeFence)
            {
                output.Add(new SafeNotePreviewLine(SafeNoteLineKind.Code, NeutralizeHtml(line)));
                continue;
            }

            if (trimmed.StartsWith("- [ ] ", StringComparison.OrdinalIgnoreCase))
            {
                output.Add(new SafeNotePreviewLine(SafeNoteLineKind.ChecklistOpen, NeutralizeHtml(trimmed[6..])));
                continue;
            }

            if (trimmed.StartsWith("- [x] ", StringComparison.OrdinalIgnoreCase))
            {
                output.Add(new SafeNotePreviewLine(SafeNoteLineKind.ChecklistDone, NeutralizeHtml(trimmed[6..])));
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                output.Add(new SafeNotePreviewLine(SafeNoteLineKind.Bullet, NeutralizeHtml(trimmed[2..])));
                continue;
            }

            var headingLevel = CountHeadingPrefix(trimmed);
            if (headingLevel is > 0 and <= 3 && trimmed.Length > headingLevel && trimmed[headingLevel] == ' ')
            {
                output.Add(new SafeNotePreviewLine(SafeNoteLineKind.Heading, NeutralizeHtml(trimmed[(headingLevel + 1)..])));
                continue;
            }

            output.Add(new SafeNotePreviewLine(SafeNoteLineKind.Paragraph, NeutralizeHtml(line)));
        }

        return new SafeNotePreview(output);
    }

    public string AppendChecklistItem(string? markdown, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (text.Length > SafeNoteLimits.MaximumChecklistItemCharacters)
            throw new ArgumentException("Checklist item is too long.", nameof(text));

        var cleanText = text.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        if (cleanText.Length == 0) throw new ArgumentException("Checklist item is required.", nameof(text));

        var current = markdown ?? string.Empty;
        ValidateLength(current);
        var separator = current.Length == 0 || current.EndsWith('\n') ? string.Empty : Environment.NewLine;
        var updated = $"{current}{separator}- [ ] {cleanText}";
        ValidateLength(updated);
        if (SafeNoteLimits.ExceedsLineLimit(updated)) throw new ArgumentException($"Secure note exceeds the {SafeNoteLimits.MaximumLines:N0}-line safety limit.", nameof(markdown));
        return updated;
    }

    public string ToggleChecklistItem(string? markdown, int checklistIndex)
    {
        if (checklistIndex < 0) throw new ArgumentOutOfRangeException(nameof(checklistIndex));
        if (string.IsNullOrEmpty(markdown)) throw new ArgumentOutOfRangeException(nameof(checklistIndex));
        ValidateLength(markdown);

        var lines = SplitLines(markdown);
        if (lines.Length > SafeNoteLimits.MaximumLines) throw new ArgumentException($"Secure note exceeds the {SafeNoteLimits.MaximumLines:N0}-line safety limit.", nameof(markdown));
        var found = 0;
        var inCodeFence = false;
        for (var index = 0; index < lines.Length; index++)
        {
            var trimmed = lines[index].TrimStart();
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
                continue;
            }
            if (inCodeFence) continue;

            var indentation = lines[index][..(lines[index].Length - trimmed.Length)];
            var isOpen = trimmed.StartsWith("- [ ] ", StringComparison.OrdinalIgnoreCase);
            var isDone = trimmed.StartsWith("- [x] ", StringComparison.OrdinalIgnoreCase);
            if (!isOpen && !isDone) continue;

            if (found++ != checklistIndex) continue;
            lines[index] = indentation + (isOpen ? "- [x] " : "- [ ] ") + trimmed[6..];
            return string.Join(Environment.NewLine, lines);
        }

        throw new ArgumentOutOfRangeException(nameof(checklistIndex), "Checklist item does not exist.");
    }

    private static string NeutralizeHtml(string value) => value.Replace('<', '‹').Replace('>', '›');

    private static int CountHeadingPrefix(string line)
    {
        var count = 0;
        while (count < line.Length && line[count] == '#') count++;
        return count;
    }

    private static string[] SplitLines(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

    private static void ValidateLength(string value)
    {
        if (value.Length > SafeNoteLimits.MaximumCharacters) throw new ArgumentException($"Secure note exceeds the {SafeNoteLimits.MaximumCharacters:N0}-character safety limit.");
    }
}
