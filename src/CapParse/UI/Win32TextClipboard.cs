using System.Runtime.InteropServices;

namespace CapParse.UI;

/// <summary>
/// Writes text to the system clipboard through the raw Win32 clipboard
/// API. This is the application's single text-write channel: both the
/// direct mode and the traditional mode write text through it (via
/// <see cref="OcrTextCopier"/>). It was promoted from a fallback to the
/// only channel because of the trace-75 evidence: the WPF (OLE) clipboard
/// path's internal retry loop (10 × 100 ms on the calling thread — WPF
/// .NET 8 source, <c>Clipboard.CriticalSetDataObject</c>) stacked with
/// the outer policy turned a clipboard that could not be opened into a
/// ~3.6 s stall of the UI thread, while the raw windowed
/// <c>OpenClipboard</c> — which carries the only tolerance that is
/// useful, the system's own wait for a lock held by a live process
/// (bounded by <c>InhibitTimeout</c>, ~1 s by default) — was never given
/// the chance to do its job. The raw path puts exactly the same
/// <c>CF_UNICODETEXT</c> data the WPF path would put on the clipboard.
/// </summary>
public interface IRawTextClipboardWriter
{
    /// <summary>
    /// Writes <paramref name="text"/> to the clipboard as
    /// <c>CF_UNICODETEXT</c>. The bounded open-retry policy is owned by
    /// the implementation. On success the clipboard takes ownership of
    /// the allocated memory; on failure the memory is released and the
    /// exception escapes. Call on the WPF UI thread, like the rest of the
    /// clipboard writes.
    /// </summary>
    /// <exception cref="ExternalException">
    /// The clipboard could not be opened, emptied, or written.</exception>
    void WriteUtf16Text(string text);
}

/// <summary>
/// <see cref="IRawTextClipboardWriter"/> backed by the Win32 clipboard
/// API: <c>OpenClipboard</c> + <c>EmptyClipboard</c> +
/// <c>SetClipboardData(CF_UNICODETEXT, ...)</c>. The open is done with a
/// window handle from the calling thread (resolved with
/// <c>EnumThreadWindows</c>; the application's UI thread always owns at
/// least one — the hidden hotkey window and the main window survive
/// between captures): with a window, <c>OpenClipboard</c> waits for a
/// lock held by another process to clear (bounded by the system's
/// <c>InhibitTimeout</c>, ~1 s by default) before failing — the same
/// tolerance the WPF path has, in a single native call instead of a
/// 10 × 100 ms poll loop. A NULL handle would fail immediately on a
/// locked clipboard and give up that wait.
///
/// A clipboard lock by another process is transient, so the open is
/// retried a few times with a short bounded delay — but only when the
/// failure signature says a retry can help: a windowed open that fails
/// well below the system's lock-wait time (see
/// <see cref="ShouldRetryOpen"/>) means the lock's owner is not a live
/// process the system can wait for (a dead owner that left the clipboard
/// open, or a broken clipboard-viewer chain). Against that signature no
/// number of retries can clear the lock, so the write stops immediately
/// instead of spending seconds on futile attempts (the trace-75
/// signature: every open failed in tens of milliseconds). Any other
/// Win32 error is a real failure and throws immediately.
/// </summary>
public sealed class Win32TextClipboard : IRawTextClipboardWriter
{
    /// <summary>The CF_UNICODETEXT format (UTF-16 text, trailing NUL).</summary>
    private const uint CfUnicodeText = 13;

    /// <summary>GMEM_MOVEABLE: the only allocation the clipboard accepts.</summary>
    private const uint GmemMoveable = 0x0002;

    /// <summary>
    /// Win32 error of a clipboard locked by another process (transient:
    /// the lock clears as soon as that process finishes its operation).
    /// </summary>
    private const uint Win32ErrorAccessDenied = 5;

    /// <summary>Win32 error of a momentarily unavailable clipboard.</summary>
    private const uint Win32ErrorNoClipboard = 1418;

    internal const int MaxOpenAttempts = 3;

    private static readonly TimeSpan OpenDelay = TimeSpan.FromMilliseconds(150);

