using System.Drawing;

namespace CapParse.Capture;

/// <summary>
/// Pure geometry of a rectangular selection, in virtual-desktop physical
/// coordinates. The right and bottom edges are exclusive (System.Drawing
/// Rectangle semantics, as in Rectangle.FromLTRB), so a selection turns
/// directly into an image region with no off-by-one correction.
/// </summary>
public static class SelectionGeometry
{
    /// <summary>
    /// Normalizes two global physical points into a selection rectangle, in
    /// any drag direction. Left/top take the smaller coordinate, right/bottom
    /// the larger one (exclusive edges).
    /// </summary>
    public static Rectangle Normalize(Point a, Point b)
    {
        return Rectangle.FromLTRB(
            Math.Min(a.X, b.X),
            Math.Min(a.Y, b.Y),
            Math.Max(a.X, b.X),
            Math.Max(a.Y, b.Y));
    }

    /// <summary>
    /// A selection is usable only if it encloses at least one full pixel in
    /// both dimensions. Zero-area selections (identical points, horizontal
    /// or vertical drags) are not usable.
    /// </summary>
    public static bool IsValid(Rectangle selection)
    {
        return selection.Width > 0 && selection.Height > 0;
    }

    /// <summary>
    /// Maps a global physical point to its offset inside the captured
    /// virtual-desktop image. Pixel (0, 0) of the image is
    /// (virtualBounds.Left, virtualBounds.Top) in virtual-desktop
    /// coordinates.
    /// </summary>
    public static Point ToImageOffset(Point physical, Rectangle virtualBounds)
    {
        return new Point(physical.X - virtualBounds.Left, physical.Y - virtualBounds.Top);
    }

    /// <summary>
    /// Maps a global physical selection to its region inside the captured
    /// image. The result keeps the exclusive right/bottom edges.
    /// </summary>
    public static Rectangle ToImageRectangle(Rectangle physical, Rectangle virtualBounds)
    {
        return new Rectangle(
            physical.Left - virtualBounds.Left,
            physical.Top - virtualBounds.Top,
            physical.Width,
            physical.Height);
    }

    /// <summary>
    /// The part of a global physical selection that falls inside a monitor,
    /// in the monitor's local physical pixels (origin at the monitor's
    /// top-left corner). Returns null when the selection does not overlap
    /// the monitor.
    /// </summary>
    public static Rectangle? ToMonitorLocal(Rectangle selection, Rectangle monitorBounds)
    {
        var inter = Rectangle.Intersect(selection, monitorBounds);

        if (inter.Width <= 0 || inter.Height <= 0)
        {
            return null;
        }

        return new Rectangle(
            inter.Left - monitorBounds.Left,
            inter.Top - monitorBounds.Top,
            inter.Width,
            inter.Height);
    }
}
