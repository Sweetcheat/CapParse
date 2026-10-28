namespace CapParse.UI;

/// <summary>
/// The tone of a capture result's confirmation: it drives the visual
/// treatment (the toast's glyph and color) and can never be confused
/// across tones — a failure is never a success.
/// </summary>
public enum CaptureConfirmationKind
{
    /// <summary>The operation completed and produced its result.</summary>
    Success,

    /// <summary>
    /// The operation completed and has nothing to act on (for example no
    /// text was recognized): an informational notice, not an error.
    /// </summary>
    Info,

    /// <summary>The operation could not complete its result.</summary>
    Failure
}

/// <summary>
/// One confirmed result of a capture operation: its tone and the short
/// user-facing message. It is the decision of what to communicate, kept
/// apart from how it is presented: the same confirmation drives the
/// transient toast (the single transient channel) and the tray icon's
/// tooltip (the later query) — so the channels can never disagree about
/// the result.
///
/// A confirmation exists only for a result that is real: a cancelled
/// operation produces none, and a message is only produced after the
/// operation that it describes has actually finished (the copy was
/// written, the file was saved, the capture failed).
/// </summary>
public sealed record CaptureConfirmation(CaptureConfirmationKind Kind, string Message)
{
    /// <summary>
    /// The direct action's feedback as a confirmation: the kinds map one
    /// to one (success and info keep their tone; the action's error is a
    /// failure), and the message is carried over unchanged, so the toast
    /// and the tooltip say exactly what the action reported.
    /// </summary>
    public static CaptureConfirmation FromDirectFeedback(DirectFeedback feedback)
    {
        ArgumentNullException.ThrowIfNull(feedback);

        return new CaptureConfirmation(
            feedback.Kind switch
            {
                DirectFeedbackKind.Success => CaptureConfirmationKind.Success,
                DirectFeedbackKind.Info => CaptureConfirmationKind.Info,
                _ => CaptureConfirmationKind.Failure
            },
            feedback.Message);
    }
}

/// <summary>
/// The decision of what a capture operation's result communicates. Pure
/// (no window, no timer, no thread), so it is testable without a
/// desktop: the traditional flow confirms only a capture that produced a
/// crop ("Captura pronta" — the image is in the result window; nothing
/// is claimed about the OCR, which is still running, or about a file,
/// which that mode never writes), stays silent when the selection was
/// cancelled, and reports a failure as a failure. The direct flow's
/// results arrive as <see cref="DirectFeedback"/> from the runner and are
/// converted with <see cref="CaptureConfirmation.FromDirectFeedback"/>.
/// </summary>
public static class CaptureConfirmationPolicy
{
    /// <summary>The traditional capture's confirmation message.</summary>
    public const string TraditionalReadyMessage = "Captura pronta";

    /// <summary>The message recorded when a capture attempt failed.</summary>
    public const string CaptureFailureMessage = "Falha na captura";

    /// <summary>
    /// The confirmation of a finished traditional capture: the crop
    /// exists means the capture succeeded and the result window owns the
    /// image from that moment; no crop (cancelled or empty selection)
    /// means there is nothing to communicate — no message, ever.
    /// </summary>
    public static CaptureConfirmation? ForTraditionalCapture(bool hasCrop)
        => hasCrop
            ? new CaptureConfirmation(CaptureConfirmationKind.Success, TraditionalReadyMessage)
            : null;

    /// <summary>
    /// The confirmation of a capture attempt that failed (the capture
    /// itself, before any crop exists): always a failure, never a
    /// success, so a failed attempt can never leave a success standing
    /// as the last result.
    /// </summary>
    public static CaptureConfirmation ForFailedCapture()
        => new(CaptureConfirmationKind.Failure, CaptureFailureMessage);
}
