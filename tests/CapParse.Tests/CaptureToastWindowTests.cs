using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using CapParse.UI;

namespace CapParse.Tests;

// The transient confirmation window (the application's own toast): the
// tests assert its presentation guarantees on a real window constructed
// on an STA thread — it never steals focus, never appears in the taskbar,
// never receives input, and it is placed explicitly. The window is
// constructed, never shown: showing a WPF window in the (parallel) test
// suite binds it to the process-wide Application owned by other test
// classes, whose shutdown would invalidate the window from another
// thread. The show/fade/auto-hide behavior itself lives in the
// timer-agnostic CaptureToastPresenter (see CaptureToastPresenterTests),
// and the on-screen behavior (fade in/out, the work-area corner, the
// auto-hide timing) is covered by the manual validation.
public class CaptureToastWindowTests
{
    [Fact]
    public void Window_NeverStealsFocus_NeverShowsInTaskbar_NeverReceivesInput()
    {
        RunOnStaThread(() =>
        {
            var window = new CaptureToastWindow();

            // No chrome, no taskbar entry: nothing that could announce
            // itself as a new application window.
            Assert.Equal(WindowStyle.None, window.WindowStyle);
            Assert.True(window.AllowsTransparency);
            Assert.False(window.ShowInTaskbar);

            // It stays above the other windows (a topmost corner toast),
            // but it must never take the foreground from the app the
            // user is working in.
            Assert.True(window.Topmost);
            Assert.False(window.ShowActivated, "the toast must never activate itself or steal focus");
            Assert.False(window.Focusable);

            // It is not in the input path at all: the mouse and keyboard
            // pass through to whatever is behind it.
            Assert.False(window.IsHitTestVisible, "the toast must never capture the mouse or keyboard");

            // It is placed explicitly (the work area corner), never by
            // the OS at a default position.
            Assert.Equal(WindowStartupLocation.Manual, window.WindowStartupLocation);

            window.Close();
        });
    }

    [Fact]
    public void NewWindow_StartsHidden_WithNoMessage()
    {
        // A freshly created window presents nothing: it is invisible and
        // carries no message until the first real result is presented.
        RunOnStaThread(() =>
        {
            var window = new CaptureToastWindow();

            Assert.False(window.IsVisible);
            Assert.Equal(string.Empty, window.CurrentMessage);

            window.Close();
        });
    }

    /// <summary>
    /// Runs <paramref name="action"/> on a dedicated STA thread with a WPF
    /// message pump (WPF UI components require STA). The pump lets the
    /// window process the messages its construction needs; any exception
    /// (including assertion failures) is propagated back to the test
    /// thread.
    /// </summary>
    internal static void RunOnStaThread(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                var frame = new DispatcherFrame();
                dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        // A dispatcher operation swallows callback
                        // exceptions (they only surface through
                        // Dispatcher.UnhandledException, which nothing
                        // subscribes to here): capture the error so the
                        // test thread can rethrow it and the test can fail.
                        error = ex;
                    }
                    finally
                    {
                        frame.Continue = false;
                    }
                });
                Dispatcher.PushFrame(frame);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
