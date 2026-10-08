using System.Drawing;
using System.Drawing.Imaging;
using CapParse.Capture;

namespace CapParse.Tests;

// The GDI capture itself needs a real desktop and is validated out-of-band
// (see the M4-B/M4-C validation harness). Only the input-validation logic
// can be unit-tested headless: every assertion below throws before any
// screen DC is touched.
public class ScreenCaptureTests
{
    [Theory]
    [InlineData(0, 100)]    // zero width
    [InlineData(100, 0)]    // zero height
    [InlineData(-100, 100)] // negative width
    [InlineData(100, -100)] // negative height
    public void CaptureRectangle_NonPositiveSize_ThrowsArgumentException(int width, int height)
    {
        // Negative origins are valid inputs; only size is rejected here.
        var bounds = new Rectangle(-500, -300, width, height);

        Assert.Throws<ArgumentException>(() => ScreenCapture.CaptureRectangle(bounds));
    }

    [Fact]
    public void CaptureRectangleInto_NullDestination_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ScreenCapture.CaptureRectangleInto(new Rectangle(-50, -30, 10, 10), null!, 0, 0));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(-10, 10)]
    [InlineData(10, -10)]
    public void CaptureRectangleInto_NonPositiveSourceSize_Throws(int width, int height)
    {
        using var destination = new Bitmap(100, 100, PixelFormat.Format32bppArgb);

        Assert.Throws<ArgumentException>(() =>
            ScreenCapture.CaptureRectangleInto(new Rectangle(-50, -30, width, height), destination, 0, 0));
    }

    [Theory]
    [InlineData(-1, 0)]  // negative X offset
    [InlineData(0, -1)]  // negative Y offset
    [InlineData(95, 0)]  // 95 + 10 > 100, overflows width
    [InlineData(0, 95)]  // 95 + 10 > 100, overflows height
    public void CaptureRectangleInto_RegionOutsideDestination_Throws(int x, int y)
    {
        using var destination = new Bitmap(100, 100, PixelFormat.Format32bppArgb);

        Assert.Throws<ArgumentException>(() =>
            ScreenCapture.CaptureRectangleInto(new Rectangle(-50, -30, 10, 10), destination, x, y));
    }
}
