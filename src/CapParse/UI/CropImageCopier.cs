using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace CapParse.UI;

/// <summary>
/// Copies a captured crop to the clipboard as an in-memory image. Copying
/// is always an explicit user action. The crop bitmap is never disposed by
/// this class: ownership stays with the application, which keeps the crop
/// alive until the next capture. A clipboard locked by another application
/// is transient, so the write is retried a few times with a short bounded
/// delay (see <see cref="ClipboardWritePolicy"/>) before the copy is
/// reported as a failed outcome; other clipboard failures are reported as
/// a failed outcome, not thrown. The original exception of a failed copy
/// is recorded in the opt-in CAPPARSE_TRACE log, never shown to the user.
/// </summary>
public sealed class CropImageCopier
{
    private const string NoImageMessage = "Não há imagem capturada para copiar.";
    private const string ClipboardFailureMessage =
        "Não foi possível copiar a imagem. A área de transferência pode estar em uso por outro aplicativo.";

    // Fallback used when the caller has no DPI information for the
    // selection (or the value is invalid).
    internal const int FallbackDpi = 96;

    private readonly IImageClipboard _clipboard;

    public CropImageCopier(IImageClipboard clipboard)
    {
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
    }

    /// <summary>
    /// Copies <paramref name="crop"/> to the clipboard, tagged with
    /// <paramref name="dpi"/> so DPI-aware applications paste it at its
    /// on-screen size. A null crop (capture cancelled or replaced) and a
    /// crop that was already disposed by a newer capture both fail
    /// gracefully; a clipboard failure is reported, never thrown.
    /// </summary>
    public CopyOutcome TryCopy(Bitmap? crop, int dpi)
    {
        if (crop is null)
        {
            return new CopyOutcome(false, NoImageMessage);
        }

        BitmapSource image;
        try
        {
            image = ToBitmapSource(crop, dpi);
        }
        catch
        {
            // Reading a disposed System.Drawing bitmap throws from its
            // property accessors: the crop may have been replaced by a
            // newer capture before the interface refreshed.
            return new CopyOutcome(false, NoImageMessage);
        }

        try
        {
            // A clipboard lock by another application is transient: the
            // limited retry gives the lock time to clear before the copy
            // is reported as failed. Any other exception escapes and is
            // reported below.
            ClipboardWritePolicy.WriteWithRetry(() => _clipboard.SetImage(image));
            return new CopyOutcome(true, string.Empty);
        }
        catch (Exception exception)
        {
            // The user-facing message stays generic; the original
            // exception is recorded in the opt-in CAPPARSE_TRACE log so a
            // real failure can be diagnosed without exposing technical
            // details in the interface.
            FrozenOverlaySession.Trace($"CropImageCopier: {exception}");
            return new CopyOutcome(false, ClipboardFailureMessage);
        }
    }

    /// <summary>
    /// Converts a GDI+ bitmap into a frozen WPF <see cref="BitmapSource"/>
    /// sized and DPI-tagged exactly like the source. The source's pixels
    /// are copied verbatim (32bpp ARGB maps 1:1 to WPF's Bgra32); a source
    /// in any other format is normalized with a 1:1 draw first. The source
    /// is left alive and untouched.
    /// </summary>
    /// <exception cref="ArgumentException">The bitmap was disposed, or it
    /// has no positive width or height.</exception>
    internal static BitmapSource ToBitmapSource(Bitmap bitmap, int dpi)
    {
        if (bitmap is null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        // The managed System.Drawing reports a disposed bitmap through its
        // property accessors; reading the dimensions doubles as the
        // liveness probe.
        int width = bitmap.Width;
        int height = bitmap.Height;
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException(
                $"Image must have a positive width and height (got {width}x{height}).");
        }

        if (dpi <= 0)
        {
            dpi = FallbackDpi;
        }

        byte[] pixels;
        int stride;

        if (bitmap.PixelFormat == PixelFormat.Format32bppArgb)
        {
            // The crop pipeline always produces Format32bppArgb: its BGRA
            // bytes are exactly WPF's Bgra32 layout, so the locked buffer
            // can be copied straight over.
            pixels = new byte[width * height * 4];

            var bits = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                stride = bits.Stride;
                Marshal.Copy(bits.Scan0, pixels, 0, pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bits);
            }
        }
        else
        {
            // Defensive: normalize an unexpected source format with a 1:1
            // draw into a 32bpp ARGB surface before the byte copy. The
            // fully-qualified GDI+ names and the four-argument DrawImage
            // (System.Drawing.Common has no three-argument overload) keep
            // this compiling in the WPF temp project as well.
            using var normalized = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = System.Drawing.Graphics.FromImage(normalized))
            {
                graphics.DrawImage(
                    bitmap,
                    new System.Drawing.Rectangle(0, 0, width, height),
                    new System.Drawing.Rectangle(0, 0, width, height),
                    System.Drawing.GraphicsUnit.Pixel);
            }

            pixels = new byte[width * height * 4];

            var bits = normalized.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                stride = bits.Stride;
                Marshal.Copy(bits.Scan0, pixels, 0, pixels.Length);
            }
            finally
            {
                normalized.UnlockBits(bits);
            }
        }

        // BitmapSource.Create copies the buffer; freezing makes the result
        // immutable and safe for the clipboard's deferred data rendering.
        var source = BitmapSource.Create(
            width, height, dpi, dpi, PixelFormats.Bgra32, null, pixels, stride);
        source.Freeze();

        return source;
    }
}
