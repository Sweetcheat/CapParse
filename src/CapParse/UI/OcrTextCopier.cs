using CapParse.OCR;

namespace CapParse.UI;

/// <summary>
/// The outcome of an attempt to copy the recognized text.
/// </summary>
public sealed record CopyOutcome(bool Succeeded, string Message);

/// <summary>
/// Copies the recognized text of an <see cref="OcrRunState"/> to the
/// clipboard on demand. Copying is always an explicit user action: the
/// application never copies on its own, and never copies an empty result.
/// Clipboard failures (for example the clipboard locked by another
/// application) are reported as a failed outcome, not thrown.
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

        try
        {
            _clipboard.SetText(state.Text);
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
