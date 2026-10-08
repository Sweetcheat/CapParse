using System.Drawing;
using System.Drawing.Imaging;

namespace CapParse.Capture;

/// <summary>
/// Minimal screen capture primitive: captures a physical-pixel rectangle of
/// the virtual desktop and returns it as an in-memory GDI bitmap.
/// </summary>
public static class ScreenCapture
{
    /// <summary>
    /// Captures the given virtual-desktop rectangle, expressed in physical
    /// pixels. Negative X/Y are valid: the virtual desktop is a single
    /// coordinate space that may extend below or left of the origin.
    /// </summary>
    /// <remarks>
    /// The returned bitmap is exactly <paramref name="bounds"/> in size, in
    /// 32bpp ARGB, and is owned by the caller, which must dispose it.
    /// GDI failures surface as GDI+ exceptions; the partially created bitmap
    /// is disposed before rethrowing.
    /// </remarks>
    public static Bitmap CaptureRectangle(Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentException(
                $"Rectangle must have a positive width and height (got {bounds.Width}x{bounds.Height}).",
                nameof(bounds));
        }

        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);

        try
        {
            // CopyFromScreen is a GDI BitBlt from the screen DC. The screen DC
            // uses virtual-desktop coordinates, so negative origins work
            // without any conversion.
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(
                bounds.Left, bounds.Top, 0, 0,
                new Size(bounds.Width, bounds.Height),
                CopyPixelOperation.SourceCopy);

            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }
}
