namespace CapParse.OCR;

/// <summary>
/// The outcome of recognizing one image. An image without any recognized
/// text is a valid, empty result (empty <see cref="Text"/>, empty
/// <see cref="Blocks"/>), not an error.
/// </summary>
public sealed class OcrResult
{
    public OcrResult(string text, IReadOnlyList<OcrBlock> blocks, TimeSpan processingTime)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        ProcessingTime = processingTime;
    }

    /// <summary>
    /// All recognized text: the blocks' texts joined with newlines, in the
    /// order the engine reported the lines.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// One block per recognized line, with text, confidence and bounds.
    /// </summary>
    public IReadOnlyList<OcrBlock> Blocks { get; }

    /// <summary>
    /// How long the recognition of this image took.
    /// </summary>
    public TimeSpan ProcessingTime { get; }
}
