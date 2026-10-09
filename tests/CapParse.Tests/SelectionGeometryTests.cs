using System.Drawing;
using CapParse.Capture;
using CapParse.Platform;
using CapParse.UI;

namespace CapParse.Tests;

// Pure geometry tests for the M5-B selection: normalization in every drag
// direction, edge semantics, and the physical-coordinate <-> captured-image
// mapping. Deterministic: they use the known three-monitor dev-machine
// layout and never touch the desktop.
public class SelectionGeometryTests
{
    private static readonly MonitorInfo Display1 =
        new(@"\\.\DISPLAY1", new Rectangle(0, 0, 2560, 1440), true);

    private static readonly MonitorInfo Display2 =
        new(@"\\.\DISPLAY2", new Rectangle(2560, -489, 1080, 1920), false);

    private static readonly MonitorInfo Display3 =
        new(@"\\.\DISPLAY3", new Rectangle(-1920, 361, 1920, 1080), false);

    private static readonly Rectangle VirtualBounds = new(-1920, -489, 5560, 1930);

    [Theory]
    [InlineData(10, 20, 110, 120)] // left->right, top->bottom
    [InlineData(110, 120, 10, 20)] // right->left, bottom->top
    [InlineData(10, 120, 110, 20)] // left->right, bottom->top
    [InlineData(110, 20, 10, 120)] // right->left, top->bottom
    public void Normalize_AnyDragDirection_ProducesTheSameRectangle(int ax, int ay, int bx, int by)
    {
        var rect = SelectionGeometry.Normalize(new Point(ax, ay), new Point(bx, by));

        Assert.Equal(new Rectangle(10, 20, 100, 100), rect);
    }

    [Fact]
    public void Normalize_UsesExclusiveRightBottomEdges_NoOffByOne()
    {
        var rect = SelectionGeometry.Normalize(new Point(0, 0), new Point(5, 7));

        Assert.Equal(0, rect.Left);
        Assert.Equal(0, rect.Top);
        Assert.Equal(5, rect.Width);
        Assert.Equal(7, rect.Height);
        // Exclusive edges: pixel (5, y) and (x, 7) are outside the selection.
        Assert.Equal(5, rect.Right);
        Assert.Equal(7, rect.Bottom);
    }

    [Fact]
    public void Normalize_NegativeCoordinates()
    {
        var rect = SelectionGeometry.Normalize(new Point(-500, 500), new Point(300, 900));

        Assert.Equal(new Rectangle(-500, 500, 800, 400), rect);
    }

    [Fact]
    public void Normalize_IdenticalPoints_ProducesZeroArea()
    {
        var rect = SelectionGeometry.Normalize(new Point(42, 42), new Point(42, 42));

        Assert.Equal(42, rect.Left);
        Assert.Equal(42, rect.Top);
        Assert.Equal(0, rect.Width);
        Assert.Equal(0, rect.Height);
        Assert.False(SelectionGeometry.IsValid(rect));
    }

    [Theory]
    [InlineData(0, 10)] // horizontal drag only
    [InlineData(10, 0)] // vertical drag only
    public void IsValid_ZeroWidthOrHeight_IsFalse(int width, int height)
    {
        Assert.False(SelectionGeometry.IsValid(new Rectangle(5, 5, width, height)));
    }

    [Fact]
    public void IsValid_OnePixel_IsTrue()
    {
        Assert.True(SelectionGeometry.IsValid(new Rectangle(5, 5, 1, 1)));
    }

    // Reference points from the M5-B spec: physical global -> offset in the
    // captured image (virtual bounds -1920,-489 5560x1930).
    [Theory]
    [InlineData(-1920, -489, 0, 0)]
    [InlineData(0, 0, 1920, 489)]
    [InlineData(2560, -489, 4480, 0)]
    [InlineData(-1, 0, 1919, 489)]
    public void ToImageOffset_ReferencePoints(int physicalX, int physicalY, int expectedX, int expectedY)
    {
        var offset = SelectionGeometry.ToImageOffset(new Point(physicalX, physicalY), VirtualBounds);

        Assert.Equal(expectedX, offset.X);
        Assert.Equal(expectedY, offset.Y);
    }

    [Fact]
    public void ToImageRectangle_MapsSelectionIntoCapturedImage()
    {
        // A selection crossing the Display1/Display2 boundary (x = 2560).
        var selection = new Rectangle(2500, 100, 200, 200);

        Assert.Equal(
            new Rectangle(4420, 589, 200, 200),
            SelectionGeometry.ToImageRectangle(selection, VirtualBounds));
    }

    [Fact]
    public void ImageOffset_RoundTripsBackToVirtualDesktop()
    {
        var physical = new Point(-1919, 1440);
        var offset = SelectionGeometry.ToImageOffset(physical, VirtualBounds);

        Assert.Equal(physical, new Point(offset.X + VirtualBounds.Left, offset.Y + VirtualBounds.Top));
    }

