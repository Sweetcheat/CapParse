using System.Drawing;

namespace CapParse.OCR;

/// <summary>
/// One recognized text line, with its bounding box in the input image's
/// pixel coordinates.
/// </summary>
public sealed class OcrBlock
{
    public OcrBlock(string text, float confidence, Rectangle bounds)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Confidence = confidence;
        Bounds = bounds;
    }

    /// <summary>
    /// The text of the line, in reading order as produced by the engine.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Average per-character confidence of the line, in [0, 1]. Zero when
    /// the engine exposed no usable scores for the line.
    /// </summary>
    public float Confidence { get; }

    /// <summary>
    /// Axis-aligned bounding rectangle of the line's box, in input image
    /// pixels. An empty rectangle at the origin when the engine exposed no
    /// box points for the line.
    /// </summary>
    public Rectangle Bounds { get; }
}
