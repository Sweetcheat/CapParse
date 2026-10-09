using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
// Aliases: the project's implicit usings bring in System.Drawing (and
// System.Windows.Forms) types with the same names; the WPF ones win here.
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace CapParse.UI;

/// <summary>
/// Borderless, topmost overlay window covering a single monitor. Displays
/// that monitor's region of the frozen desktop image with a dim layer on
/// top. M5-A: no selection UI; the mouse is ignored.
/// </summary>
public sealed class MonitorOverlayWindow : Window
{
    private readonly Image _image;

    public MonitorOverlayWindow(double dimOpacity)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Background = Brushes.Black;

        _image = new Image
        {
            // The source is exactly the monitor's pixel region and the
            // window is exactly the monitor's size, so Fill maps it 1:1
            // with no distortion (portrait included).
            Stretch = Stretch.Fill
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
                }
            }
        };
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
}
