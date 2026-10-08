using System.Drawing;
using System.Drawing.Imaging;
using CapParse.Platform;

namespace CapParse.Capture;

/// <summary>
/// Captures the entire virtual desktop (all monitors) as a single bitmap.
/// </summary>
public static class DesktopCapture
{
    /// <summary>
    /// Captures every monitor into one bitmap sized to the virtual desktop.
    /// Monitor positions, negative coordinates, mixed resolutions and
    /// orientations are preserved exactly; areas of the bounding rectangle
    /// not covered by any monitor keep the bitmap's initial value.
    /// </summary>
    /// <remarks>
    /// On success the bitmap is owned by the returned result, which the
    /// caller must dispose. If a capture fails, the bitmap is disposed and
    /// the exception is rethrown; no result is returned.
    /// </summary>
    public static CaptureResult CaptureVirtualDesktop()
    {
        var monitors = MonitorDiscovery.GetMonitors();
        var virtualBounds = MonitorDiscovery.GetVirtualBounds(monitors);

        var bitmap = new Bitmap(virtualBounds.Width, virtualBounds.Height, PixelFormat.Format32bppArgb);

        try
        {
            foreach (var monitor in monitors)
            {
                var offset = MonitorDiscovery.GetOffset(monitor, virtualBounds);
                ScreenCapture.CaptureRectangleInto(monitor.Bounds, bitmap, offset.X, offset.Y);
            }

            return new CaptureResult(bitmap, virtualBounds);
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }
}
