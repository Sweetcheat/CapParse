using CapParse.OCR;

namespace CapParse.Tests;

// M8-B: the run state is the explicit, testable core of the result
// presentation. It is driven by run ids, so a stale (superseded or
// cancelled) run can never overwrite the state of a more recent one, and
// a valid empty result is a distinct, non-error outcome.
public class OcrRunStateTests
{
    private static OcrResult Result(string text)
        => new(text, Array.Empty<OcrBlock>(), TimeSpan.Zero);

    [Fact]
    public void InitialState_IsIdleWithNoTextAndNoCopy()
    {
        var state = new OcrRunState();

        Assert.Equal(OcrPhase.Idle, state.Phase);
        Assert.Empty(state.Text);
        Assert.False(state.CanCopy);
    }

    [Fact]
    public void BeginRun_MovesToProcessingAndDropsPreviousText()
    {
        var state = new OcrRunState();
        var runId = state.BeginRun();
        state.CompleteRun(runId, Result("texto anterior"));

        state.BeginRun();

        Assert.Equal(OcrPhase.Processing, state.Phase);
        Assert.Empty(state.Text);
        Assert.False(state.CanCopy);
    }

    [Fact]
    public void CompletedRunWithText_IsSucceededAndCopiable()
    {
        var state = new OcrRunState();
        var runId = state.BeginRun();

        var completed = state.CompleteRun(runId, Result("linha um\nlinha dois"));

        Assert.True(completed);
        Assert.Equal(OcrPhase.Succeeded, state.Phase);
        Assert.Equal("linha um\nlinha dois", state.Text);
        Assert.True(state.CanCopy);
    }

    [Fact]
    public void CompletedRunWithoutText_IsNoTextAndNotCopiable()
    {
        var state = new OcrRunState();
        var runId = state.BeginRun();

        var completed = state.CompleteRun(runId, Result(string.Empty));

        Assert.True(completed);
        Assert.Equal(OcrPhase.NoText, state.Phase);
        Assert.Empty(state.Text);
        Assert.False(state.CanCopy);
    }

    [Fact]
    public void FailedRun_IsFailedAndNotCopiable()
    {
        var state = new OcrRunState();
        var runId = state.BeginRun();

        var failed = state.FailRun(runId);

        Assert.True(failed);
        Assert.Equal(OcrPhase.Failed, state.Phase);
        Assert.Empty(state.Text);
        Assert.False(state.CanCopy);
    }

    [Fact]
    public void StaleRun_CannotCompleteANewerRun()
    {
        var state = new OcrRunState();
        var staleRun = state.BeginRun();
        var currentRun = state.BeginRun();

        var completed = state.CompleteRun(staleRun, Result("antigo"));

        Assert.False(completed);
        // The newer run is still the one in progress.
        Assert.Equal(OcrPhase.Processing, state.Phase);
        Assert.Empty(state.Text);
    }

    [Fact]
    public void StaleRun_CannotFailANewerRun()
    {
        var state = new OcrRunState();
        var staleRun = state.BeginRun();
        var currentRun = state.BeginRun();

        var failed = state.FailRun(staleRun);

        Assert.False(failed);
        Assert.Equal(OcrPhase.Processing, state.Phase);
    }

    [Fact]
    public void StaleFailure_DoesNotDestroyASucceededState()
    {
        var state = new OcrRunState();
        var firstRun = state.BeginRun();
        state.CompleteRun(firstRun, Result("texto"));

        // A newer run supersedes the one that has text.
        state.BeginRun();

        // A failure reported late by the superseded run must not replace
        // the state of the newer one.
        Assert.False(state.FailRun(firstRun));
        Assert.Equal(OcrPhase.Processing, state.Phase);
    }

    [Fact]
    public void RunIds_NeverRepeat()
    {
        var state = new OcrRunState();

        var first = state.BeginRun();
        var second = state.BeginRun();
        var third = state.BeginRun();

        Assert.True(second > first);
        Assert.True(third > second);
    }

    [Fact]
    public void UnknownRunId_IsRejected()
    {
        var state = new OcrRunState();

        Assert.False(state.CompleteRun(42, Result("x")));
        Assert.False(state.FailRun(42));
        Assert.Equal(OcrPhase.Idle, state.Phase);
    }
}