    /// <summary>
    /// The boundary between "the system waited for a live lock owner"
    /// (a slow failure, worth a retry: the owner may clear right after
    /// the wait) and "the open gave up without waiting" (a fast failure:
    /// the lock's owner is unresponsive, and no retry can help). Well
    /// below the system's default cross-process lock-wait time
    /// (<c>InhibitTimeout</c>, ~1000 ms).
    /// </summary>
    internal const int FastFailLimitMilliseconds = 100;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumThreadWindows(uint idThread, EnumThreadWindowsProc lpfn, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    /// <summary>
    /// The process id of the process that currently has the clipboard
    /// open, or 0 when no process has it open (the signature of an
    /// unresponsive lock: a dead owner, a broken viewer chain).
    /// </summary>
    [DllImport("user32.dll")]
    private static extern uint GetOpenClipboard();

    private delegate bool EnumThreadWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint format, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    /// <summary>
    /// The pure retry decision of the open loop, extracted for testing:
    /// a failed open is worth another attempt only when it is the
    /// recognized transient lock (access denied, or a momentarily
    /// unavailable clipboard), it failed only after the system's
    /// lock-wait (a fast failure means the lock's owner is unresponsive
    /// and no retry can clear it), and the bounded attempt budget has not
    /// been exhausted.
    /// </summary>
    internal static bool ShouldRetryOpen(int win32Error, long openElapsedMilliseconds, int attempt)
        => (win32Error == Win32ErrorAccessDenied || win32Error == Win32ErrorNoClipboard)
           && openElapsedMilliseconds >= FastFailLimitMilliseconds
           && attempt < MaxOpenAttempts;

    public void WriteUtf16Text(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        // A lock by another process is transient: retry a few times.
        // Any other Win32 error is a real failure and escapes immediately.
        //
        // The open uses a window of the calling thread when one exists
        // (see the class documentation): with a window handle,
        // OpenClipboard waits for a lock held by another process to clear
        // (bounded by the system's InhibitTimeout) before failing — the
        // tolerance a successful copy needs. The application's UI thread
        // always owns a window, so the windowed form is the one that runs
        // in the app; the NULL form is only the last resort for a thread
        // that owns none.
        var window = ResolveCallingThreadWindow();
        var windowLabel = window == IntPtr.Zero ? "none" : $"0x{window.ToInt64():X}";
        var startedAt = Environment.TickCount64;
        var attempt = 0;

        for (;;)
        {
            attempt++;
            var openStartedAt = Environment.TickCount64;

            if (OpenClipboard(window))
            {
                break;
            }

            var openElapsed = Environment.TickCount64 - openStartedAt;
            var error = Marshal.GetLastWin32Error();

            // Diagnostics (opt-in CAPPARSE_TRACE): the per-attempt
            // signature that discriminates a live locker (a slow failure,
            // after the system's lock-wait) from an unresponsive one (a
            // fast failure: a dead owner / a broken clipboard-viewer
            // chain). GetOpenClipboard names the live locker's process
            // (0 when there is no live opener — the unresponsive-lock
            // signature), so a trace can point at the responsible
            // process when the APIs allow it.
            FrozenOverlaySession.Trace(
                $"Win32TextClipboard: open attempt {attempt} failed in {openElapsed} ms " +
                $"(window {windowLabel}, win32 error {error}, " +
                $"clipboard open by pid {GetOpenClipboard()})");

            if (!ShouldRetryOpen(error, openElapsed, attempt))
            {
                throw new ExternalException($"OpenClipboard failed (win32 error {error}).");
            }

            Thread.Sleep(OpenDelay);
        }

        FrozenOverlaySession.Trace(
            $"Win32TextClipboard: open ok on attempt {attempt} " +
            $"(window {windowLabel}, {Environment.TickCount64 - startedAt} ms total)");

        try
        {
            if (!EmptyClipboard())
            {
                throw new ExternalException(
                    $"EmptyClipboard failed (win32 error {Marshal.GetLastWin32Error()}).");
            }

            // CF_UNICODETEXT is a UTF-16 buffer with a trailing NUL,
            // allocated with GlobalAlloc: on success the clipboard takes
            // ownership of the memory (it must never be freed), and on
            // failure the block is released below.
            var hMem = GlobalAlloc(GmemMoveable, new UIntPtr((uint)(text.Length + 1) * 2u));
            if (hMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException(
                    "GlobalAlloc failed for the clipboard text buffer.");
            }

            var pv = GlobalLock(hMem);
            if (pv == IntPtr.Zero)
            {
                GlobalFree(hMem);
                throw new ExternalException(
                    $"GlobalLock failed (win32 error {Marshal.GetLastWin32Error()}).");
            }

            try
            {
                // GlobalAlloc zero-fills the block, so the trailing NUL is
                // already in place; copy the characters before it.
                var chars = text.ToCharArray();
                Marshal.Copy(chars, 0, pv, chars.Length);
            }
            finally
            {
                GlobalUnlock(hMem);
            }

            if (SetClipboardData(CfUnicodeText, hMem) == IntPtr.Zero)
            {
                GlobalFree(hMem);
                throw new ExternalException(
                    $"SetClipboardData failed (win32 error {Marshal.GetLastWin32Error()}).");
            }

            // Success: the clipboard now owns hMem.
            FrozenOverlaySession.Trace(
                $"Win32TextClipboard: write ok ({text.Length} chars, " +
                $"{Environment.TickCount64 - startedAt} ms total)");
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>
    /// The first window owned by the calling thread, or
    /// <see cref="IntPtr.Zero"/> when the thread owns none: the handle a
    /// windowed <c>OpenClipboard</c> waits on a clipboard lock with (see
    /// <see cref="WriteUtf16Text"/>). Resolved on every write — cheap
    /// (a same-thread enumeration) — so a window created after the
    /// instance was constructed is picked up without any wiring.
    /// </summary>
    internal static IntPtr ResolveCallingThreadWindow()
    {
        IntPtr found = IntPtr.Zero;

        // The callback runs on the calling thread; stop at the first
        // window. A thread that owns no window (for example a
        // thread-pool thread in a test) yields IntPtr.Zero, which keeps
        // the previous NULL-handle behavior.
        EnumThreadWindows(
            GetCurrentThreadId(),
            (hWnd, _) =>
            {
                found = hWnd;
                return false;
            },
            IntPtr.Zero);

        return found;
    }
}
