using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
// Aliases: the project's implicit usings bring in System.Drawing and
// System.Windows.Forms types with the same names as WPF types; each alias
// pins down which one wins in this file.
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using Path = System.Windows.Shapes.Path;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

namespace CapParse.UI;

/// <summary>
/// Borderless, topmost overlay window covering a single monitor. Displays
/// that monitor's region of the frozen desktop image with a dim layer on
/// top; while a selection is in progress the dim layer has a hole over the
/// selection, so the selected area shows the frozen content at full
/// brightness (the highlight) and is outlined with the theme's accent
/// border. During a selection drag it shows the part of the selection that
/// falls inside this monitor and reports the global physical cursor
/// position (via GetCursorPos) to the owning session.
/// </summary>
public sealed class MonitorOverlayWindow : Window
{
    private readonly Image _image;
    private readonly Path _dimPath;
    private readonly Rectangle _selection;
    private bool _dragging;

    /// <summary>
    /// Invoked with the global physical cursor position when the user
    /// presses the left mouse button on this overlay.
    /// </summary>
    public Action<Point>? DragStarted;

    /// <summary>
    /// Invoked with the global physical cursor position while the left
    /// mouse button is held down.
    /// </summary>
    public Action<Point>? DragUpdated;

    /// <summary>
    /// Invoked with the global physical cursor position when the user
    /// releases the left mouse button.
    /// </summary>
    public Action<Point>? DragEnded;

    public MonitorOverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        // Layered window from birth (MewuAI's approach): the DWM surface of
        // a transparent window is transparent in every state, including the
        // unpainted one. With the previous opaque window plus a black
        // background, any frame presented before the frozen image was
        // painted showed a black rectangle on the monitors (the observed
        // flash); with a transparent background the worst case is the live
        // desktop showing through for a frame, which is indistinguishable
        // from the frozen one.
        AllowsTransparency = true;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Background = Brushes.Transparent;
        // WPF on .NET 8 has no Cursors.Crosshair (it was dropped in the
        // .NET Core port); the cross cursor is the selection cursor.
        Cursor = System.Windows.Input.Cursors.Cross;

        _image = new Image
        {
            // The source is exactly the monitor's pixel region and the
            // window is exactly the monitor's size, so Fill maps it 1:1
            // with no distortion (portrait included).
            Stretch = Stretch.Fill
        };

