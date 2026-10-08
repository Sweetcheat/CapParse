using System.Drawing;
using System.Drawing.Imaging;
using CapParse.Capture;

namespace CapParse.Tests;

// Deterministic tests for the M4-E in-memory capture result. All bitmaps are
// created in memory; no screen access is involved.
public class CaptureResultTests
{
    [Fact]
    public void Ctor_StoresBitmapAndVirtualBounds()
    {
        using var bitmap = new Bitmap(32, 16, PixelFormat.Format32bppArgb);
        var bounds = new Rectangle(-100, -50, 32, 16);

        using var result = new CaptureResult(bitmap, bounds);

        Assert.Same(bitmap, result.Image);
        Assert.Equal(bounds, result.VirtualBounds);
    }

    [Fact]
    public void Dispose_DisposesBitmap()
    {
        var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb);

        var result = new CaptureResult(bitmap, new Rectangle(0, 0, 16, 16));
        result.Dispose();

        // A disposed GDI+ bitmap throws on property access (.NET 8's
        // System.Drawing.Common surfaces the dead GDI+ handle as an
        // ArgumentException, not an ObjectDisposedException).
        Assert.Throws<ArgumentException>(() => _ = bitmap.Width);
    }

    [Fact]
    public void Dispose_Twice_IsSafe()
    {
        var result = new CaptureResult(
            new Bitmap(16, 16, PixelFormat.Format32bppArgb),
            Rectangle.Empty);

        result.Dispose();
        result.Dispose();
    }

    [Fact]
    public void Ctor_NullBitmap_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CaptureResult(null!, new Rectangle(0, 0, 10, 10)));
    }

    [Fact]
    public void Image_AfterDispose_Throws()
    {
        var result = new CaptureResult(
            new Bitmap(16, 16, PixelFormat.Format32bppArgb),
            new Rectangle(0, 0, 16, 16));
        result.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = result.Image);
    }
}
