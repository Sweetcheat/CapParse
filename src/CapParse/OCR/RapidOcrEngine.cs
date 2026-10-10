using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using RapidOcrNet;
using SkiaSharp;

namespace CapParse.OCR;

/// <summary>
/// <see cref="IOcrEngine"/> backed by RapidOcrNet with the PP-OCRv6 Small
/// preset. The engine owns the <c>RapidOcr</c> instance it creates and
/// releases it in <see cref="Dispose"/>. It never disposes the caller's
/// bitmap, and initialization failures leave no partially initialized
/// engine: the constructor either succeeds or throws.
/// </summary>
public sealed class RapidOcrEngine : IOcrEngine, IDisposable
{
    private readonly RapidOcr _ocr;
    private bool _disposed;

    /// <summary>
    /// Creates the engine and loads the PP-OCRv6 Small models. Model files
    /// are resolved relative to the application base directory, so the
    /// engine works no matter which working directory the process was
    /// started from.
    /// </summary>
    /// <exception cref="Exception">The models are missing or corrupted: the
    /// ONNX Runtime fails to load them and the failure surfaces here.</exception>
    public RapidOcrEngine()
        : this(ResolveDefaultModels())
    {
    }

    /// <summary>
    /// Creates the engine with an explicit model set. Exposed to tests so
    /// initialization failures can be exercised without the real models.
    /// </summary>
    internal RapidOcrEngine(RapidOcrModelSet models)
    {
        var engine = new RapidOcr();

        try
        {
            engine.InitModels(models);
        }
        catch
        {
            engine.Dispose();
            throw;
        }

        _ocr = engine;
    }

