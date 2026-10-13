using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using CapParse.UI;

namespace CapParse.Tests;

/// <summary>
/// Covers the M14 asynchronous dispatcher passes that replaced the M5
/// synchronous PushFrame pumps in the overlay construction:
///
/// - a live dispatcher processes the pass promptly (one background
///   operation, no manual frame pumping left in the production code);
/// - a window that never renders its first frame times out (bounded),
///   never stalling the capture flow.
///
/// The dispatcher shutdown branch of the passes (the application exiting
/// mid-construction) is a HasShutdownStarted guard that only an actual
/// Application shutdown can produce; it is verified by code review and in
/// the manual validation, not here.
///
/// WPF windows and dispatchers need an STA thread, so both checks run on a
/// dedicated STA thread of this process, exactly like the
/// FrozenOverlaySessionLeakTests harness.
/// </summary>
public class FrozenOverlaySessionPumpTests
{
    [Fact]
    public void PumpDispatcherAsync_LiveDispatcher_CompletesPromptly()
    {
        var result = RunOnStaThread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            DispatcherSynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(dispatcher));

            var stopwatch = Stopwatch.StartNew();

            // Drive the pass by pumping the dispatcher: the background
            // operation posts its completion back to this dispatcher
            // through the synchronization context installed above.
            var task = FrozenOverlaySession.PumpDispatcherAsync();
            var frame = new DispatcherFrame { Continue = true };
            // No custom scheduler: DispatcherSynchronizationContext
            // refuses to act as one, and ending the frame is a plain bool
            // write that is safe from the thread that completes the task.
            task.ContinueWith(_ => frame.Continue = false);
            Dispatcher.PushFrame(frame);
            stopwatch.Stop();

            Assert.True(task.IsCompletedSuccessfully,
                "one dispatcher pass on a live dispatcher must complete");
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2),
                $"one dispatcher pass should complete promptly, took {stopwatch.Elapsed.TotalSeconds:F1}s");
        });

        Assert.Null(result);
    }

    [Fact]
    public void WaitForFirstRenderAsync_WindowNeverRendered_TimesOut()
    {
        var result = RunOnStaThread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            DispatcherSynchronizationContext.SetSynchronizationContext(
                new DispatcherSynchronizationContext(dispatcher));

            // A window that is never shown never fires ContentRendered.
            var window = new Window();

            var stopwatch = Stopwatch.StartNew();

            // Drive the bounded wait by pumping the dispatcher: the
            // Task.Delay continuation of the pass posts back to this
            // dispatcher through the synchronization context installed
            // above.
            var task = FrozenOverlaySession.WaitForFirstRenderAsync(window);
            var frame = new DispatcherFrame { Continue = true };
            task.ContinueWith(_ => frame.Continue = false);
            Dispatcher.PushFrame(frame);

            Assert.True(task.IsFaulted, "the bounded wait must surface a timeout, not complete");
            Assert.IsType<TimeoutException>(task.Exception!.InnerException);

            stopwatch.Stop();
            Assert.True(stopwatch.Elapsed >= TimeSpan.FromSeconds(1),
                $"the timeout should land near its 2s bound, took {stopwatch.Elapsed.TotalSeconds:F1}s");
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(15),
                $"the timeout should not run far past its bound, took {stopwatch.Elapsed.TotalSeconds:F1}s");

            window.Close();
        });

        Assert.Null(result);
    }

    /// <summary>
    /// Runs the action on a dedicated STA thread (WPF requirement) and
    /// returns any exception it threw, or null.
    /// </summary>
    private static Exception? RunOnStaThread(Action action)
    {
        Exception? caught = null;

        var sta = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        })
        {
            IsBackground = true
        };

        sta.SetApartmentState(ApartmentState.STA);
        sta.Start();
        sta.Join();

        return caught;
    }
}
