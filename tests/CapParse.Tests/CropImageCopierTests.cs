using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CapParse.Platform;
using CapParse.UI;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace CapParse.Tests;

// M11: the image copy action is explicit, works on the crop bitmap alone
// (independent of the OCR state), never disposes the crop, and reports
// clipboard failures as outcomes instead of throwing. The clipboard is
// faked, so none of these checks touch the real system clipboard.
public class CropImageCopierTests
{
    [Fact]
    public void TryCopy_NullCrop_FailsWithoutTouchingTheClipboard()
    {
        var clipboard = new FakeImageClipboard();
        var copier = new CropImageCopier(clipboard);

        var outcome = copier.TryCopy(null, 96);

        Assert.False(outcome.Succeeded);
        Assert.NotEmpty(outcome.Message);
        Assert.False(clipboard.Written);
    }

    [Fact]
    public void TryCopy_KnownPixels_ProducesMatchingBitmapSource()
    {
        // 4x2 with known BGRA pixels: the WPF source must carry the same
        // dimensions, pixel format, pixels and DPI.
        var expected = new byte[]
        {
            0, 0, 0, 255,
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16,
        };
        using var crop = MakeCrop(4, 2, expected);
        var clipboard = new FakeImageClipboard();
        var copier = new CropImageCopier(clipboard);

        var outcome = copier.TryCopy(crop, 144);

        Assert.True(outcome.Succeeded);
        Assert.Empty(outcome.Message);

        Assert.NotNull(clipboard.LastImage);
        var image = clipboard.LastImage;
        Assert.Equal(4, image.PixelWidth);
        Assert.Equal(2, image.PixelHeight);
        Assert.Equal(PixelFormats.Bgra32, image.Format);
        Assert.Equal(144, image.DpiX);
        Assert.Equal(144, image.DpiY);
        Assert.True(image.IsFrozen);

        var actual = new byte[4 * 2 * 4];
        image.CopyPixels(actual, 4 * 4, 0);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryCopy_InvalidDpi_FallsBackTo96()
    {
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var clipboard = new FakeImageClipboard();
        var copier = new CropImageCopier(clipboard);

        var outcome = copier.TryCopy(crop, 0);

        Assert.True(outcome.Succeeded);
        Assert.NotNull(clipboard.LastImage);
        var image = clipboard.LastImage;
        Assert.Equal(96, image.DpiX);
        Assert.Equal(96, image.DpiY);
    }

    [Fact]
    public void TryCopy_ClipboardFailure_IsReportedAsAFailedOutcome()
    {
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var clipboard = new FakeImageClipboard(throwOnWrite: true);
        var copier = new CropImageCopier(clipboard);

        var outcome = copier.TryCopy(crop, 96);

        Assert.False(outcome.Succeeded);
        Assert.NotEmpty(outcome.Message);
    }

    [Fact]
    public void TryCopy_DisposedCrop_FailsWithoutCrashing()
    {
        var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        crop.Dispose();
        var clipboard = new FakeImageClipboard();
        var copier = new CropImageCopier(clipboard);

        var outcome = copier.TryCopy(crop, 96);

        Assert.False(outcome.Succeeded);
        Assert.NotEmpty(outcome.Message);
        Assert.False(clipboard.Written);
    }

    [Fact]
    public void TryCopy_NonArgbSource_IsNormalizedToBgra32()
    {
        // Defensive path: a source in another format is normalized with a
        // 1:1 draw before the byte copy. FromArgb(A, R, G, B) stores as
        // BGRA bytes in memory: [B, G, R, A].
        var crop = new Bitmap(2, 1, PixelFormat.Format24bppRgb);
        crop.SetPixel(0, 0, System.Drawing.Color.FromArgb(255, 0, 128));
        crop.SetPixel(1, 0, System.Drawing.Color.FromArgb(255, 0, 255, 0));
        var clipboard = new FakeImageClipboard();
        var copier = new CropImageCopier(clipboard);

        var outcome = copier.TryCopy(crop, 96);
        crop.Dispose();

        Assert.True(outcome.Succeeded);
        Assert.NotNull(clipboard.LastImage);
        var image = clipboard.LastImage;
        Assert.Equal(PixelFormats.Bgra32, image.Format);

        var actual = new byte[2 * 1 * 4];
        image.CopyPixels(actual, 2 * 4, 0);
        Assert.Equal(new byte[] { 128, 0, 255, 255, 0, 255, 0, 255 }, actual);
    }

    [Fact]
    public void ToBitmapSource_LeavesTheSourceAlive()
    {
        using var crop = MakeCrop(2, 1, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        var image = CropImageCopier.ToBitmapSource(crop, 96);

        // The conversion must not dispose the source: the application keeps
        // owning the crop until the next capture.
        Assert.Equal(2, crop.Width);
        Assert.Equal(1, crop.Height);
        Assert.Equal(2, image.PixelWidth);
    }

    [Fact]
    public void GetEffectiveDpiForSelection_ReturnsAUsableValue()
    {
        // Headless machines have no monitor: the query must fall back to
        // 96. On a machine with displays the result is a real effective DPI.
        int dpi = MonitorDiscovery.GetEffectiveDpiForSelection(new Rectangle(0, 0, 100, 100));

        if (MonitorDiscovery.GetMonitors().Count == 0)
        {
            Assert.Equal(96, dpi);
        }
        else
        {
            Assert.InRange(dpi, 96, 1536);
        }
    }

    private static Bitmap MakeCrop(int width, int height, byte[] bgra)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        var bits = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadWrite,
            PixelFormat.Format32bppArgb);
        try
        {
            Marshal.Copy(bgra, 0, bits.Scan0, bgra.Length);
        }
        finally
        {
            bitmap.UnlockBits(bits);
        }

        return bitmap;
    }

    private sealed class FakeImageClipboard : IImageClipboard
    {
        private readonly bool _throwOnWrite;

        public FakeImageClipboard(bool throwOnWrite = false)
        {
            _throwOnWrite = throwOnWrite;
        }

        public BitmapSource? LastImage { get; private set; }

        public bool Written { get; private set; }

        public void SetImage(BitmapSource image)
        {
            if (_throwOnWrite)
            {
                throw new ExternalException("The clipboard is in use by another application.");
            }

            Written = true;
            LastImage = image;
        }
    }
}
