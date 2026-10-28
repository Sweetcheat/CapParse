using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CapParse.Capture;
using CapParse.Platform;
using Point = System.Drawing.Point;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Key = System.Windows.Input.Key;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace CapParse.UI;

/// <summary>
/// M5 freeze-frame overlay session: one borderless overlay window per
/// monitor, each displaying that monitor's region of the single captured
/// desktop bitmap, dimmed. The user selects a rectangular region by
/// dragging the left mouse button; the drag is tracked in global physical
/// coordinates (via GetCursorPos), so it can cross monitor boundaries.
///
/// The session owns the <see cref="CaptureResult"/>: closing the session
/// (via Esc on any overlay, ending a drag, or <see cref="Dispose"/>)
/// closes every overlay window, releases the WPF images and disposes the
/// captured bitmap. If overlay creation fails partway, the windows that
/// were already created are closed before the failure propagates.
///
/// The <paramref name="onClosed"/> callback receives the final selection in
/// virtual-desktop physical coordinates (null when the capture was
/// cancelled or the selection had no area) and the cropped bitmap of that
/// selection (null in the same cases): the crop is an independent copy,
/// taken while the captured bitmap is still alive, and stays valid after
/// the session disposes it.
///
/// Construction is asynchronous (<see cref="CreateAsync"/>): the steps that
/// need the WPF dispatcher to process window messages (first-frame paint,
/// DPI change, placement) yield the UI thread with an <c>await</c> instead
/// of pumping it synchronously, so the dispatcher keeps servicing input,
/// hotkeys and shutdown between the steps. A close request that arrives
/// before the construction finishes is deferred and applied exactly once
/// when it does; a dispatcher shutdown (the application exiting) aborts
/// the construction, which closes every window it already created and
/// still runs the closed callback.
/// </summary>
public sealed class FrozenOverlaySession : IDisposable
{
    // Safety bounds for the asynchronous dispatcher passes: a missing
    // message or a missing first frame cannot stall the capture flow.
    private static readonly TimeSpan PumpTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan FirstRenderTimeout = TimeSpan.FromSeconds(2);

    private const uint SetWindowPosNoSize = 0x0001;
    private const uint SetWindowPosNoActivate = 0x0010;
    private const uint SetWindowPosNoZOrder = 0x0004;

    private readonly Action<Rectangle?, Bitmap?> _onClosed;
    // Test-only seam: when provided (internal constructor), invoked after the
    // first overlay window has been created, shown and placed, so tests can
    // fail the construction at that point and verify the cleanup of the
    // already-created windows. Always null in production.
    private readonly Action<MonitorOverlayWindow>? _failureHook;
    private readonly List<MonitorOverlayWindow> _windows = new();
    private readonly List<BitmapSource> _images = new();
    private readonly List<Target> _targets = new();
    private CaptureResult? _capture;
    private Point? _dragStart;
    private Rectangle? _selection;
    private bool _closed;
    // False while the constructor is running: input events are ignored and
    // close requests are deferred until the session is fully constructed,
    // so a close cannot tear down a partially built session (which would
    // fire the callback and release the app's capture guard before the
    // constructor finishes, orphaning the windows created afterwards).
    private bool _constructed;
    private bool _closeRequested;
    // Two-stage reveal state: set when the first CompositionTarget.Rendering
    // after the images were assigned has run (the frame containing the
    // images has been handed to the render thread). The overlays are only
    // revealed on the following event, once that frame is in the DWM
    // surface (see RevealOnNextRenderFrame).
    private bool _imageFrameRendered;

    /// <summary>
    /// True once the session has closed: all windows are closed, resources
    /// are released and the closed callback has run.
    /// </summary>
    public bool Closed => _closed;

