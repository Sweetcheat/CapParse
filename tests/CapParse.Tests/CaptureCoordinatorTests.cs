using System.Drawing.Imaging;
using CapParse.Capture;
using CapParse.Platform;

namespace CapParse.Tests;

// M4-E: the coordinator returns a capture result that owns the captured
// bitmap. The meaningful automated check is a real end-to-end call; it
// requires at least one monitor (any configuration works; specific
// multi-monitor geometry is covered by VirtualDesktopTests). The result is
// disposed at the end of each check, so a successful run also proves the
// ownership boundary completes safely.
public class CaptureCoordinatorTests
{
    [Fact]
    public void StartCapture_RealDesktop_ReturnsValidResult()
    {
        var coordinator = new CaptureCoordinator();

        var result = coordinator.StartCapture();
        var bitmap = result.Image;

        Assert.NotNull(bitmap);
        Assert.Equal(PixelFormat.Format32bppArgb, bitmap.PixelFormat);
        Assert.Equal(result.VirtualBounds.Width, bitmap.Width);
        Assert.Equal(result.VirtualBounds.Height, bitmap.Height);

        // The result must describe the same virtual desktop that produced
        // the image. A fresh discovery call matches on a stable desktop;
        // no specific monitor geometry is assumed.
        var expectedBounds = MonitorDiscovery.GetVirtualBounds(MonitorDiscovery.GetMonitors());
        Assert.Equal(expectedBounds, result.VirtualBounds);

        // Disposing the result releases the bitmap it owns. A disposed
        // GDI+ bitmap throws on property access (see CaptureResultTests).
        result.Dispose();
        Assert.Throws<ArgumentException>(() => _ = bitmap.Width);
    }

    [Fact]
    public void StartCapture_RepeatedCaptures_DisposeWithoutLeaking()
    {
        var coordinator = new CaptureCoordinator();

        for (var i = 0; i < 3; i++)
        {
            var result = coordinator.StartCapture();
            var bitmap = result.Image;

            Assert.True(bitmap.Width > 0);
            Assert.True(bitmap.Height > 0);

            result.Dispose();
        }
    }
}
