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

    [Fact]
    public void Copy_TransientClipboardLock_IsRetriedAndSucceeds()
    {
        // The clipboard is locked by another application for a moment: the
        // limited retry gives the lock time to clear and the copy
        // succeeds, so a transient lock no longer surfaces as a failure
        // (the reported desktop defect: "Copiar texto" failing right
        // after a mode switch, when a clipboard manager held the lock).
        var clipboard = new FlakyTextClipboard(failuresBeforeSuccess: 2);
        var copier = new OcrTextCopier(clipboard);
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult("texto", Array.Empty<OcrBlock>(), TimeSpan.Zero));

        var outcome = copier.TryCopy(state);

        Assert.True(outcome.Succeeded);
        Assert.Equal("texto", clipboard.LastText);
        Assert.Equal(3, clipboard.Attempts);
    }

    [Fact]
    public void Copy_PersistentClipboardLock_StillFailsWithTheUsualMessage()
    {
        // A lock that outlasts the (few, bounded) retries is still
        // reported as a failed copy, with the same user message as before
        // the retry policy: the policy softens the transient case, it
        // never hides a persistent one.
        var clipboard = new AlwaysLockedTextClipboard();
        var copier = new OcrTextCopier(clipboard);
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult("texto", Array.Empty<OcrBlock>(), TimeSpan.Zero));

        var outcome = copier.TryCopy(state);

        Assert.False(outcome.Succeeded);
        Assert.Equal(
            "Não foi possível copiar o texto. A área de transferência pode estar em uso por outro aplicativo.",
            outcome.Message);
        Assert.Equal(ClipboardWritePolicy.MaxAttempts, clipboard.Attempts);
    }

    [Fact]
    public void Copy_NonClipboardException_FailsOnTheFirstAttempt()
    {
        // A failure that is not the recognized clipboard lock (a real
        // bug) is not retried: the policy must not mask it or delay it.
        var clipboard = new FaultyTextClipboard();
        var copier = new OcrTextCopier(clipboard);
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult("texto", Array.Empty<OcrBlock>(), TimeSpan.Zero));

        var outcome = copier.TryCopy(state);

        Assert.False(outcome.Succeeded);
        Assert.Equal(1, clipboard.Attempts);
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

    /// <summary>
    /// A clipboard that is locked by another application for the first
    /// <c>failuresBeforeSuccess</c> writes and then frees up: the shape
    /// of a transient lock.
    /// </summary>
    private sealed class FlakyTextClipboard : ITextClipboard
    {
        private readonly int _failuresBeforeSuccess;

        public FlakyTextClipboard(int failuresBeforeSuccess)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
        }

        public int Attempts { get; private set; }
        public string? LastText { get; private set; }

        public void SetText(string text)
        {
            Attempts++;
            if (Attempts <= _failuresBeforeSuccess)
            {
                throw new System.Runtime.InteropServices.ExternalException(
                    "The clipboard is in use by another application.");
            }

            LastText = text;
        }
    }

    /// <summary>A clipboard that stays locked for the whole copy attempt.</summary>
    private sealed class AlwaysLockedTextClipboard : ITextClipboard
    {
        public int Attempts { get; private set; }

        public void SetText(string text)
        {
            Attempts++;
            throw new System.Runtime.InteropServices.ExternalException(
                "The clipboard is in use by another application.");
        }
    }

    /// <summary>A clipboard whose write fails for a reason that is not a clipboard lock.</summary>
    private sealed class FaultyTextClipboard : ITextClipboard
    {
        public int Attempts { get; private set; }

        public void SetText(string text)
        {
            Attempts++;
            throw new InvalidOperationException("simulated bug");
        }
    }
}