    /// <summary>
    /// Creates the session and builds its overlays asynchronously on the
    /// calling (UI) thread. The returned task completes when the
    /// construction finishes: on success the session is fully built (or a
    /// close request that arrived during the construction has already run
    /// the closed callback, with <see cref="Closed"/> true), and on failure
    /// (a window that did not paint in time, a dispatcher shutdown, a
    /// monitor enumeration failure) the task faults after every created
    /// window has been closed and the closed callback has run, so the
    /// application's capture guard is always reset.
    /// </summary>
    public static Task<FrozenOverlaySession> CreateAsync(
        CaptureResult capture, Action<Rectangle?, Bitmap?> onClosed)
        => CreateAsync(capture, onClosed, null);

    /// <summary>
    /// Test-only overload: <paramref name="failureHook">, when non-null,
    /// is invoked right after the first overlay window has been created,
    /// shown and placed on its monitor, letting tests fail the construction
    /// at that exact point and verify that the already-created windows are
    /// cleaned up (see FrozenOverlaySessionLeakTests).
    /// </summary>
    internal static async Task<FrozenOverlaySession> CreateAsync(
        CaptureResult capture,
        Action<Rectangle?, Bitmap?> onClosed,
        Action<MonitorOverlayWindow>? failureHook)
    {
        var session = new FrozenOverlaySession(capture, onClosed, failureHook);
        await session.BuildAsync();
        return session;
    }

