using System.Drawing;
using System.Drawing.Imaging;
using CapParse.Capture;

namespace CapParse.Tests;

// Deterministic tests for the M5-C crop: pixel-exact region copies out of
// synthetic in-memory bitmaps. No screen access, no monitors, no WPF.
//
// The fixture mimics a multi-monitor virtual desktop with a negative
// origin on both axes: the captured image is 32x16 and its pixel (0, 0)
// corresponds to virtual-desktop point (-8, -4). A "monitor boundary"
// runs through the image at local x = 16 (global x = 8).
public class CaptureCropperTests
{
    private static readonly Rectangle VirtualBounds = new(-8, -4, 32, 16);

    private static readonly Color Red = Color.FromArgb(255, 255, 0, 0);
    private static readonly Color Green = Color.FromArgb(255, 0, 255, 0);
    private static readonly Color Blue = Color.FromArgb(255, 0, 0, 255);
    private static readonly Color Transparent = Color.FromArgb(0, 0, 0, 0);
    private static readonly Color SemiTransparent = Color.FromArgb(128, 255, 0, 0);

    /// <summary>
    /// 32x16, 32bpp ARGB. Quadrants (16x8 each): top-left red, top-right
    /// green, bottom-left blue, bottom-right transparent except one
    /// semi-transparent pixel at local (30, 14). Pixels are written with
    /// SetPixel (not Graphics.FillRectangle) so the fixture has exact,
    /// predictable values: GDI+ fill compositing on 32bpp ARGB surfaces
    /// is not pixel-exact and would leak into the assertions.
    /// </summary>
    private static Bitmap CreateTestImage()
    {
        var image = new Bitmap(32, 16, PixelFormat.Format32bppArgb);

        for (var y = 0; y < 16; y++)
        {
            for (var x = 0; x < 32; x++)
            {
                var color = x < 16
                    ? (y < 8 ? Red : Blue)
                    : (y < 8 ? Green : Transparent);

                image.SetPixel(x, y, color);
            }
        }

        image.SetPixel(30, 14, SemiTransparent);

        return image;
    }

    private static CaptureResult CreateResult(Bitmap image)
    {
        return new CaptureResult(image, VirtualBounds);
    }

    // Global -> local: local = global - (-8, -4).

    [Fact]
    public void Crop_InternalRegion_ReturnsExactPixels()
    {
        using var result = CreateResult(CreateTestImage());
        // Global (0, 0) 8x4 -> local (8, 4) 8x4, inside the red quadrant.
        using var crop = CaptureCropper.Crop(result, new Rectangle(0, 0, 8, 4));

        Assert.Equal(8, crop.Width);
        Assert.Equal(4, crop.Height);
        Assert.Equal(PixelFormat.Format32bppArgb, crop.PixelFormat);

        Assert.Equal(Red, crop.GetPixel(0, 0));
        Assert.Equal(Red, crop.GetPixel(7, 0));
        Assert.Equal(Red, crop.GetPixel(0, 3));
        Assert.Equal(Red, crop.GetPixel(7, 3));
        Assert.Equal(Red, crop.GetPixel(4, 2));
    }

    [Fact]
    public void Crop_StartingAtImageOrigin_UsesNegativeVirtualOrigin()
    {
        using var result = CreateResult(CreateTestImage());
        // Image pixel (0, 0) is virtual point (-8, -4), not (0, 0).
        using var crop = CaptureCropper.Crop(result, new Rectangle(-8, -4, 4, 4));

        Assert.Equal(Red, crop.GetPixel(0, 0));
        Assert.Equal(Red, crop.GetPixel(3, 3));
    }

    [Fact]
    public void Crop_EndingAtRightAndBottomEdges_UsesExclusiveEdges()
    {
        using var result = CreateResult(CreateTestImage());
        // The full image: exactly 32x16, no off-by-one on the right/bottom.
        using var crop = CaptureCropper.Crop(result,
            new Rectangle(VirtualBounds.Left, VirtualBounds.Top, VirtualBounds.Width, VirtualBounds.Height));

        Assert.Equal(32, crop.Width);
        Assert.Equal(16, crop.Height);

        Assert.Equal(Red, crop.GetPixel(0, 0));
        Assert.Equal(Green, crop.GetPixel(31, 0));
        Assert.Equal(Blue, crop.GetPixel(0, 15));
        Assert.Equal(Transparent, crop.GetPixel(31, 15));
    }

