using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CapParse.Capture;
using CapParse.Platform;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Key = System.Windows.Input.Key;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace CapParse.UI;

/// <summary>
/// M5-A freeze-frame overlay session: one borderless overlay window per
/// monitor, each displaying that monitor's region of the single captured
/// desktop bitmap, dimmed.
///
/// The session owns the <see cref="CaptureResult"/>: closing the session
/// (via Esc on any overlay, or <see cref="Dispose"/>) closes every overlay
/// window, releases the WPF images and disposes the captured bitmap. If
/// overlay creation fails partway, the windows that were already created
/// are closed before the failure propagates.
/// </summary>
public sealed class FrozenOverlaySession : IDisposable
{
    // Conservative dimming: clearly frozen, content still recognizable.
    private const double DimOpacity = 0.5;

    private const uint SetWindowPosNoSize = 0x0001;
    private const uint SetWindowPosNoActivate = 0x0010;
    private const uint SetWindowPosNoZOrder = 0x0004;

    private readonly Action _onClosed;
    private readonly List<MonitorOverlayWindow> _windows = new();
    private readonly List<BitmapSource> _images = new();
    private CaptureResult? _capture;
    private bool _closed;

    public FrozenOverlaySession(CaptureResult capture, Action onClosed)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _onClosed = onClosed ?? throw new ArgumentNullException(nameof(onClosed));

        try
        {
            // Re-discovers the same monitors that were just captured; the
            // session lives for seconds, so the configuration cannot drift.
            var monitors = MonitorDiscovery.GetMonitors();

            foreach (var monitor in monitors)
            {
                // The window is created and placed on its monitor (opacity 0)
                // before its image is built: placement establishes the
                // window's DpiScale, which the image needs so its natural
                // size matches the window size exactly.
                var window = new MonitorOverlayWindow(DimOpacity)
                {
                    // Invisible while placed: the window is created, moved
                    // onto its monitor and sized, then revealed exactly once
                    // after all overlays are ready.
                    Opacity = 0
                };

                window.Closed += OnWindowClosed;
                window.KeyDown += OnWindowKeyDown;
                window.Show();
                PlaceOnMonitor(window, monitor.Bounds);

                int dpi = (int)Math.Round(VisualTreeHelper.GetDpi(window).DpiScaleX * 96.0);
                var image = CreateMonitorImage(capture.Image, monitor, capture.VirtualBounds, dpi);
                window.SetFrozenImage(image);

                _windows.Add(window);
                _images.Add(image);
            }

            foreach (var window in _windows)
            {
                window.Opacity = 1;
            }

            // Take keyboard focus so Esc can end the session. M5-A has no
            // other exit: Esc is the only way out of a freeze-frame session.
            _windows[0].Activate();
        }
        catch
        {
            CloseCore();
            throw;
        }
    }

    public void Close()
    {
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
        if (e.Key == Key.Escape)
        {
            Close();
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

        foreach (var window in _windows)
        {
            window.Close();
        }

        _windows.Clear();

        // The frozen BitmapSources hold managed memory only; dropping
        // the references lets the GC collect them.
        _images.Clear();

        _capture?.Dispose();
        _capture = null;

        _onClosed();
    }

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
    private static void PlaceOnMonitor(Window window, Rectangle monitorBounds)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();

        SetWindowPos(hwnd, IntPtr.Zero,
            monitorBounds.Left + 1, monitorBounds.Top + 1, 1, 1,
            SetWindowPosNoSize | SetWindowPosNoActivate | SetWindowPosNoZOrder);
        PumpDispatcher();

        ApplyDipGeometry(window, monitorBounds);
        PumpDispatcher();

        if (Math.Abs(VisualTreeHelper.GetDpi(window).DpiScaleX * 96.0 - GetDpiForWindow(hwnd)) > 0.5)
        {
            // The DPI update was not applied while the window was invisible;
            // force it with one bounded nudge, then re-apply the geometry.
            SetWindowPos(hwnd, IntPtr.Zero,
                monitorBounds.Left + 1, monitorBounds.Top + 1,
                monitorBounds.Width, monitorBounds.Height,
                SetWindowPosNoActivate | SetWindowPosNoZOrder);
            PumpDispatcher();

            ApplyDipGeometry(window, monitorBounds);
            PumpDispatcher();
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
    /// Runs the dispatcher until all queued Win32 messages (WM_DPICHANGED,
    /// WM_WINDOWPOSCHANGED) have been processed on the UI thread. Bounded:
    /// a timeout cannot stall the capture flow.
    /// </summary>
    private static void PumpDispatcher()
    {
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => done.TrySetResult(true)));

        done.Task.Wait(TimeSpan.FromSeconds(2));
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);
}
