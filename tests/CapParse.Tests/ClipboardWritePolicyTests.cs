using System.Runtime.InteropServices;
using CapParse.UI;

namespace CapParse.Tests;

// The limited clipboard-write retry policy, in full isolation from
// Windows: which exceptions are worth a retry (only the recognized
// clipboard-lock failure), how many attempts a persistent failure gets,
// and that any other exception fails immediately. The clipboard itself
// is never touched: the "write" is a plain delegate and the delay is
// injected (zero in the tests), so the whole policy runs deterministically
// without the real system clipboard.
public class ClipboardWritePolicyTests
{
    [Fact]
    public void WriteWithRetry_SucceedsOnTheFirstAttempt_MakesExactlyOneCall()
    {
        var calls = 0;

        ClipboardWritePolicy.WriteWithRetry(() => calls++, delay: TimeSpan.Zero);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void WriteWithRetry_TransientFailureThenSuccess_RetriesAndSucceeds()
    {
        var calls = 0;

        ClipboardWritePolicy.WriteWithRetry(
            () =>
            {
                calls++;
                if (calls < 3)
                {
                    throw new ExternalException(
                        "The clipboard is in use by another application.");
                }
            },
            delay: TimeSpan.Zero);

        Assert.Equal(3, calls);
    }

    [Fact]
    public void WriteWithRetry_PersistentTransientFailure_RethrowsAfterTheLastAttempt()
    {
        var calls = 0;
        var exception = new ExternalException(
            "The clipboard is in use by another application.");

        var thrown = Record.Exception(() =>
            ClipboardWritePolicy.WriteWithRetry(
                () =>
                {
                    calls++;
                    throw exception;
                },
                delay: TimeSpan.Zero));

        Assert.Same(exception, thrown);
        Assert.Equal(ClipboardWritePolicy.MaxAttempts, calls);
    }

    [Fact]
    public void WriteWithRetry_NonClipboardException_FailsImmediately()
    {
        var calls = 0;
        var exception = new InvalidOperationException("a real bug");

        var thrown = Record.Exception(() =>
            ClipboardWritePolicy.WriteWithRetry(
                () =>
                {
                    calls++;
                    throw exception;
                },
                delay: TimeSpan.Zero));

        Assert.Same(exception, thrown);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void IsTransientClipboardFailure_MatchesTheRecognizedClipboardLock()
    {
        // The WPF clipboard APIs report the lock as an ExternalException
        // whose message names the clipboard.
        Assert.True(
            ClipboardWritePolicy.IsTransientClipboardFailure(
                new ExternalException("The clipboard is in use by another application.")));

        // A COM-reported clipboard failure is the same shape (COMException
        // derives from ExternalException).
        Assert.True(
            ClipboardWritePolicy.IsTransientClipboardFailure(
                new COMException("clipboard busy", unchecked((int)0x800704C7))));
    }

    [Fact]
    public void IsTransientClipboardFailure_RejectsEverythingElse()
    {
        // A COM exception that is not about the clipboard...
        Assert.False(
            ClipboardWritePolicy.IsTransientClipboardFailure(
                new ExternalException("out of memory")));

        // ...a non-COM exception that merely mentions the clipboard...
        Assert.False(
            ClipboardWritePolicy.IsTransientClipboardFailure(
                new InvalidOperationException("The clipboard is in use")));

        // ...and a plain resource failure.
        Assert.False(
            ClipboardWritePolicy.IsTransientClipboardFailure(
                new OutOfMemoryException()));
    }
}