    [Fact]
    public void Crop_NegativeGlobalCoordinates_MapsToPositiveImageOffsets()
    {
        using var result = CreateResult(CreateTestImage());
        // Global (-6, -2) 6x6 -> local (2, 2) 6x6. A naive implementation
        // that indexes the bitmap with selection.X/Y would be wrong here.
        using var crop = CaptureCropper.Crop(result, new Rectangle(-6, -2, 6, 6));

        Assert.Equal(6, crop.Width);
        Assert.Equal(6, crop.Height);
        Assert.Equal(Red, crop.GetPixel(0, 0));
        Assert.Equal(Red, crop.GetPixel(5, 5));
    }

    [Fact]
    public void Crop_RegionOnSecondMonitor_ReturnsThatMonitorSPixels()
    {
        using var result = CreateResult(CreateTestImage());
        // Global (12, 0) 6x4 -> local (20, 4): fully right of the boundary
        // at local x = 16, i.e. on the "second monitor".
        using var crop = CaptureCropper.Crop(result, new Rectangle(12, 0, 6, 4));

        Assert.Equal(Green, crop.GetPixel(0, 0));
        Assert.Equal(Green, crop.GetPixel(5, 3));
    }

    [Fact]
    public void Crop_CrossingMonitorBoundary_PreservesBothSides()
    {
        using var result = CreateResult(CreateTestImage());
        // Global (4, -2) 12x8 -> local (12, 2) 12x8: spans local x = 12..23
        // (crossing the boundary at x = 16) and local y = 2..9 (crossing the
        // top/bottom quadrant edge at y = 8).
        using var crop = CaptureCropper.Crop(result, new Rectangle(4, -2, 12, 8));

        Assert.Equal(12, crop.Width);
        Assert.Equal(8, crop.Height);

        Assert.Equal(Red, crop.GetPixel(0, 0));   // local (12, 2): left of boundary, top
        Assert.Equal(Red, crop.GetPixel(3, 0));   // local (15, 2): last pixel left of boundary
        Assert.Equal(Green, crop.GetPixel(4, 0)); // local (16, 2): first pixel right of boundary
        Assert.Equal(Blue, crop.GetPixel(0, 6));  // local (12, 8): below the y = 8 edge
        Assert.Equal(Green, crop.GetPixel(11, 5)); // local (23, 7): right of both edges
        Assert.Equal(Transparent, crop.GetPixel(11, 7)); // local (23, 9): bottom-right quadrant
    }

    [Fact]
    public void Crop_PreservesExactDimensions()
    {
        using var result = CreateResult(CreateTestImage());

        using var crop = CaptureCropper.Crop(result, new Rectangle(-2, 3, 7, 5));

        Assert.Equal(7, crop.Width);
        Assert.Equal(5, crop.Height);
    }

    [Fact]
    public void Crop_PreservesTransparency()
    {
        using var result = CreateResult(CreateTestImage());
        // Global (16, 4) 8x8 -> local (24, 8): the transparent bottom-right
        // quadrant, including the semi-transparent pixel at local (30, 14).
        using var crop = CaptureCropper.Crop(result, new Rectangle(16, 4, 8, 8));

        Assert.Equal(8, crop.Width);
        Assert.Equal(8, crop.Height);

        Assert.Equal(Transparent, crop.GetPixel(0, 0));
        Assert.Equal(Transparent, crop.GetPixel(7, 7));
        Assert.Equal(SemiTransparent, crop.GetPixel(6, 6)); // local (30, 14)
    }

    [Fact]
    public void Crop_OriginalImageRemainsIntact()
    {
        var result = CreateResult(CreateTestImage());
        var original = result.Image;

        var snapshot = new Color[original.Width * original.Height];
        for (var y = 0; y < original.Height; y++)
        {
            for (var x = 0; x < original.Width; x++)
            {
                snapshot[y * original.Width + x] = original.GetPixel(x, y);
            }
        }

        using var crop = CaptureCropper.Crop(result,
            new Rectangle(VirtualBounds.Left, VirtualBounds.Top, VirtualBounds.Width, VirtualBounds.Height));

        for (var y = 0; y < original.Height; y++)
        {
            for (var x = 0; x < original.Width; x++)
            {
                Assert.Equal(snapshot[y * original.Width + x], original.GetPixel(x, y));
            }
        }

        crop.Dispose();
        result.Dispose();
    }

