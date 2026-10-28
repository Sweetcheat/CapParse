using System.Runtime.InteropServices;
using CapParse.OCR;
using CapParse.UI;

namespace CapParse.Tests;

// The text-copy contract over the application's single text-clipboard
// channel (OcrTextCopier + IRawTextClipboardWriter), plus the pure
// retry decision of the Win32 writer's open loop. The desktop history:
// the raw Win32 writer used to be a fallback of the WPF (OLE) clipboard
// path, and the trace-75 evidence showed both channels failing at the
// Win32 level — while the WPF path's internal 10 × 100 ms retry loop
// stacked with the outer policy stalled the UI thread for ~3.6 s. The
// writer is now the only channel, and these tests pin its decision
// logic with fakes — no real clipboard, no desktop: success is
// reported only when the text is really on the clipboard, a locked
// clipboard is a failed copy (never a false success), a non-clipboard
// failure (a real bug) is reported without being masked, and the open
// retry is granted only for the failure signatures a retry can fix (the
// trace-75 fast-fail signature is not one of them).
public class OcrTextCopierWriterTests
{
    private static OcrRunState StateWithText(string text)
    {
        var state = new OcrRunState();
        state.CompleteRun(state.BeginRun(), new OcrResult(text, Array.Empty<OcrBlock>(), TimeSpan.Zero));
        return state;
    }

    [Fact]
    public void WriterSucceeds_ReportsSuccess_TheTextIsOnTheClipboard()
    {
        // The common case: the writer writes the text (the success is
        // real — the write only returns after SetClipboardData and
        // CloseClipboard), and the copy is reported as succeeded.
        var writer = new FakeWriter();
        var copier = new OcrTextCopier(writer);

        var outcome = copier.TryCopyText("texto reconhecido");

        Assert.True(outcome.Succeeded);
        Assert.Equal("texto reconhecido", writer.LastText);
        // One write: the writer is invoked once, never a second time.
        Assert.Equal(1, writer.WriteCount);
    }

    [Fact]
    public void WriterSucceeds_TheStateEntryPointReportsTheSameSuccess()
    {
        // The state-based entry point (the main window's copy button)
        // goes through the same writer: the final outcome of the
        // operation is the writer's real result.
        var writer = new FakeWriter();
        var copier = new OcrTextCopier(writer);

        var outcome = copier.TryCopy(StateWithText("linha um"));

        Assert.True(outcome.Succeeded);
        Assert.Equal("linha um", writer.LastText);
    }

    [Fact]
    public void WriterFailsWithAClipboardLock_ReportsFailureWithoutFalseSuccess()
    {
        // The clipboard stayed locked through the writer's whole bounded
        // open budget: the copy is reported as failed (the usual
        // message), never as a success, and the failure is real — no
        // silent degradation.
        var writer = new FailingWriter(
            new ExternalException("OpenClipboard failed (win32 error 5)."));
        var copier = new OcrTextCopier(writer);

        var outcome = copier.TryCopyText("texto reconhecido");

        Assert.False(outcome.Succeeded);
        Assert.Equal(
            "Não foi possível copiar o texto. A área de transferência pode estar em uso por outro aplicativo.",
            outcome.Message);
        Assert.Equal(1, writer.WriteCount);
    }

    [Fact]
    public void WriterFailsWithANonClipboardError_ReportsFailure_NotMasked()
    {
        // A failure that is not a clipboard failure (a real bug in the
        // writer or the OS call chain) is reported as a failed copy: it
        // is never thrown to the caller and never masked as a success.
        var writer = new FailingWriter(new InvalidOperationException("simulated bug"));
        var copier = new OcrTextCopier(writer);

        var outcome = copier.TryCopyText("texto reconhecido");

        Assert.False(outcome.Succeeded);
        Assert.NotEmpty(outcome.Message);
    }

