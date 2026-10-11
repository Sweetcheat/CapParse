using CapParse.OCR;
using CapParse.UI;

namespace CapParse.Tests;

// M8-B: the copy action is explicit, only ever copies the text the
// interface shows, never copies an empty result, and reports clipboard
// failures as outcomes instead of throwing. The clipboard is faked, so
// none of these checks touch the real system clipboard.
public class OcrTextCopierTests
{
    [Fact]
    public void Copy_SucceededState_WritesExactlyTheShownText()
    {
        var clipboard = new FakeClipboard();
        var copier = new OcrTextCopier(clipboard);
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult("linha um\nlinha dois", Array.Empty<OcrBlock>(), TimeSpan.Zero));

        var outcome = copier.TryCopy(state);

        Assert.True(outcome.Succeeded);
        Assert.Equal("linha um\nlinha dois", clipboard.LastText);
    }

    [Theory]
    [InlineData(OcrPhase.Idle)]
    [InlineData(OcrPhase.Processing)]
    [InlineData(OcrPhase.NoText)]
    [InlineData(OcrPhase.Failed)]
    public void Copy_WithoutText_DoesNotTouchTheClipboard(OcrPhase phase)
    {
        var clipboard = new FakeClipboard();
        var copier = new OcrTextCopier(clipboard);
        var state = new OcrRunState();

        if (phase == OcrPhase.Idle)
        {
            // Already idle.
        }
        else if (phase == OcrPhase.Processing)
        {
            state.BeginRun();
        }
        else if (phase == OcrPhase.NoText)
        {
            var runId = state.BeginRun();
            state.CompleteRun(runId, new OcrResult(string.Empty, Array.Empty<OcrBlock>(), TimeSpan.Zero));
        }
        else
        {
            var runId = state.BeginRun();
            state.FailRun(runId);
        }

        var outcome = copier.TryCopy(state);

        Assert.False(outcome.Succeeded);
        Assert.False(clipboard.Written);
    }

    [Fact]
    public void Copy_ClipboardFailure_IsReportedAsAFailedOutcome()
    {
        var clipboard = new FakeClipboard(throwOnWrite: true);
        var copier = new OcrTextCopier(clipboard);
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult("texto", Array.Empty<OcrBlock>(), TimeSpan.Zero));

        var outcome = copier.TryCopy(state);

        Assert.False(outcome.Succeeded);
        Assert.NotEmpty(outcome.Message);
        Assert.Equal("texto", state.Text);
    }

    private sealed class FakeClipboard : ITextClipboard
    {
        private readonly bool _throwOnWrite;

        public FakeClipboard(bool throwOnWrite = false)
        {
            _throwOnWrite = throwOnWrite;
        }

        public string? LastText { get; private set; }

        public bool Written { get; private set; }

        public void SetText(string text)
        {
            if (_throwOnWrite)
            {
                throw new System.Runtime.InteropServices.ExternalException(
                    "The clipboard is in use by another application.");
            }

            Written = true;
            LastText = text;
        }
    }
}
