namespace CipherNest.Application.Models;

public enum SafeNoteLineKind
{
    Paragraph,
    Heading,
    Bullet,
    ChecklistOpen,
    ChecklistDone,
    Code
}

public sealed record SafeNotePreviewLine(SafeNoteLineKind Kind, string Text);

public sealed record SafeNotePreview(IReadOnlyList<SafeNotePreviewLine> Lines)
{
    public static SafeNotePreview Empty { get; } = new(Array.Empty<SafeNotePreviewLine>());

    public string ToAccessibleText() => string.Join(
        Environment.NewLine,
        Lines.Select(static line => line.Kind switch
        {
            SafeNoteLineKind.Heading => $"Heading: {line.Text}",
            SafeNoteLineKind.Bullet => $"Bullet: {line.Text}",
            SafeNoteLineKind.ChecklistOpen => $"Unchecked: {line.Text}",
            SafeNoteLineKind.ChecklistDone => $"Checked: {line.Text}",
            SafeNoteLineKind.Code => $"Code: {line.Text}",
            _ => line.Text
        }));
}
