using System.Windows;
using System.Windows.Media;
using CapParse.UI;

namespace CapParse.Tests;

// Pure geometry tests of the capture overlay's dim layer (V0.2 phase 5):
// the dim is a path that covers the full monitor surface and, while a
// selection is in progress, carries an even-odd hole over it, so the
// frozen content shows at full brightness inside the selection. No window,
// no capture, no screen access is needed.
public class MonitorOverlayWindowTests
{
    [Fact]
    public void BuildDimGeometry_NoSelection_IsTheFullSurface()
    {
        var geometry = MonitorOverlayWindow.BuildDimGeometry(1920, 1080, null);

        Assert.True(geometry.IsFrozen);
        var group = Assert.IsType<GeometryGroup>(geometry);
        Assert.Equal(FillRule.EvenOdd, group.FillRule);
        var children = group.Children;

        Assert.Single(children);
        var full = Assert.IsType<RectangleGeometry>(children[0]);
        Assert.Equal(new Rect(0, 0, 1920, 1080), full.Rect);
    }

    [Fact]
    public void BuildDimGeometry_WithSelection_AddsAHoleOverIt()
    {
        var selection = new Rect(100, 200, 300, 150);
        var geometry = MonitorOverlayWindow.BuildDimGeometry(1920, 1080, selection);

        Assert.True(geometry.IsFrozen);
        var group = Assert.IsType<GeometryGroup>(geometry);
        Assert.Equal(FillRule.EvenOdd, group.FillRule);
        var children = group.Children;

        Assert.Equal(2, children.Count);
        var full = Assert.IsType<RectangleGeometry>(children[0]);
        Assert.Equal(new Rect(0, 0, 1920, 1080), full.Rect);

        // The hole is the selection, in the window's DIPs.
        var hole = Assert.IsType<RectangleGeometry>(children[1]);
        Assert.Equal(selection, hole.Rect);
    }

    [Fact]
    public void BuildDimGeometry_ZeroAreaSelection_IsIgnored()
    {
        // A zero-area selection (identical points, a purely horizontal or
        // vertical drag) must not add a degenerate hole: the dim stays the
        // full surface, exactly as before the drag began.
        AssertZeroArea(new Rect(100, 200, 0, 150));
        AssertZeroArea(new Rect(100, 200, 300, 0));
        AssertZeroArea(new Rect(100, 200, 0, 0));

        void AssertZeroArea(Rect selection)
        {
            var geometry = MonitorOverlayWindow.BuildDimGeometry(1920, 1080, selection);
            var group = Assert.IsType<GeometryGroup>(geometry);

            Assert.Single(group.Children);
        }
    }

    [Fact]
    public void BuildDimGeometry_SelectionOutsideTheSurface_IsKeptAsGiven()
    {
        // The session only ever passes the part of the selection that
        // falls inside this monitor, so clipping is its responsibility:
        // the geometry builder records the hole as given.
        var selection = new Rect(1800, 1000, 400, 200);
        var geometry = MonitorOverlayWindow.BuildDimGeometry(1920, 1080, selection);
        var group = Assert.IsType<GeometryGroup>(geometry);

        Assert.Equal(2, group.Children.Count);
        var hole = Assert.IsType<RectangleGeometry>(group.Children[1]);
        Assert.Equal(selection, hole.Rect);
    }
}
