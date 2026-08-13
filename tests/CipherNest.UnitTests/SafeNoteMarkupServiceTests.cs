using CipherNest.Application.Models;
using CipherNest.Application.Services;
using CipherNest.Application.Validation;

namespace CipherNest.UnitTests;

public sealed class SafeNoteMarkupServiceTests
{
    private readonly SafeNoteMarkupService _service = new();

    [Fact]
    public void Parse_RecognizesSupportedSubset_AndNeutralizesHtml()
    {
        var preview = _service.Parse("# Account\n- item\n- [ ] rotate password\n- [x] backup done\n<script>alert(1)</script>");

        Assert.Collection(
            preview.Lines,
            line => { Assert.Equal(SafeNoteLineKind.Heading, line.Kind); Assert.Equal("Account", line.Text); },
            line => { Assert.Equal(SafeNoteLineKind.Bullet, line.Kind); Assert.Equal("item", line.Text); },
            line => { Assert.Equal(SafeNoteLineKind.ChecklistOpen, line.Kind); Assert.Equal("rotate password", line.Text); },
            line => { Assert.Equal(SafeNoteLineKind.ChecklistDone, line.Kind); Assert.Equal("backup done", line.Text); },
            line => { Assert.Equal(SafeNoteLineKind.Paragraph, line.Kind); Assert.DoesNotContain('<', line.Text); Assert.DoesNotContain('>', line.Text); });
    }

    [Fact]
    public void AppendAndToggleChecklist_RoundTrips()
    {
        var markdown = _service.AppendChecklistItem("Notes", "Review recovery copy");
        Assert.Contains("- [ ] Review recovery copy", markdown, StringComparison.Ordinal);

        var toggled = _service.ToggleChecklistItem(markdown, 0);
        Assert.Contains("- [x] Review recovery copy", toggled, StringComparison.Ordinal);

        var reopened = _service.ToggleChecklistItem(toggled, 0);
        Assert.Contains("- [ ] Review recovery copy", reopened, StringComparison.Ordinal);
    }

    [Fact]
    public void AppendChecklistItem_EnforcesCharacterBoundary()
    {
        var maximum = new string('x', SafeNoteLimits.MaximumChecklistItemCharacters);
        var added = _service.AppendChecklistItem(string.Empty, maximum);
        Assert.EndsWith(maximum, added, StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(() =>
            _service.AppendChecklistItem(string.Empty, new string('x', SafeNoteLimits.MaximumChecklistItemCharacters + 1)));
    }

    [Fact]
    public void CodeFence_IsPlainTextOnly()
    {
        var preview = _service.Parse("```html\n<b>secret</b>\n```");
        var line = Assert.Single(preview.Lines);
        Assert.Equal(SafeNoteLineKind.Code, line.Kind);
        Assert.Equal("‹b›secret‹/b›", line.Text);
    }
}
