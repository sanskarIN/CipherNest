using CipherNest.Application.Models;

namespace CipherNest.Application.Abstractions;

public interface ISafeNoteMarkupService
{
    SafeNotePreview Parse(string? markdown);
    string AppendChecklistItem(string? markdown, string text);
    string ToggleChecklistItem(string? markdown, int checklistIndex);
}
