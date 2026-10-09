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
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace CapParse.UI;

/// <summary>
/// Borderless, topmost overlay window covering a single monitor. Displays
/// that monitor's region of the frozen desktop image with a dim layer on
/// top. During a selection drag it shows the part of the selection that
/// falls inside this monitor and reports the global physical cursor
/// position (via GetCursorPos) to the owning session.
/// </summary>
public sealed class MonitorOverlayWindow : Window
{
    private readonly Image _image;
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

    public MonitorOverlayWindow(double dimOpacity)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Background = Brushes.Black;
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

        _selection = new Rectangle
        {
            Stroke = Brushes.White,
            StrokeThickness = 1,
            // Discreet treatment of the selected area: a thin border over a
            // barely visible fill; the dim layer is untouched elsewhere.
            Fill = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Collapsed
        };

        Content = new Grid
        {
            Children =
            {
                _image,
                new Rectangle
                {
                    Fill = new SolidColorBrush(
                        Color.FromArgb((byte)Math.Round(dimOpacity * 255), 0, 0, 0))
                },
                _selection
            }
        };

        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftUp;
    }

    /// <summary>
    /// Sets the frozen monitor image. The window is placed on its monitor
    /// (and has the correct DpiScale) before the image is created, so the
    /// image's DPI matches the window's and the Fill stretch is 1:1.
    /// </summary>
    public void SetFrozenImage(BitmapSource frozenImage)
    {
        _image.Source = frozenImage;
    }

    /// <summary>
    /// Shows the part of the selection that falls inside this monitor, in
    /// this window's DIPs (origin at the monitor's top-left corner).
    /// </summary>
    public void ShowSelection(double left, double top, double width, double height)
    {
        _selection.Margin = new Thickness(left, top, 0, 0);
        _selection.Width = width;
        _selection.Height = height;
        _selection.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides the selection visual (used when the selection no longer
    /// overlaps this monitor, or before a drag begins).
    /// </summary>
    public void HideSelection()
    {
        _selection.Visibility = Visibility.Collapsed;
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
