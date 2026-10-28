using CapParse.UI;

namespace CapParse.Tests;

// The decision of what a capture operation's result communicates
// (CaptureConfirmationPolicy) and the direct-feedback-to-confirmation
// conversion (CaptureConfirmation.FromDirectFeedback). Pure logic — no
// window, no timer, no thread: the message that is shown can only ever
// describe an operation that has really finished, and a failure can
// never be dressed as a success.
public class CaptureConfirmationTests
{
    // ------------------------------------------------------------------
    // The traditional flow
    // ------------------------------------------------------------------

    [Fact]
    public void TraditionalCapture_WithACrop_ConfirmsSuccessWithTheReadyMessage()
    {
        // A finished traditional capture with a crop: the image is in the
        // result window from that moment. The message says the capture is
        // ready — it does not claim the OCR (still running) and does not
        // claim a saved file (this mode never writes one).
        var confirmation = CaptureConfirmationPolicy.ForTraditionalCapture(true);

        Assert.NotNull(confirmation);
        Assert.Equal(CaptureConfirmationKind.Success, confirmation!.Kind);
        Assert.Equal("Captura pronta", confirmation.Message);
    }

    [Fact]
    public void TraditionalCapture_CancelledSelection_ConfirmsNothing()
    {
        // No crop (cancelled or empty selection) means the operation
        // produced nothing: there is no message of any kind — a
        // cancellation must never read as a success.
        Assert.Null(CaptureConfirmationPolicy.ForTraditionalCapture(false));
    }

    [Fact]
    public void FailedCapture_IsALwaysAFailure_NeverASuccess()
    {
        var confirmation = CaptureConfirmationPolicy.ForFailedCapture();

        Assert.Equal(CaptureConfirmationKind.Failure, confirmation.Kind);
        Assert.NotEqual(CaptureConfirmationKind.Success, confirmation.Kind);
        Assert.NotEmpty(confirmation.Message);
    }

    [Fact]
    public void TheReadyMessage_NeverClaimsASavedFile()
    {
        // The traditional mode never writes a file: its confirmation
        // must not say the capture was "saved", or it would promise a
        // result that does not exist.
        Assert.DoesNotContain("salva", CaptureConfirmationPolicy.TraditionalReadyMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------
    // Direct feedback -> confirmation
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(DirectFeedbackKind.Success)]
    [InlineData(DirectFeedbackKind.Info)]
    [InlineData(DirectFeedbackKind.Error)]
    public void FromDirectFeedback_KeepsTheMessage_AndMapsTheKind(DirectFeedbackKind kind)
    {
        var confirmation = CaptureConfirmation.FromDirectFeedback(new DirectFeedback(kind, "mensagem"));

        Assert.Equal("mensagem", confirmation.Message);

        var expected = kind switch
        {
            DirectFeedbackKind.Success => CaptureConfirmationKind.Success,
            DirectFeedbackKind.Info => CaptureConfirmationKind.Info,
            _ => CaptureConfirmationKind.Failure
        };
        Assert.Equal(expected, confirmation.Kind);
    }

    [Fact]
    public void FromDirectFeedback_KeepsTheDistinctMessagesSoResultsAreNotConfused()
    {
        // Saving and copying are different operations: the messages the
        // runner reports are carried through unchanged, so a save
        // confirmation can never read as a copy confirmation (and vice
        // versa).
        var saved = CaptureConfirmation.FromDirectFeedback(
            new DirectFeedback(DirectFeedbackKind.Success, "Imagem salva."));
        var copied = CaptureConfirmation.FromDirectFeedback(
            new DirectFeedback(DirectFeedbackKind.Success, "Imagem copiada."));

        Assert.NotEqual(saved.Message, copied.Message);
        Assert.Equal("Imagem salva.", saved.Message);
        Assert.Equal("Imagem copiada.", copied.Message);
    }

    [Fact]
    public void FromDirectFeedback_AFailedAction_IsAFailureConfirmation()
    {
        // A failed action (for example an automatic save that could not
        // write the folder) must surface as a failure, never as the
        // success of the capture that preceded it.
        var confirmation = CaptureConfirmation.FromDirectFeedback(
            new DirectFeedback(DirectFeedbackKind.Error, "Não foi possível salvar a imagem."));

        Assert.Equal(CaptureConfirmationKind.Failure, confirmation.Kind);
        Assert.Equal("Não foi possível salvar a imagem.", confirmation.Message);
    }

    [Fact]
    public void FromDirectFeedback_NullFeedback_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CaptureConfirmation.FromDirectFeedback(null!));
    }
}
