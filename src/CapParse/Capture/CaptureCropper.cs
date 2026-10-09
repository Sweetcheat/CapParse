using System.Drawing;
using System.Drawing.Imaging;

namespace CapParse.Capture;

/// <summary>
/// Crops the selected region out of a captured virtual-desktop image. The
/// selection is expressed in virtual-desktop physical coordinates and is
/// mapped to the image's local pixels through the capture's
/// <see cref="CaptureResult.VirtualBounds"/>; no DPI is involved, the crop
/// operates on the captured pixels only.
/// </summary>
public static class CaptureCropper
{
    /// <summary>
    /// Returns an independent copy of the selected region: a new 32bpp ARGB
    /// bitmap sized exactly to the selection, with the original pixels
    /// (transparency included) copied verbatim, without resampling. The
    /// source bitmap is left untouched, and the result stays valid after
    /// the source is disposed.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentException">The selection has no area or is
    /// not fully inside the captured image. Invalid selections are rejected,
    /// never truncated or clamped.</exception>
    public static Bitmap Crop(CaptureResult source, Rectangle selection)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (!SelectionGeometry.IsValid(selection))
        {
            throw new ArgumentException(
                $"Selection must have a positive width and height (got {selection.Width}x{selection.Height}).",
                nameof(selection));
        }

        // Throws ObjectDisposedException if the source was already disposed.
        var image = source.Image;

        // Global physical -> image-local offset. Pixel (0, 0) of the image
        // is (VirtualBounds.Left, VirtualBounds.Top), so negative virtual
        // desktop origins map to positive image offsets.
        var region = SelectionGeometry.ToImageRectangle(selection, source.VirtualBounds);

        // Long arithmetic: X/Y and Width/Height are int, and the sums must
        // not overflow before the comparisons.
        if (region.X < 0 || region.Y < 0
            || (long)region.X + region.Width > image.Width
            || (long)region.Y + region.Height > image.Height)
        {
            throw new ArgumentException(
                $"Selection at ({selection.Left},{selection.Top}) {selection.Width}x{selection.Height} " +
                $"is not fully inside the captured image ({image.Width}x{image.Height}).",
                nameof(selection));
        }

        // Clone copies the region's pixels verbatim (no resampling, no
        // recapture) into a new independent bitmap, in the same 32bpp ARGB
        // format the capture uses.
        return image.Clone(region, PixelFormat.Format32bppArgb);
    }
}
