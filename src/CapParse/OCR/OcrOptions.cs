namespace CapParse.OCR;

/// <summary>
/// OCR processing options. V0.1 exposes no configurable options: the engine
/// always runs its fixed PP-OCRv6 Small preset. The type exists so the
/// documented <see cref="IOcrEngine"/> signature can gain options later
/// without a breaking change.
/// </summary>
public sealed class OcrOptions
{
    public static OcrOptions Default { get; } = new();
}
