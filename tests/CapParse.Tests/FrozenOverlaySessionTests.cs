using System.Drawing;
using CapParse.Platform;
using CapParse.UI;

namespace CapParse.Tests;

// Pure mapping tests for the M5-A monitor -> captured-bitmap region. Uses
// the known-good geometry of the dev machine's three monitors; no capture,
// no window, no screen access.
public class FrozenOverlaySessionTests
{
    private static readonly MonitorInfo Display1 =
        new(@"\\.\DISPLAY1", new Rectangle(0, 0, 2560, 1440), true);

    private static readonly MonitorInfo Display2 =
        new(@"\\.\DISPLAY2", new Rectangle(2560, -489, 1080, 1920), false);

    private static readonly MonitorInfo Display3 =
        new(@"\\.\DISPLAY3", new Rectangle(-1920, 361, 1920, 1080), false);

    private static readonly Rectangle VirtualBounds = new(-1920, -489, 5560, 1930);

    [Fact]
    public void GetSourceRegion_RealThreeMonitorSetup()
    {
        Assert.Equal(
            new Rectangle(1920, 489, 2560, 1440),
            FrozenOverlaySession.GetSourceRegion(Display1, VirtualBounds));

        Assert.Equal(
            new Rectangle(4480, 0, 1080, 1920),
            FrozenOverlaySession.GetSourceRegion(Display2, VirtualBounds));

        Assert.Equal(
            new Rectangle(0, 850, 1920, 1080),
            FrozenOverlaySession.GetSourceRegion(Display3, VirtualBounds));
    }

    [Fact]
    public void GetSourceRegion_SingleMonitorAtOrigin()
    {
        var monitor = new MonitorInfo(@"\\.\DISPLAY1", new Rectangle(0, 0, 1920, 1080), true);

        Assert.Equal(
            new Rectangle(0, 0, 1920, 1080),
            FrozenOverlaySession.GetSourceRegion(monitor, new Rectangle(0, 0, 1920, 1080)));
    }

    [Fact]
    public void GetSourceRegion_RegionFitsInsideBitmap()
    {
        // The source region of every monitor must lie inside the captured
        // bitmap (no negative offsets, no overflow past the right/bottom).
        var monitors = new[] { Display1, Display2, Display3 };

        foreach (var monitor in monitors)
        {
            var region = FrozenOverlaySession.GetSourceRegion(monitor, VirtualBounds);

            Assert.True(region.Left >= 0, $"left {region.Left} < 0");
            Assert.True(region.Top >= 0, $"top {region.Top} < 0");
            Assert.True(region.Right <= VirtualBounds.Width, $"right {region.Right} > {VirtualBounds.Width}");
            Assert.True(region.Bottom <= VirtualBounds.Height, $"bottom {region.Bottom} > {VirtualBounds.Height}");
        }
    }
}
