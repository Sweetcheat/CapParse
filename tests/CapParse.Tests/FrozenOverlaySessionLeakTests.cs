using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using CapParse.Capture;
using CapParse.Platform;
using CapParse.UI;

namespace CapParse.Tests;

/// <summary>
/// Verifies the real cleanup of a FrozenOverlaySession whose construction
/// fails after an overlay window has been created, shown offscreen, painted
/// and placed on its monitor: no window may be left behind (the M11-A
/// orphan-window risk).
///
/// The failure is provoked deterministically through the test-only internal
/// constructor hook, which fires right after the first window is shown and
/// placed - exactly the point at which a pre-fix failure would orphan it.
/// The session creates real, invisible (Opacity = 0) WPF windows, which
/// require an STA thread; WPF windows cannot be created on xUnit's default
/// MTA test threads, so the construction runs on a dedicated STA thread of
/// this same process. The leak check also runs on that thread while it is
/// still alive: when a thread terminates Windows reclaims its windows, so a
/// leaked overlay would disappear from EnumWindows and a post-join check
/// could never see it. The test needs a real desktop; without any monitor
/// there is nothing to verify and it returns early.
/// </summary>
public class FrozenOverlaySessionLeakTests
{
    [Fact]
    public void Constructor_FailsAfterWindowShown_DoesNotLeakOverlayWindows()
    {
        var monitors = MonitorDiscovery.GetMonitors();

        if (monitors.Count == 0)
        {
            return; // headless: nothing to verify
        }

        Exception? constructionException = null;
        var leakedWindows = new List<IntPtr>();

        var sta = new Thread(() =>
        {
            try
            {
                using var bitmap = new Bitmap(10, 10, PixelFormat.Format32bppArgb);
                var capture = new CaptureResult(bitmap, new Rectangle(0, 0, 10, 10));

                // Fail the construction right after the first overlay window
                // has been created, shown and placed on its monitor.
                new FrozenOverlaySession(capture, (_, _) => { }, _ =>
                    throw new InvalidOperationException("injected test failure"));
            }
            catch (Exception ex)
            {
                constructionException = ex;
            }

            // On this (still alive) thread: a leaked overlay would be a
            // visible (WS_VISIBLE) top-level window of this process, even
            // with Opacity = 0.
            leakedWindows = VisibleTopLevelWindows();
        })
        {
            IsBackground = true
        };

        sta.SetApartmentState(ApartmentState.STA);
        sta.Start();
        sta.Join();

        // The construction must have failed at the injected point for this
        // test to be meaningful: the whole point is the cleanup of the
        // window that was already created, shown and placed.
        var failure = Assert.IsType<InvalidOperationException>(constructionException);
        Assert.Equal("injected test failure", failure.Message);

        Assert.Empty(leakedWindows);
    }

    private static List<IntPtr> VisibleTopLevelWindows()
    {
        int pid = Process.GetCurrentProcess().Id;
        var handles = new List<IntPtr>();

        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out int windowPid);

            if (windowPid == pid && IsWindowVisible(hwnd))
            {
                handles.Add(hwnd);
            }

            return true;
        }, IntPtr.Zero);

        return handles;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);
}
