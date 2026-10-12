using System.Windows.Media.Imaging;
using Clipboard = System.Windows.Clipboard;

namespace CapParse.UI;

/// <summary>
/// Writes an image to the system clipboard. The implementation must be
/// called on the WPF UI thread, where the clipboard APIs require it.
/// </summary>
public interface IImageClipboard
{
    /// <exception cref="System.Runtime.InteropServices.ExternalException">
    /// The clipboard is unavailable or locked by another application.</exception>
    void SetImage(BitmapSource image);
}

/// <summary>
/// <see cref="IImageClipboard"/> backed by the WPF clipboard. The image is
/// placed in memory as a DIB: no temporary file is written, and the
/// clipboard takes its own copy of the pixels, so the caller keeps
/// ownership of the <see cref="BitmapSource"/>.
/// </summary>
public sealed class WpfImageClipboard : IImageClipboard
{
    public void SetImage(BitmapSource image)
    {
        if (image is null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        Clipboard.SetImage(image);
    }
}
