using System.Drawing;

namespace CapParse.Capture;

/// <summary>
/// In-memory result of a successful desktop capture.
/// </summary>
/// <remarks>
/// The result owns the captured bitmap: disposing the result disposes the
/// bitmap. Callers must not dispose the bitmap while the result owns it,
/// and must dispose the result when they are done with it.
/// </remarks>
public sealed class CaptureResult : IDisposable
{
    private Bitmap? _bitmap;

    public CaptureResult(Bitmap bitmap, Rectangle virtualBounds)
    {
        _bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
        VirtualBounds = virtualBounds;
    }

    /// <summary>
    /// The captured virtual-desktop image, in physical pixels.
    /// </summary>
    public Bitmap Image => _bitmap ?? throw new ObjectDisposedException(nameof(CaptureResult));

    /// <summary>
    /// The virtual desktop bounds the image was captured from. Pixel (0, 0)
    /// of the image corresponds to (VirtualBounds.Left, VirtualBounds.Top)
    /// in virtual-desktop coordinates.
    /// </summary>
    public Rectangle VirtualBounds { get; }

    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
    }
}
