using System.Security.Cryptography;
using System.Text;
using CipherNest.App.Services;
using CipherNest.Domain.Models;
using CommunityToolkit.Mvvm.Input;

namespace CipherNest.App.ViewModels;

public partial class ItemEditorViewModel
{
    private const long MaxTextPreviewBytes = 512 * 1024;
    private const int MaxDisplayedPreviewCharacters = 20_000;

    [RelayCommand]
    private async Task PreviewAttachmentAsync(AttachmentReference attachment)
    {
        if (IsReauthenticationRequired || _existing is null || attachment is null) return;
        if (!AttachmentTypePolicy.CanPreview(attachment.MediaType, attachment.DisplayName))
        {
            ErrorMessage = "In-app preview is limited to small TXT, Markdown, CSV, JSON, and LOG attachments. Other formats remain encrypted until you explicitly export them.";
            return;
        }
        if (attachment.PlaintextLength > MaxTextPreviewBytes)
        {
            ErrorMessage = $"In-app text preview is limited to {MaxTextPreviewBytes / 1024:N0} KB to bound decrypted memory use.";
            return;
        }

        IsBusy = true;
        using var buffer = new MemoryStream(capacity: checked((int)Math.Max(0, attachment.PlaintextLength)));
        try
        {
            await _vault.ExportAttachmentAsync(_existing.Id, attachment.Id, buffer);
            if (!buffer.TryGetBuffer(out var segment)) throw new InvalidOperationException("Preview buffer is unavailable.");
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            var text = utf8.GetString(segment.Array!, segment.Offset, checked((int)buffer.Length));
            text = SanitizePreviewText(text);
            if (text.Length > MaxDisplayedPreviewCharacters)
                text = text[..MaxDisplayedPreviewCharacters] + Environment.NewLine + Environment.NewLine + "[Preview truncated. Export explicitly to inspect the complete file.]";
            await Shell.Current.DisplayAlert($"Preview · {attachment.DisplayName}", text.Length == 0 ? "[Empty text file]" : text, "Close");
            ErrorMessage = "Text preview stayed inside CipherNest and did not create a plaintext file. Managed strings cannot be guaranteed to be erased immediately from process memory.";
        }
        catch (DecoderFallbackException)
        {
            ErrorMessage = "This attachment is not valid UTF-8 text, so CipherNest will not preview it as text.";
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException or CryptographicException)
        {
            ErrorMessage = $"Attachment preview failed safely: {ex.Message}";
        }
        finally
        {
            if (buffer.TryGetBuffer(out var segment) && segment.Array is not null)
                CryptographicOperations.ZeroMemory(segment.Array.AsSpan(segment.Offset, segment.Count));
            IsBusy = false;
        }
    }

    private static string SanitizePreviewText(string value)
    {
        var chars = value.ToCharArray();
        for (var index = 0; index < chars.Length; index++)
        {
            var ch = chars[index];
            if (ch is '\r' or '\n' or '\t') continue;
            if (char.IsControl(ch)) chars[index] = '�';
        }
        return new string(chars).Replace('<', '‹').Replace('>', '›');
    }
}
