using System.Drawing;
using CapParse.UI;

namespace CapParse.Tests;

// Pure geometry tests for the M5-A physical-pixel -> WPF-DIP boundary.
// No desktop access, no window creation.
public class MonitorGeometryTests
{
    [Theory]
    [InlineData(96)]
    [InlineData(120)]
    [InlineData(144)]
    [InlineData(192)]
    public void ToDip_KnownDpiValues(int dpi)
    {
        Assert.Equal(100 * 96.0 / dpi, MonitorGeometry.ToDip(100, dpi), 10);
        Assert.Equal(2560 * 96.0 / dpi, MonitorGeometry.ToDip(2560, dpi), 10);
    }

    [Fact]
    public void ToDip_At96Dpi_IsIdentity()
    {
        Assert.Equal(960.0, MonitorGeometry.ToDip(960, 96), 10);
    }

    [Fact]
    public void ToDip_NegativeCoordinates_ArePreserved()
    {
        // Edges of DISPLAY3 (left) and DISPLAY2 (top) on the dev machine.
        Assert.Equal(-1920.0, MonitorGeometry.ToDip(-1920, 96), 10);
        Assert.Equal(-489 * 96.0 / 144, MonitorGeometry.ToDip(-489, 144), 10);
    }

    [Fact]
    public void ToDipRectangle_PortraitMonitor_MixedDpi()
    {
        // DISPLAY2 on the dev machine: 1080x1920 portrait at (2560, -489),
        // here converted at 150% scaling (144 DPI).
        var (left, top, width, height) = MonitorGeometry.ToDipRectangle(
            new Rectangle(2560, -489, 1080, 1920), 144);

        Assert.Equal(2560 * 96.0 / 144, left, 10);
        Assert.Equal(-489 * 96.0 / 144, top, 10);
        Assert.Equal(1080 * 96.0 / 144, width, 10);
        Assert.Equal(1920 * 96.0 / 144, height, 10);
    }

    [Fact]
    public void ToDip_NonPositiveDpi_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MonitorGeometry.ToDip(100, 0));
    }
}
