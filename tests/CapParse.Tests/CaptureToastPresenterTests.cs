using CapParse.UI;

namespace CapParse.Tests;

// The toast's state machine (CaptureToastPresenter): the show/replace/
// hide behavior of the transient confirmation, tested without a desktop,
// without a shown window and without a real timer — the presenter is the
// logic the window renders, so these tests pin the behavior the user
// sees: a result is shown, a newer result replaces the older one in
// place, the toast hides, a later result shows it again.
public class CaptureToastPresenterTests
{
    [Fact]
    public void BeforeAnyPresent_TheToastIsHidden_WithNoMessage()
    {
        var presenter = new CaptureToastPresenter();

        Assert.False(presenter.IsVisible);
        Assert.Null(presenter.Current);
        Assert.Equal(string.Empty, presenter.CurrentMessage);
    }

    [Fact]
    public void Present_ShowsTheConfirmation()
    {
        var presenter = new CaptureToastPresenter();

        presenter.Present(new CaptureConfirmation(CaptureConfirmationKind.Success, "Captura pronta"));

        Assert.True(presenter.IsVisible);
        Assert.Equal("Captura pronta", presenter.CurrentMessage);
        Assert.Equal(CaptureConfirmationKind.Success, presenter.Current!.Kind);
    }

    [Fact]
    public void SecondPresent_ReplacesTheCurrentConfirmation_InPlace()
    {
        // One toast, one message at a time: a newer result overwrites the
        // older one — the older message is gone, the toast stays on
        // screen (no second toast, no gap).
        var presenter = new CaptureToastPresenter();

        presenter.Present(new CaptureConfirmation(CaptureConfirmationKind.Success, "Imagem copiada."));
        presenter.Present(new CaptureConfirmation(CaptureConfirmationKind.Success, "Imagem salva."));

        Assert.True(presenter.IsVisible);
        Assert.Equal("Imagem salva.", presenter.CurrentMessage);
        Assert.DoesNotContain("copiada", presenter.CurrentMessage);
    }

    [Fact]
    public void AFailureReplacesASuccessfulResult()
    {
        // The latest result always wins: after a failure the previous
        // success is not left standing as the toast's message.
        var presenter = new CaptureToastPresenter();

        presenter.Present(new CaptureConfirmation(CaptureConfirmationKind.Success, "Imagem copiada."));
        presenter.Present(new CaptureConfirmation(CaptureConfirmationKind.Failure, "Falha na captura"));

        Assert.True(presenter.IsVisible);
        Assert.Equal(CaptureConfirmationKind.Failure, presenter.Current!.Kind);
        Assert.Equal("Falha na captura", presenter.CurrentMessage);
    }

    [Fact]
    public void AutoHide_HidesTheToast_KeepingTheLastMessage()
    {
        var presenter = new CaptureToastPresenter();

        presenter.Present(new CaptureConfirmation(CaptureConfirmationKind.Success, "Captura pronta"));
        presenter.AutoHide();

        Assert.False(presenter.IsVisible);
        // The message is not lost between confirmations: the next present
        // reuses it, and a query right after the hide still answers with
        // the last result.
        Assert.Equal("Captura pronta", presenter.CurrentMessage);
    }

    [Fact]
    public void PresentAfterAutoHide_ShowsTheToastAgain()
    {
        // The same toast is reused, never recreated: a later result
        // brings it back on screen.
        var presenter = new CaptureToastPresenter();

        presenter.Present(new CaptureConfirmation(CaptureConfirmationKind.Success, "Captura pronta"));
        presenter.AutoHide();

        presenter.Present(new CaptureConfirmation(CaptureConfirmationKind.Info, "Nenhum texto foi reconhecido nesta região."));

        Assert.True(presenter.IsVisible);
        Assert.Equal("Nenhum texto foi reconhecido nesta região.", presenter.CurrentMessage);
    }

    [Fact]
    public void Present_NullConfirmation_Throws()
    {
        var presenter = new CaptureToastPresenter();

        Assert.Throws<ArgumentNullException>(() => presenter.Present(null!));
    }
}
