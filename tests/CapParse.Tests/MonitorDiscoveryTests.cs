using System.Drawing;
using CapParse.Platform;

namespace CapParse.Tests;

public class MonitorDiscoveryTests
{
    [Fact]
    public void OrderMonitors_SortsDeterministicallyByDeviceName()
    {
        var shuffled = new[]
        {
            CreateMonitor(@"\\.\DISPLAY3"),
            CreateMonitor(@"\\.\DISPLAY1"),
            CreateMonitor(@"\\.\DISPLAY2")
        };

        var ordered = MonitorDiscovery.OrderMonitors(shuffled);

        Assert.Equal(
            new[] { @"\\.\DISPLAY1", @"\\.\DISPLAY2", @"\\.\DISPLAY3" },
            ordered.Select(m => m.DeviceName));
    }

    [Fact]
    public void ToPhysicalBounds_UsesExclusiveRightAndBottom_WithNegativeCoordinates()
    {
        // Geometry of DISPLAY2 on the dev machine: (2560, -489) to (3640, 1431),
        // 1080x1920 in portrait. Guards against an off-by-one (right - left + 1).
        var bounds = MonitorDiscovery.ToPhysicalBounds(2560, -489, 3640, 1431);

        Assert.Equal(2560, bounds.X);
        Assert.Equal(-489, bounds.Y);
        Assert.Equal(1080, bounds.Width);
        Assert.Equal(1920, bounds.Height);
    }

    [Fact]
    public void ToPhysicalBounds_PrimaryMonitorAtOrigin()
    {
        // Geometry of DISPLAY1 on the dev machine: (0, 0) to (2560, 1440).
        var bounds = MonitorDiscovery.ToPhysicalBounds(0, 0, 2560, 1440);

        Assert.Equal(new Rectangle(0, 0, 2560, 1440), bounds);
    }

    private static MonitorInfo CreateMonitor(string deviceName)
    {
        return new MonitorInfo(deviceName, Rectangle.Empty, false);
    }
}
