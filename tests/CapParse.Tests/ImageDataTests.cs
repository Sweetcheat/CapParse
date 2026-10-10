using System.Drawing;
using System.Drawing.Imaging;
using CapParse.OCR;

namespace CapParse.Tests;

// Deterministic validation of the OCR input wrapper. No model, no engine.
public class ImageDataTests
{
    [Fact]
    public void Ctor_NullBitmap_ThrowsArgumentNullException()
    {
        Bitmap? nullBitmap = null;

        Assert.Throws<ArgumentNullException>(() => new ImageData(nullBitmap));
    }

    [Fact]
    public void Ctor_ValidBitmap_ExposesTheSameInstance()
    {
        using var bitmap = new Bitmap(4, 2, PixelFormat.Format32bppArgb);

        var data = new ImageData(bitmap);

        // ImageData wraps, it never copies: the consumer keeps its bitmap.
        Assert.Same(bitmap, data.Bitmap);
    }

    [Fact]
    public void Ctor_DisposedBitmap_ThrowsArgumentException()
    {
        var bitmap = new Bitmap(4, 2, PixelFormat.Format32bppArgb);
        bitmap.Dispose();

        // The managed System.Drawing surfaces a disposed bitmap through its
        // property accessors with ArgumentException.
        Assert.Throws<ArgumentException>(() => new ImageData(bitmap));
    }
}
