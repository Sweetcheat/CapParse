using System.Drawing;
using System.Runtime.InteropServices;

namespace CapParse.Platform;

/// <summary>
/// A physical monitor in virtual-desktop coordinates, expressed in physical
/// pixels. <see cref="Bounds"/> may start at negative coordinates (a monitor
/// placed left of the primary, etc.). Conversion to WPF DIP happens only at
/// the UI positioning boundary, never here.
/// </summary>
public sealed record MonitorInfo(
    string DeviceName,
    Rectangle Bounds,
    bool IsPrimary);

/// <summary>
/// Discovers the monitors visible to the process using EnumDisplayMonitors +
/// GetMonitorInfo. Results are ordered deterministically by DeviceName
/// because EnumDisplayMonitors does not guarantee callback order.
/// </summary>
public static class MonitorDiscovery
{
    private const int MonitorInfofPrimary = 0x00000001;

    // MonitorFromRect flag: the nearest monitor to the rectangle, which
    // always resolves to some monitor on a machine with displays.
    private const uint MonitorDefaultTonearest = 2;

    // GetDpiForMonitor type: the effective DPI (the user's scaling).
    private const int DpiTypeEffective = 0;

    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();

        bool ok = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref NativeRect _, IntPtr _) =>
        {
            var info = new MonitorInfoEx { cbSize = Marshal.SizeOf<MonitorInfoEx>() };

            if (GetMonitorInfo(hMonitor, ref info))
            {
                monitors.Add(new MonitorInfo(
                    info.szDevice,
                    ToPhysicalBounds(info.rcMonitor.Left, info.rcMonitor.Top, info.rcMonitor.Right, info.rcMonitor.Bottom),
                    (info.dwFlags & MonitorInfofPrimary) != 0));
            }

            return true;
        }, IntPtr.Zero);

        if (!ok)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        return OrderMonitors(monitors);
    }

    /// <summary>
    /// Sorts monitors by DeviceName (\.\DISPLAYn) so the order is stable and
    /// deterministic across calls.
    /// </summary>
    public static IReadOnlyList<MonitorInfo> OrderMonitors(IEnumerable<MonitorInfo> monitors)
    {
        return monitors
            .OrderBy(m => m.DeviceName, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Converts a Win32 monitor rectangle (right/bottom edges are exclusive)
    /// into a physical-pixel <see cref="Rectangle"/>.
    /// </summary>
    public static Rectangle ToPhysicalBounds(int left, int top, int right, int bottom)
    {
        return new Rectangle(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// Computes the bounding rectangle that encloses all the given monitors,
    /// in virtual-desktop physical pixels. The result may start at negative
    /// coordinates and may contain gaps (areas not covered by any monitor).
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="monitors"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="monitors"/> is empty.</exception>
    public static Rectangle GetVirtualBounds(IEnumerable<MonitorInfo> monitors)
    {
        if (monitors == null)
        {
            throw new ArgumentNullException(nameof(monitors));
        }

        int left = int.MaxValue;
        int top = int.MaxValue;
        int right = int.MinValue;
        int bottom = int.MinValue;

        foreach (var monitor in monitors)
        {
            left = Math.Min(left, monitor.Bounds.Left);
            top = Math.Min(top, monitor.Bounds.Top);
            right = Math.Max(right, monitor.Bounds.Right);
            bottom = Math.Max(bottom, monitor.Bounds.Bottom);
        }

        if (left == int.MaxValue)
        {
            throw new ArgumentException("At least one monitor is required.", nameof(monitors));
        }

        return new Rectangle(left, top, right - left, bottom - top);
    }

    /// <summary>
    /// Position of a monitor within the virtual desktop, i.e. its offset from
    /// the virtual desktop origin. Used to compose per-monitor captures into
    /// a single image of the virtual desktop.
    /// </summary>
    public static Point GetOffset(MonitorInfo monitor, Rectangle virtualBounds)
    {
        return new Point(
            monitor.Bounds.Left - virtualBounds.Left,
            monitor.Bounds.Top - virtualBounds.Top);
    }

    /// <summary>
    /// The effective DPI of the monitor nearest to the given virtual-
    /// desktop rectangle (for example 144 at 150% scaling). Used to tag
    /// the clipboard image so DPI-aware applications paste it at its
    /// on-screen size. Falls back to 96 when no monitor is found or the
    /// query fails, so the result is always usable.
    /// </summary>
    public static int GetEffectiveDpiForSelection(Rectangle selection)
    {
        try
        {
            var rect = new NativeRect
            {
                Left = selection.Left,
                Top = selection.Top,
                Right = selection.Right,
                Bottom = selection.Bottom
            };

            var hMonitor = MonitorFromRect(ref rect, MonitorDefaultTonearest);

            if (hMonitor != IntPtr.Zero
                && GetDpiForMonitor(hMonitor, DpiTypeEffective, out uint dpiX, out _) == 0
                && dpiX > 0)
            {
                return (int)dpiX;
            }
        }
        catch
        {
            // A DPI query failure must never break an image copy: fall
            // through to the 96 fallback below.
        }

        return 96;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public int dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeRect lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc proc, IntPtr data);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref NativeRect lprcRect, uint dwFlags);

    // shcore.dll exists on Windows 10 and later, the app's minimum target.
    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);
}