    private FrozenOverlaySession(
        CaptureResult capture,
        Action<Rectangle?, Bitmap?> onClosed,
        Action<MonitorOverlayWindow>? failureHook)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _onClosed = onClosed ?? throw new ArgumentNullException(nameof(onClosed));
        _failureHook = failureHook;
    }

    /// <summary>
    /// Builds the overlay windows. Runs on the UI thread; every step that
    /// needs the dispatcher to process window messages yields it with an
    /// <c>await</c> (never a synchronous pump), so input, hotkeys and
    /// shutdown keep being serviced between the steps. Any failure closes
    /// every window that was already created and runs the closed callback
    /// before rethrowing, so a failed construction never leaves a window
    /// behind nor skips the callback the application relies on to reset
    /// its capture guard.
    /// </summary>
    private async Task BuildAsync()
    {
        try
        {
            // Set by the constructor: the session instance that runs
            // BuildAsync is the one that was constructed, so this cannot
            // be null here.
            var capture = _capture!;

            // Re-discovers the same monitors that were just captured; the
            // session lives for seconds, so the configuration cannot drift.
            var monitors = MonitorDiscovery.GetMonitors();

            foreach (var monitor in monitors)
            {
                var window = new MonitorOverlayWindow()
                {
                    // Invisible while prepared: the window is shown offscreen,
                    // painted, placed on its monitor and sized, then revealed
                    // exactly once after all overlays are ready.
                    Opacity = 0
                };

                // Registered before any operation that can throw (Show, the
                // dispatcher passes, placement, image creation): CloseCore
                // closes everything in _windows, so a failure partway
                // through construction can never leave a created window
                // orphaned on a monitor. Windows not yet shown are closed
                // the same way; WPF's Close() on a never-shown window just
                // marks it closed.
                _windows.Add(window);

                window.Closed += OnWindowClosed;
                window.KeyDown += OnWindowKeyDown;

                // The window is made visible offscreen first, and its first
                // frame is waited for, before it is moved onto the monitor:
                // from Show() until WPF's render thread paints the window's
                // surface, DWM presents the unpainted surface, which is
                // black. Opacity = 0 does not protect it, because the
                // opacity is only applied by WPF's own rendering. Showing
                // offscreen lets that first (transparent) frame be painted
                // where the user cannot see it, so no black surface is ever
                // presented on a monitor. The window is visible (albeit
                // transparent) during placement, exactly as before, so the
                // PerMonitorV2 DPI nudge in PlaceOnMonitorAsync behaves
                // identically.
                window.Left = -32000;
                window.Top = -32000;
                window.Show();
                await WaitForFirstRenderAsync(window);
                Trace($"{monitor.DeviceName} shown offscreen, first frame painted");
                TraceWindowStates($"{monitor.DeviceName} after offscreen show");

                await PlaceOnMonitorAsync(window, monitor.Bounds);
                Trace($"{monitor.DeviceName} placed");
                TraceWindowStates($"{monitor.DeviceName} after placement");

                _failureHook?.Invoke(window);

                int dpi = (int)Math.Round(VisualTreeHelper.GetDpi(window).DpiScaleX * 96.0);
                var image = CreateMonitorImage(capture.Image, monitor, capture.VirtualBounds, dpi);
                window.SetFrozenImage(image);
                Trace($"{monitor.DeviceName} image set ({dpi} dpi)");

                // The window only reports global physical cursor positions;
                // the session owns the selection state, so the drag logic
                // is not duplicated per window and a drag can cross
                // monitors.
                window.DragStarted = BeginDrag;
                window.DragUpdated = UpdateDrag;
                window.DragEnded = EndDrag;

                _images.Add(image);
                _targets.Add(new Target(window, monitor.Bounds, dpi));
            }

            // The windows stay invisible until their frozen image has been
            // composed (see RevealOnNextRenderFrame).
            RevealOnNextRenderFrame();
            Trace("reveal scheduled");

            // Construction is complete: from now on input events and close
            // requests take effect immediately.
            _constructed = true;

            if (_closeRequested)
            {
                // A close request (Esc, an overlay window closed) arrived
                // while the session was still being built: process it now,
                // with the full window list, so no window is orphaned.
                CloseCore();
            }
        }
        catch
        {
            CloseCore();
            throw;
        }
    }

    /// <summary>
    /// Makes every overlay visible two composition frames after the frozen
    /// images were assigned (black-flash fix).
    ///
    /// Window.Opacity is implemented natively (WS_EX_LAYERED +
    /// SetLayeredWindowAttributes), so the alpha 0 -> 1 switch runs
    /// synchronously on the UI thread, while the image reaches the DWM
    /// surface only when the render thread composes and presents its
    /// frame. The previous single-stage reveal raised the alpha in the
    /// first CompositionTarget.Rendering after the images were set -
    /// i.e. before that frame was composed: when the DWM v-sync landed in
    /// between, it presented the last surface content (black background
    /// plus the dim layer, i.e. black) at full alpha on every monitor -
    /// the observed black flash.
    ///
    /// Two stages remove the race: the first event only records that the
    /// frame containing the images has been rendered; the WPF render loop
    /// schedules the next event only after that frame was presented to
    /// the DWM surface. Raising the alpha on the second event therefore
    /// always presents a surface that already contains the frozen image:
    /// the first visible frame is the correct one.
    ///
    /// The handler is an instance method (not a local function) so
    /// CloseCore can unhook it on every teardown path (Esc, window
    /// closed, Dispose, constructor failure); a stale subscription would
    /// otherwise pin the closed session until the next render frame.
    /// </summary>
    private void RevealOnNextRenderFrame()
    {
        _imageFrameRendered = false;
        CompositionTarget.Rendering += OnRevealRendering;
    }

    private void OnRevealRendering(object? sender, EventArgs e)
    {
        if (_closed)
        {
            // The session was torn down before the reveal completed: drop
            // the subscription, nothing to reveal.
            CompositionTarget.Rendering -= OnRevealRendering;
            return;
        }

        if (!_imageFrameRendered)
        {
            // Stage 1: this event precedes the frame that contains the
            // frozen images; keep the overlays hidden and remember that
            // the frame has been rendered.
            _imageFrameRendered = true;
            Trace("reveal stage 1: image frame rendered");
            return;
        }

        // Stage 2: the image frame is already presented in the DWM
        // surface, so the alpha switch can no longer expose the old
        // (dark) surface content.
        CompositionTarget.Rendering -= OnRevealRendering;

        foreach (var window in _windows)
        {
            window.Opacity = 1;
        }

        Trace("overlays revealed (opacity 1)");
        TraceWindowStates("at reveal");

        // TEMPORARY diagnostics: one snapshot shortly after the reveal,
        // to record the window state of the frames in which the black
        // flash is observed.
        var postReveal = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        postReveal.Tick += (_, _) =>
        {
            postReveal.Stop();
            if (!_closed)
            {
                TraceWindowStates("200ms after reveal");
            }
        };
        postReveal.Start();

        // Take keyboard focus so Esc can end the session. M5-A has no
        // other exit: Esc is the only way out of a freeze-frame session.
        _windows[0].Activate();
    }

    /// <summary>
    /// Yields the UI thread until the window's first frame has actually
    /// been painted by the render thread.
    ///
    /// The await hands the thread back to the dispatcher's message loop,
    /// which keeps processing input and window messages while the render
    /// thread works (the M5 synchronous PushFrame pump is gone). The
    /// ContentRendered event fires on the UI thread after the composition
    /// finishes, so it is the only reliable signal that the window's DWM
    /// surface is no longer the unpainted (black) one. Bounded: a missing
    /// render is surfaced as an exception, never as a silent stall.
    /// </summary>
    internal static async Task WaitForFirstRenderAsync(Window window)
    {
        var rendered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnContentRendered(object? sender, EventArgs e)
        {
            window.ContentRendered -= OnContentRendered;
            rendered.TrySetResult(true);
        }

        window.ContentRendered += OnContentRendered;

        try
        {
            var completed = await Task.WhenAny(rendered.Task, Task.Delay(FirstRenderTimeout));

            if (completed != rendered.Task)
            {
                throw new TimeoutException(
                    "The overlay window did not render its first frame in time; " +
                    "its unpainted (black) surface could be presented on the monitor.");
            }
        }
        finally
        {
            window.ContentRendered -= OnContentRendered;
        }
    }

    // TEMPORARY diagnostics for the black-flash investigation: when the
    // CAPPARSE_TRACE environment variable points to a file, the capture
    // timeline (show, first render, placement, image set, reveal) is
    // appended to it with millisecond timestamps. Remove once the fix is
    // validated manually.
    internal static void Trace(string message)
    {
        var path = Environment.GetEnvironmentVariable("CAPPARSE_TRACE");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            File.AppendAllText(path, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch (Exception exception)
        {
            // The configured path is unusable (for example a literal
            // %TEMP% that PowerShell does not expand, a missing directory
            // or a denied write). The failure cannot be reported in the
            // same defective file, so it goes to a fixed fallback in the
            // user's TEMP: without this the diagnostic channel would fail
            // silently and hide the very problem it exists to expose.
            // Diagnostics must never break the flow.
            ReportTraceFailure(path, exception);
        }
    }

    private static void ReportTraceFailure(string configuredPath, Exception exception)
    {
        try
        {
            var fallback = Path.Combine(Path.GetTempPath(), "capparse-trace-fallback.log");
            File.AppendAllText(fallback,
                $"{DateTime.Now:HH:mm:ss.fff} CAPPARSE_TRACE write to '{configuredPath}' failed: " +
                $"{exception.GetType().Name}: {exception.Message}{Environment.NewLine}");
        }
        catch
        {
            // Last resort: if even the fixed fallback cannot be written
            // there is nowhere left to report the failure; the channel
            // degrades to silence, as before.
        }
    }

    /// <summary>
    /// TEMPORARY diagnostics (black-flash investigation): appends the state
    /// of every top-level window of this process (visible flag, rectangle,
    /// extended style) to the CAPPARSE_TRACE log. Note: IsWindowVisible
    /// reflects WS_VISIBLE, not WPF's Opacity or the painted state of the
    /// DWM surface; it answers "which of our windows was on a monitor at
    /// this moment", which is what the recording cross-reference needs.
    /// </summary>
    private static void TraceWindowStates(string tag)
    {
        var path = Environment.GetEnvironmentVariable("CAPPARSE_TRACE");

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        int pid = Process.GetCurrentProcess().Id;
        var lines = new List<string>();

        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out int windowPid);

            if (windowPid != pid)
            {
                return true;
            }

            bool visible = IsWindowVisible(hwnd);
            GetWindowRect(hwnd, out NativeRect rect);
            int exStyle = GetWindowLong(hwnd, -20); // GWL_EXSTYLE

            lines.Add($"{DateTime.Now:HH:mm:ss.fff} WINDOW {tag} hwnd=0x{hwnd.ToInt64():X} " +
                $"visible={visible} rect=({rect.Left},{rect.Top})-{rect.Right - rect.Left}x{rect.Bottom - rect.Top} " +
                $"exstyle=0x{exStyle:X8}");
            return true;
        }, IntPtr.Zero);

        if (lines.Count > 0)
        {
            try
            {
                File.AppendAllText(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);
            }
            catch
            {
                // Diagnostics must never break the capture flow.
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int index);

    public void Close()
    {
        if (_closed)
        {
            return;
        }

        if (!_constructed)
        {
            // A close request that arrives while the session is still being
            // constructed cannot tear the session down yet: it would fire
            // the callback and release the app's capture guard before the
            // constructor finishes, orphaning the windows created after it.
            // Defer it until the construction ends.
            _closeRequested = true;
            return;
        }

        CloseCore();
    }

    public void Dispose()
    {
        Close();
    }

    /// <summary>
    /// Region of the captured virtual-desktop bitmap that corresponds to a
    /// monitor, in the bitmap's physical pixels. Pixel (0, 0) of the bitmap
    /// is (virtualBounds.Left, virtualBounds.Top) in virtual-desktop
    /// coordinates.
    /// </summary>
    public static Rectangle GetSourceRegion(MonitorInfo monitor, Rectangle virtualBounds)
    {
        var offset = MonitorDiscovery.GetOffset(monitor, virtualBounds);
        return new Rectangle(offset.X, offset.Y, monitor.Bounds.Width, monitor.Bounds.Height);
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (!_constructed)
        {
            // Esc while the session is still being built: defer the close
            // so it runs once all overlays are registered (see Close).
            _closeRequested = true;
            return;
        }

        Close();
    }

    /// <summary>
    /// Starts a selection drag at a global physical cursor position.
    /// </summary>
    private void BeginDrag(Point point)
    {
        if (!_constructed || _closed)
        {
            return;
        }

        _dragStart = point;
        UpdateSelectionVisuals(SelectionGeometry.Normalize(point, point));
    }

    /// <summary>
    /// Grows the selection to the current global physical cursor position
    /// and updates the selection visual on every overlay the selection
    /// touches.
    /// </summary>
    private void UpdateDrag(Point point)
    {
        if (!_constructed || _closed || _dragStart is not { } start)
        {
            return;
        }

        UpdateSelectionVisuals(SelectionGeometry.Normalize(start, point));
    }

    /// <summary>
    /// Ends the drag: the final selection (in virtual-desktop physical
    /// coordinates) is kept only if it has area, then the session closes.
    /// A zero-area drag ends the session without a selection, exactly like
    /// a cancellation.
    /// </summary>
    private void EndDrag(Point point)
    {
        if (!_constructed || _closed || _dragStart is not { } start)
        {
            return;
        }

        var selection = SelectionGeometry.Normalize(start, point);
        _selection = SelectionGeometry.IsValid(selection) ? selection : null;
        Close();
    }

    private void UpdateSelectionVisuals(Rectangle selection)
    {
        foreach (var target in _targets)
        {
            var local = SelectionGeometry.ToMonitorLocal(selection, target.Bounds);

            if (local is null)
            {
                target.Window.HideSelection();
                continue;
            }

            var dip = MonitorGeometry.ToDipRectangle(local.Value, target.Dpi);
            target.Window.ShowSelection(dip.Left, dip.Top, dip.Width, dip.Height);
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Close();
    }

    private void CloseCore()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;

        // Drop the (possibly still pending) two-stage reveal subscription
        // on every teardown path: Esc, an overlay window closed, Dispose
        // and constructor failures all funnel through here. Without this,
        // a close that lands between the two render events would leave the
        // handler subscribed (pinning the session) until the next frame.
        CompositionTarget.Rendering -= OnRevealRendering;

        var selection = _selection;

        foreach (var window in _windows)
        {
            window.Close();
        }

        _windows.Clear();
        _targets.Clear();

        // The frozen BitmapSources hold managed memory only; dropping
        // the references lets the GC collect them.
        _images.Clear();

        // The crop is taken while the captured bitmap is still alive: it is
        // disposed right below, and the crop is independent of it.
        Bitmap? crop = null;

        if (selection is { } sel && _capture is { } capture)
        {
            try
            {
                crop = CaptureCropper.Crop(capture, sel);
            }
            catch
            {
                // Defensive: a selection from the overlays always fits the
                // captured image, so this path should be unreachable. An
                // unexpected failure must not break session teardown (which
                // would leave App stuck in the Capturing state); treat it
                // like a cancelled capture: no crop, callback still runs.
                crop = null;
            }
        }

        _capture?.Dispose();
        _capture = null;

        _dragStart = null;
        _selection = null;

        _onClosed(selection, crop);
    }

    /// <summary>
    /// An overlay window and the geometry needed to project the selection
    /// onto it: the monitor's physical bounds and the DPI the window was
    /// placed at.
    /// </summary>
    private sealed record Target(MonitorOverlayWindow Window, Rectangle Bounds, int Dpi);

    /// <summary>
    /// Creates a monitor-sized WPF image from the monitor's region of the
    /// captured desktop bitmap. The region's BGRA pixels are copied into a
    /// frozen <see cref="BitmapSource"/> whose DPI is the monitor's DPI, so
    /// the image's natural DIP size equals the overlay window's size and
    /// the Fill stretch maps the content 1:1 to physical pixels at any
    /// scaling. The result is fully WPF-managed: it does not depend on the
    /// captured bitmap, which the session keeps alive until it ends.
    /// </summary>
    private static BitmapSource CreateMonitorImage(
        Bitmap desktop, MonitorInfo monitor, Rectangle virtualBounds, int dpi)
    {
        var source = GetSourceRegion(monitor, virtualBounds);

        using var region = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(region))
        {
            // 1:1 pixel copy of the monitor region out of the captured
            // virtual-desktop bitmap; no resampling, no recapture.
            graphics.DrawImage(
                desktop,
                new Rectangle(0, 0, source.Width, source.Height),
                source,
                GraphicsUnit.Pixel);
        }

        // A 32bpp GDI+ surface has no row padding beyond width*4, so the
        // locked bytes can be handed to BitmapSource.Create (which copies
        // them) straight from the locked buffer.
        var bytes = new byte[source.Width * source.Height * 4];
        var bits = region.LockBits(
            new Rectangle(0, 0, source.Width, source.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            Marshal.Copy(bits.Scan0, bytes, 0, bytes.Length);
        }
        finally
        {
            region.UnlockBits(bits);
        }

        var image = BitmapSource.Create(
            source.Width, source.Height, dpi, dpi,
            PixelFormats.Bgra32, null, bytes, source.Width * 4);

        image.Freeze();
        return image;
    }

    /// <summary>
    /// Places the window exactly over the given monitor, in physical pixels.
    ///
    /// Sequence (the documented pattern for PerMonitorV2 WPF windows):
    /// 1. a 1px nudge move inside the target monitor: puts the window in
    ///    that monitor's DPI context; if the DPI differs from the window's
    ///    current one, Windows sends WM_DPICHANGED and WPF updates the
    ///    window's DpiScale;
    /// 2. the exact monitor geometry set through WPF's own DIP properties
    ///    (Left/Top/Width/Height), not a native SetWindowPos: WPF writes
    ///    the native rectangle from its own state, so it cannot later
    ///    re-assert a stale native rect on top of the placement (a native
    ///    SetWindowPos here races WPF's WmMoveChanged re-sync and left one
    ///    overlay mispositioned in a 3-monitor validation);
    /// 3. a check that WPF's DpiScale matches the monitor's real DPI, with
    ///    one bounded nudge + re-apply as a fallback.
    /// </summary>
    private static async Task PlaceOnMonitorAsync(Window window, Rectangle monitorBounds)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();

        SetWindowPos(hwnd, IntPtr.Zero,
            monitorBounds.Left + 1, monitorBounds.Top + 1, 1, 1,
            SetWindowPosNoSize | SetWindowPosNoActivate | SetWindowPosNoZOrder);
        await PumpDispatcherAsync();

        ApplyDipGeometry(window, monitorBounds);
        await PumpDispatcherAsync();

        if (Math.Abs(VisualTreeHelper.GetDpi(window).DpiScaleX * 96.0 - GetDpiForWindow(hwnd)) > 0.5)
        {
            // The DPI update was not applied while the window was invisible;
            // force it with one bounded nudge, then re-apply the geometry.
            SetWindowPos(hwnd, IntPtr.Zero,
                monitorBounds.Left + 1, monitorBounds.Top + 1,
                monitorBounds.Width, monitorBounds.Height,
                SetWindowPosNoActivate | SetWindowPosNoZOrder);
            await PumpDispatcherAsync();

            ApplyDipGeometry(window, monitorBounds);
            await PumpDispatcherAsync();
        }
    }

    /// <summary>
    /// Sets the window's DIP geometry so that, at the window's current
    /// DpiScale, its native rectangle is exactly the monitor's physical
    /// rectangle.
    /// </summary>
    private static void ApplyDipGeometry(Window window, Rectangle monitorBounds)
    {
        int dpi = (int)Math.Round(VisualTreeHelper.GetDpi(window).DpiScaleX * 96.0);
        window.Left = MonitorGeometry.ToDip(monitorBounds.Left, dpi);
        window.Top = MonitorGeometry.ToDip(monitorBounds.Top, dpi);
        window.Width = MonitorGeometry.ToDip(monitorBounds.Width, dpi);
        window.Height = MonitorGeometry.ToDip(monitorBounds.Height, dpi);
    }

    /// <summary>
    /// Runs the dispatcher for one pass so the queued Win32 messages
    /// (WM_DPICHANGED, WM_WINDOWPOSCHANGED) are processed on the UI
    /// thread without blocking it: the await yields the thread back to
    /// the dispatcher's message loop (the M5 synchronous PushFrame pump is
    /// replaced by this asynchronous pass).
    ///
    /// A dispatcher that is shutting down (the application exiting) cannot
    /// process the messages: the pass aborts with a timeout so the
    /// construction closes the windows it already created and the
    /// application's shutdown path takes over.
    /// </summary>
    internal static async Task PumpDispatcherAsync()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;

        if (dispatcher.HasShutdownStarted)
        {
            throw new TimeoutException(
                "The dispatcher is shutting down; the overlay cannot be built.");
        }

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        // The operation's own completion is not awaited; the pass awaits
        // the TCS it completes (bounded by PumpTimeout).
        _ = dispatcher.InvokeAsync(
            () => completion.TrySetResult(true), DispatcherPriority.Background);

        // Safety bound: a missing completion cannot stall the capture flow.
        var completed = await Task.WhenAny(completion.Task, Task.Delay(PumpTimeout));

        if (completed != completion.Task)
        {
            throw new TimeoutException(
                "The dispatcher did not process the window messages in time; " +
                "an overlay may be positioned at the wrong DPI.");
        }

        if (dispatcher.HasShutdownStarted)
        {
            throw new TimeoutException(
                "The dispatcher is shutting down; the overlay cannot be built.");
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);
}
