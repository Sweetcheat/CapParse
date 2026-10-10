using System.Drawing;

namespace CapParse.OCR;

/// <summary>
/// Wraps the bitmap that is the input of an OCR operation. The consumer
/// keeps ownership of the bitmap: creating an <see cref="ImageData"/> does
/// not take it, and nothing in the OCR layer ever disposes it.
/// </summary>
public sealed class ImageData
{
    private readonly Bitmap _bitmap;

    /// <exception cref="ArgumentNullException"><paramref name="bitmap"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="bitmap"/> was
    /// already disposed, or it has no positive width or height.</exception>
    public ImageData(Bitmap bitmap)
    {
        if (bitmap is null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        // The managed System.Drawing reports a disposed bitmap through its
        // property accessors with ArgumentException ("Parameter is not
        // valid."), so reading the dimensions doubles as the liveness
        // probe for a disposed bitmap.
        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            throw new ArgumentException(
                $"Image must have a positive width and height (got {bitmap.Width}x{bitmap.Height}).",
                nameof(bitmap));
        }

        _bitmap = bitmap;
    }

    /// <summary>
    /// The wrapped bitmap. The consumer owns it and is responsible for
    /// disposing it; the OCR engine only reads from it.
    /// </summary>
    public Bitmap Bitmap => _bitmap;
}