    [Fact]
    public void ThreadWithoutAWindow_TheWindowResolutionYieldsNullHandle()
    {
        // The open's window handle is resolved from the calling thread's
        // own windows: a thread that owns none yields IntPtr.Zero, which
        // keeps the previous NULL-handle open behavior. On the
        // application's UI thread the resolution always finds a window
        // (the hidden hotkey window, the main window). A dedicated thread
        // is used because the test host's own threads may have hosted
        // windows earlier in the run, which would make the assertion
        // flaky.
        IntPtr handle = new IntPtr(int.MinValue);
        var probe = new Thread(
            () => handle = Win32TextClipboard.ResolveCallingThreadWindow())
        {
            IsBackground = true
        };
        probe.Start();
        probe.Join();

        Assert.Equal(IntPtr.Zero, handle);
    }

    // ------------------------------------------------------------------
    // The open-retry decision (Win32TextClipboard.ShouldRetryOpen):
    // pure logic, unit-tested without a desktop.
    // ------------------------------------------------------------------

    [Fact]
    public void ShouldRetryOpen_TransientLock_SlowFailure_WithinBudget_Retries()
    {
        // The open failed only after the system's lock-wait (a live
        // locker held the clipboard for the whole InhibitTimeout): the
        // owner may clear right after, so a short retry is worth it.
        Assert.True(Win32TextClipboard.ShouldRetryOpen(5, 1000, attempt: 1));
        Assert.True(Win32TextClipboard.ShouldRetryOpen(5, 1000, attempt: 2));
    }

    [Fact]
    public void ShouldRetryOpen_TransientLock_FastFailure_DoesNotRetry()
    {
        // The trace-75 signature: the windowed open failed in tens of
        // milliseconds, far below the system's lock-wait time — the
        // lock's owner is not a live process the system can wait for
        // (a dead owner, a broken clipboard-viewer chain). No number of
        // retries can clear that lock: stopping immediately is what
        // removes the multi-second stall.
        Assert.False(Win32TextClipboard.ShouldRetryOpen(5, 10, attempt: 1));
        Assert.False(Win32TextClipboard.ShouldRetryOpen(5, 50, attempt: 1));
    }

    [Fact]
    public void ShouldRetryOpen_FastFailureAtTheBoundary_IsARetry()
    {
        // The boundary itself counts as "the system waited": an open
        // that took at least the fast-fail limit is not the
        // unresponsive-owner signature.
        Assert.True(Win32TextClipboard.ShouldRetryOpen(5, Win32TextClipboard.FastFailLimitMilliseconds, attempt: 1));
    }

    [Fact]
    public void ShouldRetryOpen_TransientLock_BudgetExhausted_DoesNotRetry()
    {
        // The bounded budget is the last line: even a slow transient
        // failure that persists through the last attempt stops here
        // (the caller reports the failed copy), never in a loop.
        Assert.False(Win32TextClipboard.ShouldRetryOpen(5, 1000, attempt: Win32TextClipboard.MaxOpenAttempts));
    }

    [Fact]
    public void ShouldRetryOpen_MomentarilyUnavailableClipboard_SlowFailure_Retries()
    {
        // The other recognized transient shape (win32 error 1418, the
        // clipboard momentarily unavailable) is retried like a lock.
        Assert.True(Win32TextClipboard.ShouldRetryOpen(1418, 500, attempt: 2));
    }

    [Fact]
    public void ShouldRetryOpen_NonTransientError_DoesNotRetry()
    {
        // Any Win32 error that is not the recognized transient lock is a
        // real failure: it is never retried, so a real bug can never be
        // masked or delayed by the policy.
        Assert.False(Win32TextClipboard.ShouldRetryOpen(8, 1000, attempt: 1));
        Assert.False(Win32TextClipboard.ShouldRetryOpen(0, 1000, attempt: 1));
    }

    // ------------------------------------------------------------------
    // Fakes
    // ------------------------------------------------------------------

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

    private sealed class FailingWriter : IRawTextClipboardWriter
    {
        private readonly Exception _failure;

        public FailingWriter(Exception failure)
        {
            _failure = failure;
        }

        public int WriteCount { get; private set; }

        public void WriteUtf16Text(string text)
        {
            WriteCount++;
            throw _failure;
        }
    }
}
