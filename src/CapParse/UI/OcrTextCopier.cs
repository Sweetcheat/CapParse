using CapParse.OCR;

namespace CapParse.UI;

/// <summary>
/// The outcome of an attempt to copy the recognized text.
/// </summary>
public sealed record CopyOutcome(bool Succeeded, string Message);

/// <summary>
/// Copies recognized text to the clipboard on demand. Copying is always an
/// explicit user action: the application never copies on its own, and never
/// copies an empty result. Clipboard failures (for example the clipboard
/// locked by another application) are reported as a failed outcome, not
/// thrown.
/// </summary>
public sealed class OcrTextCopier
{
    private readonly ITextClipboard _clipboard;

    public OcrTextCopier(ITextClipboard clipboard)
    {
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
    }

    public CopyOutcome TryCopy(OcrRunState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        if (!state.CanCopy)
        {
            return new CopyOutcome(false, "Não há texto reconhecido para copiar.");
        }

        return TryCopyText(state.Text);
    }

    /// <summary>
    /// Copies an arbitrary recognized text (for example the text of a history
    /// entry) to the clipboard. Empty or whitespace-only text is not copied.
    /// </summary>
    public CopyOutcome TryCopyText(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new CopyOutcome(false, "Não há texto reconhecido para copiar.");
        }

        try
        {
            _clipboard.SetText(text);
            return new CopyOutcome(true, string.Empty);
        }
        catch
        {
            return new CopyOutcome(
                false,
                "Não foi possível copiar o texto. A área de transferência pode estar em uso por outro aplicativo.");
        }
    }
}
