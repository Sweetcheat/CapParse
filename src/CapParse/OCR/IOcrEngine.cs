namespace CapParse.OCR;

/// <summary>
/// Abstraction over the OCR mechanism, so the capture flow and the actions
/// never depend on a specific engine. The engine runs entirely locally and
/// never touches the UI, the clipboard, or the file system.
/// </summary>
public interface IOcrEngine
{
    /// <summary>
    /// Recognizes the text in <paramref name="image"/>. The engine only
    /// reads the image: the caller keeps ownership of its bitmap.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="image"/> holds a
    /// bitmap that was disposed.</exception>
    /// <exception cref="ObjectDisposedException">The engine was disposed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    Task<OcrResult> RecognizeAsync(
        ImageData image,
        OcrOptions options,
        CancellationToken cancellationToken);
}
