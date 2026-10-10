using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace CapParse.Tests;

/// <summary>
/// Keeps the committed M7-C collection honest. It re-renders every case
/// from <c>TestData/Ocr/manifest.json</c> and compares it pixel by pixel
/// with the committed PNGs:
///
/// - files missing  → they are written (bootstrap for a checkout where the
///   images were deleted) and the test fails, asking for a review of the
///   regenerated collection;
/// - files present  → they must match within a small tolerance. GDI+
///   anti-aliasing jitters by a couple of edge pixels across processes on
///   the reference machine (measured: at most 2 pixels, max channel delta
///   36), so the tolerance absorbs that noise while any real change
///   (content, font, layout, colors) alters many pixels with large deltas
///   and fails the test.
///
/// Regenerate deliberately: delete the PNGs under
/// <c>tests/CapParse.Tests/TestData/Ocr/</c> and run
/// <c>dotnet test --filter "FullyQualifiedName~OcrCollectionGeneratorTests"</c>.
/// </summary>
public class OcrCollectionGeneratorTests
{
    /// <summary>
    /// Measured on the reference machine: the worst cross-process GDI+
    /// anti-aliasing jitter was 2 differing pixels with a max channel
    /// delta of 36. The thresholds keep a wide margin over that noise
    /// floor while any deliberate change to the manifest blows far past
    /// them.
    /// </summary>
    private const double MaxDifferingPixelFraction = 0.005;

    private const int MaxChannelDelta = 96;

    /// <summary>
    /// The collection lives in the repository (it is versioned); the test
    /// binary sees a build-output copy, so generation targets the source
    /// folder three levels above the output directory.
    /// </summary>
    private static string TestDataDirectory =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Ocr"));

    [Fact]
    public void CommittedImages_MatchManifestRendering()
    {
        var manifest = OcrCollectionManifest.Load(TestDataDirectory);
        var created = new List<string>();
        var drifted = new List<string>();

        foreach (var caseSpec in manifest.Cases)
        {
            using var rendered = caseSpec.Render(manifest.Font, manifest.Dpi);
            var file = Path.Combine(TestDataDirectory, caseSpec.File);

            if (!File.Exists(file))
            {
                rendered.Save(file, ImageFormat.Png);
                created.Add(caseSpec.File);
                continue;
            }

            using var committed = new Bitmap(file);
            var (differing, maxDelta) = ComparePixels(committed, rendered);

            var threshold = caseSpec.Width * caseSpec.Height * MaxDifferingPixelFraction;
            if (differing > threshold || maxDelta > MaxChannelDelta)
            {
                drifted.Add($"{caseSpec.File} (differing={differing}, maxDelta={maxDelta})");
            }
        }

        if (created.Count > 0)
        {
            Assert.True(
                false,
                "Bootstrapped missing collection images: " + string.Join(", ", created) +
                ". Review the regenerated files, then re-run the test to confirm they are stable.");
        }

        Assert.True(
            drifted.Count == 0,
            "The committed images no longer match the manifest rendering: " + string.Join(", ", drifted) +
            ". If the manifest changed on purpose, delete the PNGs and regenerate the collection deliberately.");
    }

    /// <summary>
    /// Counts how many pixels differ and the largest per-pixel delta
    /// (sum of the three channel differences) between two same-sized
    /// bitmaps.
    /// </summary>
    private static (long DifferingPixels, int MaxDelta) ComparePixels(Bitmap left, Bitmap right)
    {
        if (left.Width != right.Width || left.Height != right.Height)
        {
            throw new InvalidOperationException(
                $"Size mismatch: {left.Width}x{left.Height} vs {right.Width}x{right.Height}.");
        }

        var leftInfo = left.LockBits(
            new Rectangle(0, 0, left.Width, left.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var rightInfo = right.LockBits(
            new Rectangle(0, 0, right.Width, right.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            var leftBytes = new byte[leftInfo.Stride * left.Height];
            var rightBytes = new byte[rightInfo.Stride * right.Height];

            Marshal.Copy(leftInfo.Scan0, leftBytes, 0, leftBytes.Length);
            Marshal.Copy(rightInfo.Scan0, rightBytes, 0, rightBytes.Length);

            long differing = 0;
            int maxDelta = 0;

            for (long offset = 0; offset < leftBytes.Length; offset += 4)
            {
                int delta =
                    Math.Abs(leftBytes[offset] - rightBytes[offset]) +
                    Math.Abs(leftBytes[offset + 1] - rightBytes[offset + 1]) +
                    Math.Abs(leftBytes[offset + 2] - rightBytes[offset + 2]);

                if (delta > 0)
                {
                    differing++;
                    maxDelta = Math.Max(maxDelta, delta);
                }
            }

            return (differing, maxDelta);
        }
        finally
        {
            left.UnlockBits(leftInfo);
            right.UnlockBits(rightInfo);
        }
    }
}
