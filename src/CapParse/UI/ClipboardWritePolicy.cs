using System.Runtime.InteropServices;

namespace CapParse.UI;

/// <summary>
/// The limited retry policy for clipboard writes. A clipboard lock by
/// another application is transient — the lock is released as soon as
/// that application finishes its clipboard operation — so a write that
/// fails with the recognized clipboard-lock condition is retried a few
/// times with a short delay, the same shape the WPF clipboard APIs use
/// internally (a bounded wait on the UI thread, never an indefinite one).
/// Only that condition is retried: any other exception fails immediately,
/// so a real bug can never be masked or delayed by the policy.
/// </summary>
internal static class ClipboardWritePolicy
{
    /// <summary>The total number of attempts (the first write plus the retries).</summary>
    internal const int MaxAttempts = 3;

    /// <summary>The wait between two attempts.</summary>
    internal static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromMilliseconds(150);

    /// <summary>
    /// Whether the exception is the recognized transient clipboard-lock
    /// failure: the WPF clipboard APIs report it as an
    /// <see cref="ExternalException"/> (a COM exception) whose message
    /// names the clipboard (for example "The clipboard is in use by
    /// another application"). Anything else — a real bug, an out-of
    /// memory failure, a disposed object — is never retried.
    /// </summary>
    internal static bool IsTransientClipboardFailure(Exception exception)
        => exception is ExternalException external
           && external.Message.Contains("clipboard", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Runs <paramref name="write"/> with the limited retry policy: on a
    /// recognized transient clipboard failure it waits and tries again,
    /// up to <see cref="MaxAttempts"/> attempts in total. A transient
    /// failure that persists after the last attempt, and any other
    /// exception, is rethrown untouched so the caller reports it as a
    /// failed copy. The total wait is bounded (a few hundred
    /// milliseconds at most), so the UI thread can never be blocked
    /// indefinitely.
    /// </summary>
    internal static void WriteWithRetry(
        Action write,
        Func<Exception, bool>? isTransient = null,
        TimeSpan? delay = null)
    {
        ArgumentNullException.ThrowIfNull(write);

        isTransient ??= IsTransientClipboardFailure;
        delay ??= DelayBetweenAttempts;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                write();
                return;
            }
            catch (Exception exception)
            {
                // A transient clipboard lock that still has attempts left
                // falls through to the bounded wait and retries; anything
                // else — a non-clipboard failure, or a transient one that
                // persisted through the last attempt — is rethrown
                // untouched.
                if (!isTransient(exception) || attempt >= MaxAttempts)
                {
                    throw;
                }
            }

            Thread.Sleep(delay.Value);
        }
    }
}
