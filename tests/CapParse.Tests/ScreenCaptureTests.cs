using System.Drawing;
using CapParse.Capture;

namespace CapParse.Tests;

// The GDI capture itself needs a real desktop and is validated out-of-band
// (see the M4-B validation harness). Only the input-validation logic can be
// unit-tested headless.
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
}