    public Task<OcrResult> RecognizeAsync(
        ImageData image,
        OcrOptions options,
        CancellationToken cancellationToken)
    {
        if (image is null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        // OcrOptions carries no V0.1 settings; the engine always runs with
        // the PP-OCRv6 preset, so the parameter is accepted and ignored.
        _ = options;

        return RecognizeCoreAsync(image.Bitmap, cancellationToken);
    }

    private async Task<OcrResult> RecognizeCoreAsync(Bitmap bitmap, CancellationToken cancellationToken)
    {
        // Throws ObjectDisposedException if the caller disposed its bitmap.
        using var skBitmap = ToSkiaBitmap(bitmap);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var packageResult = await _ocr
                .DetectAsync(skBitmap, RapidOcrOptions.PPOCRv6, cancellationToken)
                .ConfigureAwait(false);

            return MapResult(packageResult, stopwatch.Elapsed);
        }
        finally
        {
            // Idempotent: covers the success and the failure paths alike.
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Maps a RapidOcrNet result into the public <see cref="OcrResult"/>.
    /// Exposed to tests so the mapping can be validated with synthetic
    /// data without loading the models.
    /// </summary>
    internal static OcrResult MapResult(RapidOcrNet.OcrResult packageResult, TimeSpan processingTime)
    {
        var blocks = new List<OcrBlock>();

        if (packageResult.TextBlocks is not null)
        {
            foreach (var block in packageResult.TextBlocks)
            {
                if (block is null)
                {
                    continue;
                }

                blocks.Add(new OcrBlock(
                    block.Text ?? string.Empty,
                    AverageScore(block.CharScores),
                    BoundsFromBoxPoints(block.BoxPoints)));
            }
        }

        return new OcrResult(packageResult.StrRes ?? string.Empty, blocks, processingTime);
    }

    /// <summary>
    /// The preset's model paths are relative to the process's working
    /// directory. Re-anchor them to the application base directory so a
    /// shortcut or shell that starts the process from elsewhere does not
    /// break model loading.
    /// </summary>
    private static RapidOcrModelSet ResolveDefaultModels()
    {
        var preset = RapidOcrModelSet.PPOCRv6Small;
        var baseDirectory = AppContext.BaseDirectory;

        return preset with
        {
            DetModelPath = ToAbsolutePath(baseDirectory, preset.DetModelPath),
            ClsModelPath = ToAbsolutePath(baseDirectory, preset.ClsModelPath),
            RecModelPath = ToAbsolutePath(baseDirectory, preset.RecModelPath),
            KeysPath = ToAbsolutePath(baseDirectory, preset.KeysPath)
        };
    }

    private static string ToAbsolutePath(string baseDirectory, string path)
        => Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(baseDirectory, path));

    /// <summary>
    /// Averages the per-character scores the engine reported for a line.
    /// Zero when there are no usable scores: inventing a confidence value
    /// would present a made-up number as a measurement.
    /// </summary>
    private static float AverageScore(float[]? scores)
    {
        if (scores is null || scores.Length == 0)
        {
            return 0f;
        }

        var sum = 0f;
        for (var i = 0; i < scores.Length; i++)
        {
            sum += scores[i];
        }

        return sum / scores.Length;
    }

    /// <summary>
    /// Axis-aligned rectangle covering the box corners, in input image
    /// pixels. The corners are inclusive, so the width and height add one.
    /// </summary>
    private static Rectangle BoundsFromBoxPoints(SKPointI[]? boxPoints)
    {
        if (boxPoints is null || boxPoints.Length == 0)
        {
            return Rectangle.Empty;
        }

        var minX = boxPoints[0].X;
        var maxX = boxPoints[0].X;
        var minY = boxPoints[0].Y;
        var maxY = boxPoints[0].Y;

        for (var i = 1; i < boxPoints.Length; i++)
        {
            minX = Math.Min(minX, boxPoints[i].X);
            maxX = Math.Max(maxX, boxPoints[i].X);
            minY = Math.Min(minY, boxPoints[i].Y);
            maxY = Math.Max(maxY, boxPoints[i].Y);
        }

        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>
    /// Copies the bitmap's pixels into a new <see cref="SKBitmap"/> that
    /// RapidOcrNet consumes. The source is only read, never taken
    /// ownership of, and the returned bitmap must be disposed by the
    /// caller. GDI+ 32bpp ARGB and Skia Rgba8888 store their bytes
    /// identically on little-endian machines (B, G, R, A), so the opaque
    /// screen content the capture pipeline produces is copied verbatim.
    /// </summary>
    private static SKBitmap ToSkiaBitmap(Bitmap source)
    {
        Bitmap sourceToCopy = source;
        Bitmap? converted = null;

        try
        {
            if (source.PixelFormat != PixelFormat.Format32bppArgb)
            {
                // The pipeline produces 32bpp ARGB; convert anything else
                // so the row copy below has a single known layout.
                converted = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
                using var graphics = Graphics.FromImage(converted);
                graphics.DrawImage(source, 0, 0, source.Width, source.Height);
                sourceToCopy = converted;
            }

            var info = new SKImageInfo(sourceToCopy.Width, sourceToCopy.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bitmap = new SKBitmap(info);

            var handle = sourceToCopy.LockBits(
                new Rectangle(0, 0, sourceToCopy.Width, sourceToCopy.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                var rowBytes = sourceToCopy.Width * 4;
                var sourceStart = handle.Scan0;

                // Marshal has no IntPtr->IntPtr copy, so the rows stage
                // through a managed buffer before they reach the SKBitmap.
                var pixels = new byte[sourceToCopy.Height * rowBytes];
                for (var y = 0; y < sourceToCopy.Height; y++)
                {
                    System.Runtime.InteropServices.Marshal.Copy(
                        sourceStart + y * handle.Stride,
                        pixels,
                        y * rowBytes,
                        rowBytes);
                }

                System.Runtime.InteropServices.Marshal.Copy(
                    pixels,
                    0,
                    bitmap.GetPixels(),
                    pixels.Length);
            }
            finally
            {
                sourceToCopy.UnlockBits(handle);
            }

            return bitmap;
        }
        finally
        {
            converted?.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ocr.Dispose();
    }
}