    [Fact]
    public void Crop_StaysValidAfterSourceIsDisposed()
    {
        var result = CreateResult(CreateTestImage());

        var crop = CaptureCropper.Crop(result, new Rectangle(-8, -4, 32, 16));
        result.Dispose();

        // The crop is an independent copy: disposing the source (which
        // disposes the captured bitmap) must not invalidate it.
        Assert.Equal(Red, crop.GetPixel(0, 0));
        Assert.Equal(Green, crop.GetPixel(31, 0));
        Assert.Equal(Blue, crop.GetPixel(0, 15));
        Assert.Equal(Transparent, crop.GetPixel(31, 15));

        crop.Dispose();
    }

    [Fact]
    public void Crop_PreviousCropCanBeDisposedIndependently()
    {
        using var result = CreateResult(CreateTestImage());

        var first = CaptureCropper.Crop(result, new Rectangle(-8, -4, 16, 8));
        // Global (12, 0) 12x8 -> local (20, 4): the green quadrant.
        var second = CaptureCropper.Crop(result, new Rectangle(12, 0, 12, 8));

        first.Dispose();

        // Disposing one crop (or the source) must not affect the other.
        Assert.Equal(Green, second.GetPixel(0, 0));
        result.Dispose();
        Assert.Equal(Green, second.GetPixel(0, 0));

        second.Dispose();
    }

    [Theory]
    [InlineData(0, 4)] // zero width
    [InlineData(4, 0)] // zero height
    public void Crop_ZeroWidthOrHeight_Throws(int width, int height)
    {
        using var result = CreateResult(CreateTestImage());

        Assert.Throws<ArgumentException>(
            () => CaptureCropper.Crop(result, new Rectangle(0, 0, width, height)));
    }

    [Theory]
    [InlineData(-5, 10)]
    [InlineData(5, -10)]
    [InlineData(-5, -10)]
    public void Crop_NegativeDimensions_Throws(int width, int height)
    {
        using var result = CreateResult(CreateTestImage());

        Assert.Throws<ArgumentException>(
            () => CaptureCropper.Crop(result, new Rectangle(0, 0, width, height)));
    }

    [Theory]
    [InlineData(-20, 0)]  // left of the image (local x = -12)
    [InlineData(24, 0)]   // right of the image (local x = 32)
    [InlineData(-8, -8)]  // above the image (local y = -4)
    [InlineData(0, 12)]   // below the image (local y = 16)
    public void Crop_FullyOutsideImage_Throws(int x, int y)
    {
        using var result = CreateResult(CreateTestImage());

        Assert.Throws<ArgumentException>(
            () => CaptureCropper.Crop(result, new Rectangle(x, y, 4, 4)));
    }

    [Fact]
    public void Crop_PartiallyOutsideImage_Throws()
    {
        using var result = CreateResult(CreateTestImage());
        // Global (20, 0) 8x4 -> local (28, 4): starts inside, ends at
        // local x = 36, past the right edge (32).
        Assert.Throws<ArgumentException>(
            () => CaptureCropper.Crop(result, new Rectangle(20, 0, 8, 4)));
    }

    [Fact]
    public void Crop_ExtremeCoordinates_CannotOverflow_Throws()
    {
        using var result = CreateResult(CreateTestImage());

        // int.MaxValue - VirtualBounds.Left overflows int; the check must
        // reject it instead of wrapping into a valid-looking offset.
        Assert.Throws<ArgumentException>(
            () => CaptureCropper.Crop(result, new Rectangle(int.MaxValue, 0, 1, 1)));
        Assert.Throws<ArgumentException>(
            () => CaptureCropper.Crop(result, new Rectangle(int.MinValue, 0, 1, 1)));
    }

    [Fact]
    public void Crop_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => CaptureCropper.Crop(null!, new Rectangle(0, 0, 4, 4)));
    }

    [Fact]
    public void Crop_DisposedSource_Throws()
    {
        var result = CreateResult(CreateTestImage());
        result.Dispose();

        Assert.Throws<ObjectDisposedException>(
            () => CaptureCropper.Crop(result, new Rectangle(0, 0, 4, 4)));
    }
}
