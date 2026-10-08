using System.Drawing;
using CapParse.Platform;

namespace CapParse.Tests;

// Pure geometry tests for the M4-C virtual-desktop composition. The actual
// GDI capture into one bitmap is validated out-of-band on a real
// three-monitor setup (see the M4-C validation harness), not here.
public class VirtualDesktopTests
{
    // Geometry of the dev machine:
    //   DISPLAY1 (0, 0)        2560x1440  primary
    //   DISPLAY2 (2560, -489)  1080x1920  portrait
    //   DISPLAY3 (-1920, 361)  1920x1080
    private static readonly MonitorInfo Display1 = new(@"\\.\DISPLAY1", new Rectangle(0, 0, 2560, 1440), true);
    private static readonly MonitorInfo Display2 = new(@"\\.\DISPLAY2", new Rectangle(2560, -489, 1080, 1920), false);
    private static readonly MonitorInfo Display3 = new(@"\\.\DISPLAY3", new Rectangle(-1920, 361, 1920, 1080), false);

    [Fact]
    public void GetVirtualBounds_RealThreeMonitorSetup()
    {
        var bounds = MonitorDiscovery.GetVirtualBounds(new[] { Display1, Display2, Display3 });

        Assert.Equal(new Rectangle(-1920, -489, 5560, 1930), bounds);
    }

    [Fact]
    public void GetVirtualBounds_SingleMonitor()
    {
        var bounds = MonitorDiscovery.GetVirtualBounds(
            new[] { new MonitorInfo(@"\\.\DISPLAY1", new Rectangle(100, 50, 800, 600), true) });

        Assert.Equal(new Rectangle(100, 50, 800, 600), bounds);
    }

    [Fact]
    public void GetVirtualBounds_EntirelyNegativeOrigin()
    {
        var bounds = MonitorDiscovery.GetVirtualBounds(new[]
        {
            new MonitorInfo(@"\\.\DISPLAY1", new Rectangle(-2000, -1000, 800, 600), true),
            new MonitorInfo(@"\\.\DISPLAY2", new Rectangle(-1200, -300, 700, 500), false)
        });

        Assert.Equal(new Rectangle(-2000, -1000, 1500, 1200), bounds);
    }

    [Fact]
    public void GetVirtualBounds_MonitorAbovePrimary()
    {
        var bounds = MonitorDiscovery.GetVirtualBounds(new[]
        {
            new MonitorInfo(@"\\.\DISPLAY1", new Rectangle(0, 0, 1920, 1080), true),
            new MonitorInfo(@"\\.\DISPLAY2", new Rectangle(0, -1080, 1920, 1080), false)
        });

        Assert.Equal(new Rectangle(0, -1080, 1920, 2160), bounds);
    }

    [Fact]
    public void GetVirtualBounds_IncludesGaps()
    {
        // 100px horizontal gap between the two monitors.
        var bounds = MonitorDiscovery.GetVirtualBounds(new[]
        {
            new MonitorInfo(@"\\.\DISPLAY1", new Rectangle(0, 0, 100, 100), true),
            new MonitorInfo(@"\\.\DISPLAY2", new Rectangle(200, 0, 100, 100), false)
        });

        Assert.Equal(new Rectangle(0, 0, 300, 100), bounds);
    }

    [Fact]
    public void GetVirtualBounds_PortraitPlusLandscape()
    {
        var bounds = MonitorDiscovery.GetVirtualBounds(new[]
        {
            new MonitorInfo(@"\\.\DISPLAY1", new Rectangle(0, 0, 1920, 1080), true),
            new MonitorInfo(@"\\.\DISPLAY2", new Rectangle(1920, -100, 540, 960), false)
        });

        Assert.Equal(new Rectangle(0, -100, 2460, 1180), bounds);
    }

    [Fact]
    public void GetVirtualBounds_IndependentOfInputOrder()
    {
        var expected = MonitorDiscovery.GetVirtualBounds(new[] { Display1, Display2, Display3 });

        var actual = MonitorDiscovery.GetVirtualBounds(new[] { Display3, Display1, Display2 });

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetVirtualBounds_EmptyList_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            MonitorDiscovery.GetVirtualBounds(Array.Empty<MonitorInfo>()));
    }

    [Fact]
    public void GetVirtualBounds_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MonitorDiscovery.GetVirtualBounds(null!));
    }

    [Fact]
    public void GetOffset_RealThreeMonitorSetup()
    {
        var virtualBounds = MonitorDiscovery.GetVirtualBounds(new[] { Display1, Display2, Display3 });

        Assert.Equal(new Point(1920, 489), MonitorDiscovery.GetOffset(Display1, virtualBounds));
        Assert.Equal(new Point(4480, 0), MonitorDiscovery.GetOffset(Display2, virtualBounds));
        Assert.Equal(new Point(0, 850), MonitorDiscovery.GetOffset(Display3, virtualBounds));
    }

    [Fact]
    public void GetOffset_NegativeVirtualOrigin()
    {
        var virtualBounds = new Rectangle(-500, -300, 800, 600);

        var left = new MonitorInfo(@"\\.\DISPLAY1", new Rectangle(-500, -300, 400, 300), true);
        var right = new MonitorInfo(@"\\.\DISPLAY2", new Rectangle(100, 0, 400, 300), false);

        Assert.Equal(new Point(0, 0), MonitorDiscovery.GetOffset(left, virtualBounds));
        Assert.Equal(new Point(600, 300), MonitorDiscovery.GetOffset(right, virtualBounds));
    }
}
