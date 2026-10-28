using CapParse.OCR;

namespace CapParse.UI;

/// <summary>
/// The outcome of an attempt to copy the recognized text.
/// </summary>
public sealed record CopyOutcome(bool Succeeded, string Message);

/// <summary>
/// Copies recognized text to the clipboard on demand. Copying is always an
/// explicit user action: the application never copies on its own, and never
/// copies an empty result.
///
/// The write goes through the application's single text-clipboard channel
/// (the raw Win32 writer, <see cref="IRawTextClipboardWriter"/>), which
/// puts the same <c>CF_UNICODETEXT</c> data the WPF (OLE) clipboard path
/// would put. The WPF path was dropped as a text writer because of the
/// trace-75 evidence: its internal retry loop (10 × 100 ms on the calling
/// thread — WPF .NET 8 source) stacked with an outer retry policy turned
/// a clipboard that could not be opened into a ~3.6 s stall of the UI
/// thread, and both channels (OLE and raw Win32) had failed at the Win32
/// level — so switching between them bought nothing. The bounded
/// open-retry policy now lives in the writer itself, where the failure
/// signature is known (a fast open failure means an unresponsive lock
/// owner — no retry can help); the copier makes exactly one write call
/// and maps its result to a <see cref="CopyOutcome"/>. Success is
/// reported only when the text is really on the clipboard; any failure
/// (clipboard locked, a real bug) is a failed outcome, never thrown. The
/// original exception of a failed copy is recorded in the opt-in
/// CAPPARSE_TRACE log, never shown to the user.
/// </summary>
public sealed class OcrTextCopier
{
    private readonly IRawTextClipboardWriter _writer;

    public OcrTextCopier(IRawTextClipboardWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
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

        // Diagnostics (opt-in CAPPARSE_TRACE): the stage timing of the
        // copy, so a trace can attribute a slow or failed copy to the
        // write. Only durations, exception types and the text length are
        // recorded — never the text itself.
        var thread = Environment.CurrentManagedThreadId;
        var startedAt = Environment.TickCount64;

        try
        {
            // The writer owns the bounded open-retry policy (and the
            // fast-fail stop for an unresponsive lock owner), so this is a
            // single call: there is no outer retry to stack on top of it
            // and block the UI thread for seconds.
            _writer.WriteUtf16Text(text);

            FrozenOverlaySession.Trace(
                $"OcrTextCopier: (thread {thread}) text written after " +
                $"{Environment.TickCount64 - startedAt} ms ({text.Length} chars)");
            return new CopyOutcome(true, string.Empty);
        }
        catch (Exception exception)
        {
            // The user-facing message stays generic; the original
            // exception is recorded in the opt-in CAPPARSE_TRACE log so a
            // real failure can be diagnosed without exposing technical
            // details in the interface.
            FrozenOverlaySession.Trace($"OcrTextCopier: (thread {thread}) {exception}");
            FrozenOverlaySession.Trace(
                $"OcrTextCopier: (thread {thread}) copy failed after " +
                $"{Environment.TickCount64 - startedAt} ms");
            return new CopyOutcome(
                false,
                "Não foi possível copiar o texto. A área de transferência pode estar em uso por outro aplicativo.");
        }
    }
}
