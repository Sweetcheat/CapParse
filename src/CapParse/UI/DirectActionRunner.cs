using System.Drawing;
using CapParse.Capture;
using CapParse.OCR;

namespace CapParse.UI;

/// <summary>The kind of feedback a direct action reports.</summary>
public enum DirectFeedbackKind
{
    /// <summary>The action succeeded; the feedback is a discreet confirmation.</summary>
    Success,

    /// <summary>The action ran and has nothing to act on (for example no
    /// text was recognized): an informational notice, not an error.</summary>
    Info,

    /// <summary>The action could not be completed; the message explains why.</summary>
    Error
}

/// <summary>
/// One feedback item of a direct action: its kind and the user-facing
/// message. The application decides how to present it (the transient
/// toast — the single presentation of the action's result).
/// </summary>
public sealed record DirectFeedback(DirectFeedbackKind Kind, string Message);

/// <summary>
/// Executes a direct capture action once a selection is finished: the
/// action runs immediately, with no result window interaction. It
/// reuses the application's existing services (image copy, text copy)
/// and the application's own recognition, supplied as delegates, so the
/// direct OCR path updates the shared run state and history exactly like
/// the traditional flow — there is no second OCR pipeline. The image
/// save is likewise a delegate: the application composes the configured
/// save policy (ask for a location on every action, or write to the
/// configured folder) and both paths go through the single existing save
/// service. A policy that is configured in the settings is consulted at
/// the moment of the action, so a settings change applies to the next
/// action without any restart.
///
/// A cancelled or empty selection (a null crop) performs no action and
/// reports nothing. Canceling a save dialog is a normal outcome and is
/// likewise silent. Only real results (successes, the "no text" notice
/// and failures) produce feedback, through the caller's callback, which
/// the application points at its own transient confirmation — the single
/// presentation channel of the action's result. This class has
/// no window, no tray and no clipboard of its own: everything goes
/// through the injected services, which is what makes it testable without
/// a desktop.
/// </summary>
public sealed class DirectActionRunner
{
    private const string OcrFailureMessage =
        "Não foi possível reconhecer o texto na região capturada.";
    private const string NoTextMessage =
        "Nenhum texto foi reconhecido nesta região.";
    private const string ImageCopiedMessage = "Imagem copiada.";
    private const string TextCopiedMessage = "Texto copiado.";
    private const string ImageSavedMessage = "Imagem salva.";

    private readonly CropImageCopier _imageCopier;
    private readonly OcrTextCopier _textCopier;
    private readonly Func<Bitmap, int, SaveImageOutcome> _save;
    private readonly Func<Bitmap, Task<OcrRunOutcome>> _recognize;
    private readonly Action<DirectFeedback> _feedback;

    public DirectActionRunner(
        CropImageCopier imageCopier,
        OcrTextCopier textCopier,
        Func<Bitmap, int, SaveImageOutcome> save,
        Func<Bitmap, Task<OcrRunOutcome>> recognize,
        Action<DirectFeedback> feedback)
    {
        _imageCopier = imageCopier ?? throw new ArgumentNullException(nameof(imageCopier));
        _textCopier = textCopier ?? throw new ArgumentNullException(nameof(textCopier));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _recognize = recognize ?? throw new ArgumentNullException(nameof(recognize));
        _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
    }

    /// <summary>
    /// Runs <paramref name="action"/> on <paramref name="crop"/>. A null
    /// crop (the selection was cancelled or had no area) performs nothing
    /// and reports nothing; <see cref="DirectAction.None"/> is a no-op.
    /// </summary>
    public async Task RunAsync(DirectAction action, Bitmap? crop, int dpi)
    {
        if (action == DirectAction.None || crop is null)
        {
            return;
        }

        switch (action)
        {
            case DirectAction.CopyImage:
            {
                var outcome = _imageCopier.TryCopy(crop, dpi);
                ReportCopy(outcome, ImageCopiedMessage);
                break;
            }

            case DirectAction.CopyText:
            {
                // Diagnostics (opt-in CAPPARSE_TRACE): the stage split of
                // the action — how long the recognition took and how long
                // the whole action ran — so a "slow copy text" report can
                // be attributed to the OCR or to the clipboard write.
                var actionStartedAt = Environment.TickCount64;
                var ocrStartedAt = Environment.TickCount64;
                FrozenOverlaySession.Trace(
                    $"DirectActionRunner: CopyText started (crop {crop.Width}x{crop.Height}, dpi {dpi})");
                var run = await _recognize(crop);
                var ocrMs = Environment.TickCount64 - ocrStartedAt;
                FrozenOverlaySession.Trace(
                    $"DirectActionRunner: recognition finished after {ocrMs} ms " +
                    $"(failed: {run.Failed}, has result: {run.Result is not null}, " +
                    $"text chars: {run.Result?.Text.Length ?? 0})");

                if (run.Failed)
                {
                    // A real failure (the shared run state already recorded
                    // it): report it, copy nothing.
                    _feedback(new DirectFeedback(DirectFeedbackKind.Error, OcrFailureMessage));
                    return;
                }

                if (run.Result is null)
                {
                    // Superseded or cancelled by a newer capture: that run
                    // owns the state now, so this one stays silent.
                    return;
                }

                if (string.IsNullOrWhiteSpace(run.Result.Text))
                {
                    // A valid, empty result: inform, and never copy an
                    // empty text.
                    _feedback(new DirectFeedback(DirectFeedbackKind.Info, NoTextMessage));
                    return;
                }

                var outcome = _textCopier.TryCopyText(run.Result.Text);
                FrozenOverlaySession.Trace(
                    $"DirectActionRunner: CopyText finished after " +
                    $"{Environment.TickCount64 - actionStartedAt} ms " +
                    $"(ocr {ocrMs} ms, copy succeeded: {outcome.Succeeded})");
                ReportCopy(outcome, TextCopiedMessage);
                break;
            }

            case DirectAction.SaveImage:
            {
                // crop is non-null here: the null guard at the top of the
                // method already returned for a cancelled selection.
                var outcome = _save(crop!, dpi);

                switch (outcome.Status)
                {
                    case SaveImageStatus.Saved:
                        _feedback(new DirectFeedback(DirectFeedbackKind.Success, ImageSavedMessage));
                        break;

                    // Canceling the dialog is a normal operation: silent.
                    case SaveImageStatus.Canceled:
                        break;

                    case SaveImageStatus.Failed:
                        _feedback(new DirectFeedback(DirectFeedbackKind.Error, outcome.Message));
                        break;
                }

                break;
            }
        }
    }

    private void ReportCopy(CopyOutcome outcome, string successMessage)
    {
        if (outcome.Succeeded)
        {
            _feedback(new DirectFeedback(DirectFeedbackKind.Success, successMessage));
        }
        else
        {
            _feedback(new DirectFeedback(DirectFeedbackKind.Error, outcome.Message));
        }
    }
}