    [Fact]
    public void ToImageRectangle_FullDesktop_IsNotClampedToAnySingleMonitor()
    {
        var selection = new Rectangle(VirtualBounds.Left, VirtualBounds.Top, VirtualBounds.Width, VirtualBounds.Height);

        Assert.Equal(new Rectangle(0, 0, 5560, 1930), SelectionGeometry.ToImageRectangle(selection, VirtualBounds));
    }

    [Fact]
    public void ToMonitorLocal_SelectionCrossingDisplay1AndDisplay2()
    {
        // x spans 2400..2800, crossing the boundary at x = 2560.
        var selection = SelectionGeometry.Normalize(new Point(2400, 200), new Point(2800, 500));

        // Display1 keeps the part left of the boundary, in its local pixels.
        Assert.Equal(
            new Rectangle(2400, 200, 160, 300),
            SelectionGeometry.ToMonitorLocal(selection, Display1.Bounds));

        // Display2 keeps the part right of the boundary; local Y is relative
        // to Display2's top edge (-489), so 200 becomes 689.
        Assert.Equal(
            new Rectangle(0, 689, 240, 300),
            SelectionGeometry.ToMonitorLocal(selection, Display2.Bounds));

        Assert.Null(SelectionGeometry.ToMonitorLocal(selection, Display3.Bounds));
    }

    [Fact]
    public void ToMonitorLocal_SelectionCrossingDisplay3AndDisplay1_NegativeCoordinates()
    {
        // x spans -1500..300, crossing the boundary at x = 0 between
        // Display3 (negative side) and Display1.
        var selection = SelectionGeometry.Normalize(new Point(-1500, 600), new Point(300, 900));

        // Display3 local X: -1500 - (-1920) = 420; local Y: 600 - 361 = 239.
        Assert.Equal(
            new Rectangle(420, 239, 1500, 300),
            SelectionGeometry.ToMonitorLocal(selection, Display3.Bounds));

        // Display1 local X starts at 0; the selection reaches x = 300.
        Assert.Equal(
            new Rectangle(0, 600, 300, 300),
            SelectionGeometry.ToMonitorLocal(selection, Display1.Bounds));

        Assert.Null(SelectionGeometry.ToMonitorLocal(selection, Display2.Bounds));
    }

    [Fact]
    public void ToMonitorLocal_FullDesktop_CoversEveryMonitorExactly()
    {
        var selection = new Rectangle(VirtualBounds.Left, VirtualBounds.Top, VirtualBounds.Width, VirtualBounds.Height);

        Assert.Equal(new Rectangle(0, 0, 2560, 1440), SelectionGeometry.ToMonitorLocal(selection, Display1.Bounds));
        Assert.Equal(new Rectangle(0, 0, 1080, 1920), SelectionGeometry.ToMonitorLocal(selection, Display2.Bounds));
        Assert.Equal(new Rectangle(0, 0, 1920, 1080), SelectionGeometry.ToMonitorLocal(selection, Display3.Bounds));
    }

    [Fact]
    public void ToMonitorLocal_NoOverlap_ReturnsNull()
    {
        var selection = new Rectangle(100, 100, 50, 50); // fully inside Display1

        Assert.Null(SelectionGeometry.ToMonitorLocal(selection, Display2.Bounds));
        Assert.Null(SelectionGeometry.ToMonitorLocal(selection, Display3.Bounds));
    }

    [Theory]
    [InlineData(96)]
    [InlineData(120)]
    [InlineData(144)]
    [InlineData(192)]
    public void ToDipRectangle_LocalSelectionOffsets(int dpi)
    {
        var local = new Rectangle(10, 20, 100, 50);
        var (left, top, width, height) = MonitorGeometry.ToDipRectangle(local, dpi);

        Assert.Equal(10 * 96.0 / dpi, left, 10);
        Assert.Equal(20 * 96.0 / dpi, top, 10);
        Assert.Equal(100 * 96.0 / dpi, width, 10);
        Assert.Equal(50 * 96.0 / dpi, height, 10);
    }

    [Theory]
    [InlineData(96)]
    [InlineData(120)]
    [InlineData(144)]
    [InlineData(192)]
    public void DipToPhysical_RoundTrip_PreservesLocalSelection(int dpi)
    {
        // A local selection on Display2 (top edge at -489), converted to
        // DIPs at the given DPI and back to physical pixels.
        var local = new Rectangle(100, 300, 200, 150);

        var (left, top, width, height) = MonitorGeometry.ToDipRectangle(local, dpi);
        var roundTrip = new Rectangle(
            (int)Math.Round(left * dpi / 96.0),
            (int)Math.Round(top * dpi / 96.0),
            (int)Math.Round(width * dpi / 96.0),
            (int)Math.Round(height * dpi / 96.0));

        Assert.Equal(local, roundTrip);
    }
}
