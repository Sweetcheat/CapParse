using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text.Json;

namespace CapParse.Tests;

/// <summary>
/// The M7-C reference collection as declared by
/// <c>TestData/Ocr/manifest.json</c>. The manifest is the single source of
/// truth: it carries each case's authored reference transcription (never
/// copied from OCR output) and every generation parameter, so the committed
/// PNGs can be regenerated and audited from it alone.
/// </summary>
internal sealed class OcrCollectionManifest
{
    public string Font { get; set; } = string.Empty;

    public int Dpi { get; set; }

    public List<OcrCaseSpec> Cases { get; set; } = new();

    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static OcrCollectionManifest Load(string directory)
    {
        var manifest = JsonSerializer
            .Deserialize<OcrCollectionManifest>(
                File.ReadAllText(Path.Combine(directory, "manifest.json")),
                ManifestOptions);

        if (manifest is null)
        {
            throw new InvalidOperationException("The OCR collection manifest is missing or invalid.");
        }

        // Fail loudly on an empty or malformed manifest: an evaluation that
        // silently runs over zero cases would report a perfect, meaningless
        // score.
        if (string.IsNullOrWhiteSpace(manifest.Font) || manifest.Dpi <= 0 || manifest.Cases.Count == 0)
        {
            throw new InvalidOperationException(
                "The OCR collection manifest is incomplete (font, dpi or cases missing).");
        }

        foreach (var c in manifest.Cases)
        {
            if (string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.File) || c.Width <= 0 || c.Height <= 0)
            {
                throw new InvalidOperationException(
                    $"OCR collection case '{c.Id ?? "<null id>"}' is missing id, file or dimensions.");
            }
        }

        return manifest;
    }
}

/// <summary>
/// One evaluation case: the image to feed the engine and its known
/// reference transcription.
/// </summary>
internal sealed class OcrCaseSpec
{
    public string Id { get; set; } = string.Empty;

    public string File { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public string Background { get; set; } = string.Empty;

    /// <summary>
    /// The expected transcription, authored in the manifest. Multi-line
    /// cases join their lines with '\n', matching how the engine joins
    /// recognized lines. No-text cases carry an empty reference and are
    /// assessed separately (no CER is defined for them).
    /// </summary>
    public string Reference { get; set; } = string.Empty;

    public List<OcrShapeSpec> Shapes { get; set; } = new();

    public List<OcrTextSpec> Elements { get; set; } = new();

    public bool IsNoTextCase => Kind == "no-text";

    /// <summary>
    /// Renders the case exactly as the manifest describes it. Rendering is
    /// deterministic within a process; across processes GDI+ anti-aliasing
    /// jitters by a couple of edge pixels, which the generator test
    /// tolerates (see its comparison threshold).
    /// </summary>
    public Bitmap Render(string font, int dpi)
    {
        var image = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(image))
        {
            graphics.Clear(ParseColor(Background));
            // Grayscale anti-aliasing: deterministic and free of the ClearType
            // color fringes a synthetic ARGB canvas would carry. The gap to
            // real screen captures (which show ClearType) is noted in the
            // M7-C report limitations.
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var shape in Shapes)
            {
                using var brush = new SolidBrush(ParseColor(shape.Color));

                switch (shape.Type)
                {
                    case "rectangle":
                        graphics.FillRectangle(brush, shape.X, shape.Y, shape.Width, shape.Height);
                        break;

                    case "line":
                        // A thin horizontal band, standing in for a UI separator.
                        graphics.FillRectangle(brush, shape.X, shape.Y, shape.Width, Math.Max(1, shape.Height));
                        break;

                    case "circle":
                        graphics.FillEllipse(brush, shape.X, shape.Y, shape.Width, shape.Height);
                        break;

                    default:
                        throw new InvalidOperationException($"Unknown shape type: {shape.Type}");
                }
            }

            foreach (var element in Elements)
            {
                // The manifest speaks in points, like real UI text. .NET's
                // System.Drawing has no settable Graphics.DpiX, so the
                // pt->px conversion is done explicitly: at 96 DPI one point
                // is 4/3 px, exactly a 100%-scaled screen.
                float sizePixels = element.SizePt * dpi / 72f;
                using var fontInstance = new Font(
                    font,
                    sizePixels,
                    element.Bold ? FontStyle.Bold : FontStyle.Regular,
                    GraphicsUnit.Pixel);

                using var brush = new SolidBrush(ParseColor(element.Color));
                graphics.DrawString(element.Text, fontInstance, brush, element.X, element.Y);
            }
        }

        return image;
    }

    private static Color ParseColor(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return ColorTranslator.FromHtml(value);
    }
}

internal sealed class OcrShapeSpec
{
    public string Type { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }
}

internal sealed class OcrTextSpec
{
    public string Text { get; set; } = string.Empty;

    public float SizePt { get; set; }

    public bool Bold { get; set; }

    public string Color { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }
}
