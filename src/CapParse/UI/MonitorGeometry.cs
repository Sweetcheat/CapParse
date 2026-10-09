using System.Drawing;

namespace CapParse.UI;

/// <summary>
/// Conversion between physical pixels and WPF device-independent pixels
/// (DIPs). Used only at the UI boundary: monitor geometry and captured image
/// coordinates remain physical pixels everywhere else in the application.
/// </summary>
public static class MonitorGeometry
{
    /// <summary>
    /// Converts a physical pixel length to WPF DIPs at the given DPI.
    /// Negative values are valid (virtual-desktop coordinates).
    /// </summary>
    public static double ToDip(int physicalPixels, int dpi)
    {
        if (dpi <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dpi), dpi, "DPI must be positive.");
        }

        return physicalPixels * 96.0 / dpi;
    }

    /// <summary>
    /// Converts a physical-pixel rectangle to WPF DIPs at the given DPI.
    /// </summary>
    public static (double Left, double Top, double Width, double Height) ToDipRectangle(
        Rectangle physical, int dpi)
    {
        return (
            ToDip(physical.Left, dpi),
            ToDip(physical.Top, dpi),
            ToDip(physical.Width, dpi),
            ToDip(physical.Height, dpi));
    }
}
