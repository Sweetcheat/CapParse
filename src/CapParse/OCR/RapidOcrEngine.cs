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
    /// <summary>
    /// The PP-OCRv6 preset with word boxes enabled. The recognition model
    /// concatenates per-character results and frequently drops the spaces
    /// between words; the word boxes let the mapping add back the spaces
    /// the geometry shows are missing (see <see cref="ReconstructLineText"/>).
    /// Only this one option differs from the preset: no detection/recognition
    /// parameter is tuned.
    /// </summary>
    private static readonly RapidOcrOptions RecognizeOptions =
        RapidOcrOptions.PPOCRv6 with { ReturnWordBox = true };

    /// <summary>
    /// A horizontal gap between two word boxes wider than this fraction of
    /// the shorter word's height is read as a missing space. Measured on the
    /// M7-C collection (docs/ocr-integration.md), this engine tiles word and
    /// character boxes across the line: gaps at real space positions stay
    /// below ~0.47 of the line height, so a half-height threshold is the
    /// smallest value that never fires on tiling noise and only fires on
    /// decisive geometric holes (a space the model dropped that the boxes
    /// did not tile over).
    /// </summary>
    private const double SpaceGapFactor = 0.5;

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
        // the PP-OCRv6 preset (plus word boxes), so the parameter is
        // accepted and ignored.
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
                .DetectAsync(skBitmap, RecognizeOptions, cancellationToken)
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
    /// Each block's text is rebuilt from its word boxes when they are
    /// available and usable (<see cref="ReconstructLineText"/>), and the
    /// full text is the block texts joined with newlines, so <c>Text</c>
    /// and <c>Blocks</c> always agree. Exposed to tests so the mapping can
    /// be validated with synthetic data without loading the models.
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
                    ReconstructLineText(block),
                    AverageScore(block.CharScores),
                    BoundsFromBoxPoints(block.BoxPoints)));
            }
        }

        var text = string.Join("\n", blocks.Select(b => b.Text));
        return new OcrResult(text, blocks, processingTime);
    }

    /// <summary>
    /// Rebuilds the text of one detected line. The model's own line text is
    /// the base: every space the recognition model reported is preserved
    /// exactly. On top of that, a single space is inserted at a boundary
    /// between two word boxes when the model reported no space there and
    /// the horizontal gap is decisive (<see cref="SpaceGapFactor"/> of the
    /// shorter word's height) — that recovers spaces the model dropped but
    /// the geometry still shows. Words are ordered left to right (top to
    /// bottom as a tie-break) regardless of the order the engine reported
    /// them in. Spaces at the edges of the line are dropped.
    ///
    /// Fallbacks (the line's own text is returned unchanged, never empty
    /// when the model produced text): no word boxes reported; any word
    /// lacks usable geometry or text; the words' texts, concatenated, do
    /// not cover the line text exactly (the geometry is not trusted to
    /// align with the text in that case).
    /// </summary>
    internal static string ReconstructLineText(RapidOcrNet.TextBlock block)
    {
        var fallback = block.Text ?? string.Empty;
        if (fallback.Length == 0)
        {
            return fallback;
        }

        var words = block.WordResults;
        if (words is null || words.Length == 0)
        {
            return fallback;
        }

        var boxes = new Rectangle[words.Length];
        for (var i = 0; i < words.Length; i++)
        {
            var box = WordBoxRectangle(words[i]);
            if (words[i].Text is null || words[i].Text.Length == 0 || box is null)
            {
                return fallback;
            }

            boxes[i] = box.Value;
        }

        var ordered = new List<(WordBox Word, Rectangle Box)>(words.Length);
        for (var i = 0; i < words.Length; i++)
        {
            ordered.Add((words[i], boxes[i]));
        }

        ordered.Sort((a, b) =>
            a.Box.Left != b.Box.Left
                ? a.Box.Left.CompareTo(b.Box.Left)
                : a.Box.Top.CompareTo(b.Box.Top));

        // The words must account for every non-space character of the line
        // text; otherwise the boxes cannot be aligned to it and any space
        // decision would be a guess.
        var covered = new System.Text.StringBuilder();
        foreach (var (word, _) in ordered)
        {
            covered.Append(word.Text);
        }

        if (covered.ToString() != StripSpaces(fallback))
        {
            return fallback;
        }

        // spanStart[i] is where word i's text starts inside the line text
        // with its spaces removed.
        var spanStart = new int[ordered.Count];
        var offset = 0;
        for (var i = 0; i < ordered.Count; i++)
        {
            spanStart[i] = offset;
            offset += ordered[i].Word.Text.Length;
        }

        var text = new System.Text.StringBuilder(fallback.Length);
        var coveredPosition = -1; // last non-space char consumed, in 'covered'
        var pendingSpaces = 0;

        foreach (var c in fallback)
        {
            if (c == ' ')
            {
                pendingSpaces++;
                continue;
            }

            if (pendingSpaces > 0 && coveredPosition >= 0)
            {
                // Model spaces pass through exactly, at word boundaries and
                // inside word spans. A run before the first character is an
                // edge artifact and is dropped (coveredPosition is -1).
                text.Append(new string(' ', pendingSpaces));
            }
            else if (coveredPosition >= 0)
            {
                // The model reported no space here: at a boundary between
                // two word boxes the geometry decides.
                var nextPosition = coveredPosition + 1;
                var nextIndex = IndexOfWordAt(ordered, spanStart, nextPosition);
                if (nextIndex > 0 && nextPosition == spanStart[nextIndex])
                {
                    text.Append(GapSpaceBetween(ordered, nextIndex));
                }
            }

            text.Append(c);
            coveredPosition++;
            pendingSpaces = 0;
        }

        // Trailing spaces of the line are engine artifacts, not content.
        return text.Length > 0 ? text.ToString() : fallback;
    }

    /// <summary>
    /// A single space when the gap between the two word boxes around
    /// boundary <paramref name="nextIndex"/> is decisive
    /// (<see cref="SpaceGapFactor"/> of the shorter word's height); empty
    /// otherwise. Only horizontal distance and box heights are compared, so
    /// a vertical offset between the boxes (a slightly slanted line) never
    /// by itself creates a space, and overlapping boxes (a negative gap)
    /// never do either.
    /// </summary>
    private static string GapSpaceBetween(List<(WordBox Word, Rectangle Box)> ordered, int nextIndex)
    {
        var previous = ordered[nextIndex - 1].Box;
        var current = ordered[nextIndex].Box;
        var gap = (double)current.Left - (previous.Left + previous.Width);
        var reference = Math.Max(1, Math.Min(previous.Height, current.Height));
        return gap > reference * SpaceGapFactor ? " " : string.Empty;
    }

    private static int IndexOfWordAt(
        List<(WordBox Word, Rectangle Box)> ordered,
        int[] spanStart,
        int position)
    {
        for (var i = ordered.Count - 1; i >= 0; i--)
        {
            if (position >= spanStart[i])
            {
                return i;
            }
        }

        return 0;
    }

    private static string StripSpaces(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c != ' ')
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Axis-aligned rectangle of a word box's corners, in input image
    /// pixels. Null when the box has no usable geometry (missing points or
    /// zero extent), which marks the word as unusable for reconstruction.
    /// </summary>
    private static Rectangle? WordBoxRectangle(RapidOcrNet.WordBox word)
    {
        var points = word.BoxPoints;
        if (points is null || points.Length < 4)
        {
            return null;
        }

        var minX = points[0].X;
        var maxX = points[0].X;
        var minY = points[0].Y;
        var maxY = points[0].Y;

        for (var i = 1; i < points.Length; i++)
        {
            minX = Math.Min(minX, points[i].X);
            maxX = Math.Max(maxX, points[i].X);
            minY = Math.Min(minY, points[i].Y);
            maxY = Math.Max(maxY, points[i].Y);
        }

        if (maxX <= minX || maxY <= minY)
        {
            return null;
        }

        return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
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