        // The dim layer is a path, not a plain rectangle: while a selection
        // is in progress it carries an even-odd hole over the selection, so
        // the frozen content shows at full brightness inside it (the
        // highlight) while the surroundings stay dimmed. With no selection
        // the geometry is the full surface (the original uniform dim).
        _dimPath = new Path
        {
            Fill = ResolveBrush(
                "OverlayDimBrush",
                new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)))
        };

        // Discreet treatment of the selected area: a thin accent border
        // over the undimmed (cut out) region; no extra fill, the cutout
        // itself is the highlight.
        _selection = new Rectangle
        {
            Stroke = ResolveBrush(
                "AccentBrush",
                new SolidColorBrush(Color.FromRgb(0x2E, 0x6B, 0xE6))),
            StrokeThickness = 2,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Collapsed
        };

        Content = new Grid
        {
            Children =
            {
                _image,
                _dimPath,
                _selection
            }
        };

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftUp;
    }

    /// <summary>
    /// Sets the frozen monitor image and the dim layer's full-surface
    /// geometry. The window is placed on its monitor (and has the correct
    /// DpiScale and size) before the image is created, so the image's DPI
    /// matches the window's, the Fill stretch is 1:1, and the dim covers
    /// the whole monitor from the first presented frame.
    /// </summary>
    public void SetFrozenImage(BitmapSource frozenImage)
    {
        _image.Source = frozenImage;

        var size = SurfaceSize();
        _dimPath.Data = BuildDimGeometry(size.Width, size.Height, null);
    }

    /// <summary>
    /// Shows the part of the selection that falls inside this monitor, in
    /// this window's DIPs (origin at the monitor's top-left corner), and
    /// cuts a hole in the dim layer over it so the selected area is
    /// highlighted at full brightness.
    /// </summary>
    public void ShowSelection(double left, double top, double width, double height)
    {
        _selection.Margin = new Thickness(left, top, 0, 0);
        _selection.Width = width;
        _selection.Height = height;
        _selection.Visibility = Visibility.Visible;

        var size = SurfaceSize();
        _dimPath.Data = BuildDimGeometry(
            size.Width, size.Height, new Rect(left, top, width, height));
    }

    /// <summary>
    /// Hides the selection visual (used when the selection no longer
    /// overlaps this monitor, or before a drag begins) and restores the
    /// uniform dim over the whole surface.
    /// </summary>
    public void HideSelection()
    {
        _selection.Visibility = Visibility.Collapsed;

        var size = SurfaceSize();
        _dimPath.Data = BuildDimGeometry(size.Width, size.Height, null);
    }

    /// <summary>
    /// The window's size in DIPs. The session sizes the window explicitly
    /// (Width/Height) before the image is set; ActualWidth covers the
    /// (never used in production) auto-size case.
    /// </summary>
    private Size SurfaceSize()
    {
        double width = double.IsNaN(Width) ? ActualWidth : Width;
        double height = double.IsNaN(Height) ? ActualHeight : Height;
        return new Size(width, height);
    }

    /// <summary>
    /// The dim layer's geometry: the full surface with an even-odd hole
    /// over the selection (when there is one), so the frozen image shows
    /// at full brightness inside the selection while the surroundings stay
    /// dimmed. Pure function of the surface size and the selection, both
    /// in the window's DIPs - testable without a window.
    /// </summary>
    internal static Geometry BuildDimGeometry(
        double width, double height, Rect? selection)
    {
        var geometry = new GeometryGroup { FillRule = FillRule.EvenOdd };
        geometry.Children.Add(new RectangleGeometry(new Rect(0, 0, width, height)));

        if (selection is { } hole && hole.Width > 0 && hole.Height > 0)
        {
            geometry.Children.Add(new RectangleGeometry(hole));
        }

        geometry.Freeze();
        return geometry;
    }

    /// <summary>
    /// Resolves a theme token from the application resources (the theme
    /// dictionaries are merged into Application.Resources at startup). The
    /// fallback keeps the overlay correct where no application resources
    /// exist (test harnesses that build windows without an Application).
    /// The overlay is created per capture, so it always reflects the theme
    /// active when the capture starts.
    /// </summary>
    private static Brush ResolveBrush(string token, Brush fallback)
    {
        var application = Application.Current;

        return application is not null
                   && application.TryFindResource(token) is Brush brush
            ? brush
            : fallback;
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_dragging)
        {
            return;
        }

        _dragging = true;

        // With the mouse captured, this window keeps receiving
        // MouseMove/MouseLeftUp even when the cursor leaves it, so a drag
        // can continue across monitors. The position is read with
        // GetCursorPos, not from the WPF event: when the cursor is on
        // another (possibly different-DPI) monitor, WPF local coordinates
        // would force a DIP round-trip, while the session works in physical
        // pixels end to end.
        Mouse.Capture(this);
        DragStarted?.Invoke(NativeCursorPos());
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragging)
        {
            DragUpdated?.Invoke(NativeCursorPos());
        }
    }

    private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        Mouse.Capture(null);
        DragEnded?.Invoke(NativeCursorPos());
    }

    /// <summary>
    /// The cursor position in physical virtual-desktop pixels. GetCursorPos
    /// returns virtual-screen coordinates, which are exactly the physical
    /// coordinate space the capture result is expressed in.
    /// </summary>
    private static Point NativeCursorPos()
    {
        GetCursorPos(out NativePoint p);
        return new Point(p.X, p.Y);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint lpPoint);
}
