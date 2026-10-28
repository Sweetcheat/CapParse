using CapParse.OCR;
using CapParse.UI;

namespace CapParse.Tests;

// The copy action is explicit, only ever copies the text the
// interface shows, never copies an empty result, makes exactly one
// write call through the application's single text-clipboard channel
// (no outer retry stacked on top of the writer's own bounded policy),
// and reports clipboard failures as outcomes instead of throwing. The
// writer is faked, so none of these checks touch the real system
// clipboard.
public class OcrTextCopierTests
{
    [Fact]
    public void Copy_SucceededState_WritesExactlyTheShownText()
    {
        var writer = new FakeWriter();
        var copier = new OcrTextCopier(writer);
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult("linha um\nlinha dois", Array.Empty<OcrBlock>(), TimeSpan.Zero));

        var outcome = copier.TryCopy(state);

        Assert.True(outcome.Succeeded);
        Assert.Equal("linha um\nlinha dois", writer.LastText);
    }

    [Theory]
    [InlineData(OcrPhase.Idle)]
    [InlineData(OcrPhase.Processing)]
    [InlineData(OcrPhase.NoText)]
    [InlineData(OcrPhase.Failed)]
    public void Copy_WithoutText_DoesNotTouchTheClipboard(OcrPhase phase)
    {
        var writer = new FakeWriter();
        var copier = new OcrTextCopier(writer);
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
        Assert.Equal(0, writer.WriteCount);
    }

    [Fact]
    public void Copy_ClipboardFailure_IsReportedAsAFailedOutcome()
    {
        var writer = new FailingWriter(
            new System.Runtime.InteropServices.ExternalException(
                "OpenClipboard failed (win32 error 5)."));
        var copier = new OcrTextCopier(writer);
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult("texto", Array.Empty<OcrBlock>(), TimeSpan.Zero));

        var outcome = copier.TryCopy(state);

        Assert.False(outcome.Succeeded);
        Assert.NotEmpty(outcome.Message);
        Assert.Equal("texto", state.Text);
    }

    [Fact]
    public void Copy_TransientLockClearedByTheWriter_ReportsSuccess()
    {
        // A transient lock that the writer's own bounded open-retry
        // survives (it fails a few opens and then writes) is a success:
        // the writer is the owner of that policy, and the copier makes
        // exactly one call — it never stacks a second retry on top.
        var writer = new RecurringFailureWriter(failuresBeforeSuccess: 2);
        var copier = new OcrTextCopier(writer);
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult("texto", Array.Empty<OcrBlock>(), TimeSpan.Zero));

        var outcome = copier.TryCopy(state);

        Assert.True(outcome.Succeeded);
        Assert.Equal("texto", writer.LastText);
        Assert.Equal(1, writer.WriteCalls);
    }

    [Fact]
    public void Copy_PersistentClipboardLock_FailsWithTheUsualMessage_InOneCall()
    {
        // A lock that outlasts the writer's (few, bounded) retries is
        // reported as a failed copy, with the same user message as
        // before: the policy softens the transient case, it never hides
        // a persistent one — and the copier does not retry on top of
        // the writer, so the whole failure path stays short (the
        // trace-75 defect: seconds of stacked retries on the UI thread).
        var writer = new FailingWriter(
            new System.Runtime.InteropServices.ExternalException(
                "OpenClipboard failed (win32 error 5)."));
        var copier = new OcrTextCopier(writer);
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult("texto", Array.Empty<OcrBlock>(), TimeSpan.Zero));

        var outcome = copier.TryCopy(state);

        Assert.False(outcome.Succeeded);
        Assert.Equal(
            "Não foi possível copiar o texto. A área de transferência pode estar em uso por outro aplicativo.",
            outcome.Message);
        Assert.Equal(1, writer.WriteCalls);
    }

    [Fact]
    public void Copy_NonClipboardException_IsReportedNotThrown()
    {
        // A failure that is not a clipboard lock (a real bug) is
        // reported as a failed outcome, never thrown to the caller, and
        // never retried by the copier.
        var writer = new FailingWriter(new InvalidOperationException("simulated bug"));
        var copier = new OcrTextCopier(writer);
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult("texto", Array.Empty<OcrBlock>(), TimeSpan.Zero));

        var outcome = copier.TryCopy(state);

        Assert.False(outcome.Succeeded);
        Assert.Equal(1, writer.WriteCalls);
    }

    private sealed class FakeWriter : IRawTextClipboardWriter
    {
        public int WriteCount { get; private set; }
        public string? LastText { get; private set; }

        public void WriteUtf16Text(string text)
        {
            WriteCount++;
            LastText = text;
        }
    }

    /// <summary>
    /// A writer whose open-retry policy sees a clipboard locked by
    /// another application for the first
    /// <c>failuresBeforeSuccess</c> opens and then writes: the shape of
    /// a transient lock, as it happens inside a single
    /// <c>WriteUtf16Text</c> call.
    /// </summary>
    private sealed class RecurringFailureWriter : IRawTextClipboardWriter
    {
        private readonly int _failuresBeforeSuccess;

        public RecurringFailureWriter(int failuresBeforeSuccess)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
        }

        /// <summary>How many times the copier invoked the writer.</summary>
        public int WriteCalls { get; private set; }

        public string? LastText { get; private set; }

        public void WriteUtf16Text(string text)
        {
            WriteCalls++;

            // The writer's own bounded open loop: it fails a few opens
            // and then succeeds within the single call.
            for (var open = 1; ; open++)
            {
                if (open > _failuresBeforeSuccess)
                {
                    LastText = text;
                    return;
                }
            }
        }
    }

    /// <summary>A writer whose write fails for the given reason (and never recovers).</summary>
    private sealed class FailingWriter : IRawTextClipboardWriter
    {
        private readonly Exception _failure;

        public FailingWriter(Exception failure)
        {
            _failure = failure;
        }

        public int WriteCalls { get; private set; }

        public void WriteUtf16Text(string text)
        {
            WriteCalls++;
            throw _failure;
        }
    }
}
