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

    /// <summary>
    /// Captures a physical-pixel rectangle of the virtual desktop directly
    /// into an existing bitmap, without allocating an intermediate bitmap.
    /// Negative source X/Y are valid, as in <see cref="CaptureRectangle"/>.
    /// </summary>
    /// <remarks>
    /// <paramref name="destination"/> must be large enough to hold the
    /// <paramref name="sourceBounds"/> size at
    /// (<paramref name="destinationX"/>, <paramref name="destinationY"/>).
    /// This method does not take ownership of the bitmap: the caller remains
    /// responsible for disposing it.
    /// </remarks>
    public static void CaptureRectangleInto(
        Rectangle sourceBounds,
        Bitmap destination,
        int destinationX,
        int destinationY)
    {
        if (sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
        {
            throw new ArgumentException(
                $"Rectangle must have a positive width and height (got {sourceBounds.Width}x{sourceBounds.Height}).",
                nameof(sourceBounds));
        }

        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        if (destinationX < 0 || destinationY < 0
            || destinationX + sourceBounds.Width > destination.Width
            || destinationY + sourceBounds.Height > destination.Height)
        {
            throw new ArgumentException(
                $"Region {sourceBounds.Width}x{sourceBounds.Height} at ({destinationX},{destinationY}) " +
                $"does not fit inside a {destination.Width}x{destination.Height} bitmap.",
                nameof(destinationX));
        }

        // Same GDI BitBlt from the screen DC as CaptureRectangle, drawn at an
        // offset into an existing bitmap.
        using var graphics = Graphics.FromImage(destination);
        graphics.CopyFromScreen(
            sourceBounds.Left, sourceBounds.Top, destinationX, destinationY,
            new Size(sourceBounds.Width, sourceBounds.Height),
            CopyPixelOperation.SourceCopy);
    }
}
