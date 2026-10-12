using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace CapParse.UI;

/// <summary>
/// The result of an attempt to save the captured image to a file.
/// </summary>
public enum SaveImageStatus
{
    /// <summary>The image was written to the file the user chose.</summary>
    Saved,

    /// <summary>The user canceled or closed the save dialog. A normal
    /// outcome, not an error.</summary>
    Canceled,

    /// <summary>The image could not be saved.
    /// <see cref="SaveImageOutcome.Message"/> carries the user-friendly
    /// explanation.</summary>
    Failed
}

/// <summary>
/// The outcome of an attempt to save the captured image.
/// </summary>
public sealed record SaveImageOutcome(SaveImageStatus Status, string Message);

/// <summary>
/// Saves a captured crop to a file chosen by the user through the standard
/// Windows save dialog. Saving is always an explicit user action: the
/// application never writes files on its own. The image is encoded as PNG
/// (lossless, the only format offered) and tagged with the effective DPI of
/// the monitor the selection came from, the same convention the clipboard
/// copy uses.
///
/// The crop bitmap is never disposed by this class: ownership stays with the
/// application, which keeps the crop alive until the next capture. A null
/// crop (capture cancelled or replaced) and a crop already disposed by a
/// newer capture both fail gracefully. Canceling the dialog is a normal
/// outcome; write failures (invalid path, access denied, file in use) are
/// reported as a failed outcome with a friendly message, never thrown.
/// </summary>
public sealed class CropImageSaver
{
    private const string NoImageMessage = "Não há imagem capturada para salvar.";
    private const string SaveFailureMessage =
        "Não foi possível salvar a imagem. Verifique o local e o nome do arquivo e tente novamente.";

    private const string PngExtension = ".png";

    private readonly ICropImageSaveDialog _dialog;

    public CropImageSaver(ICropImageSaveDialog dialog)
    {
        _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
    }

    /// <summary>
    /// Saves <paramref name="crop"/> to the file the user chooses, tagged
    /// with <paramref name="dpi"/>. See the type summary for the outcome
    /// semantics: a missing or disposed crop fails, a canceled dialog is a
    /// normal outcome, and a write failure is reported, never thrown.
    /// </summary>
    public SaveImageOutcome TrySave(Bitmap? crop, int dpi)
    {
        if (crop is null)
        {
            return new SaveImageOutcome(SaveImageStatus.Failed, NoImageMessage);
        }

        BitmapSource image;
        try
        {
            image = CropImageCopier.ToBitmapSource(crop, dpi);
        }
        catch
        {
            // Reading a disposed System.Drawing bitmap throws from its
            // property accessors: the crop may have been replaced by a
            // newer capture before the interface refreshed.
            return new SaveImageOutcome(SaveImageStatus.Failed, NoImageMessage);
        }

        string? chosen;
        try
        {
            chosen = _dialog.ShowDialog(SuggestedFileName(DateTime.Now));
        }
        catch
        {
            return new SaveImageOutcome(SaveImageStatus.Failed, SaveFailureMessage);
        }

        if (string.IsNullOrWhiteSpace(chosen))
        {
            // Canceling (or closing) the dialog is a normal operation, not
            // an error: no file is written and nothing is reported.
            return new SaveImageOutcome(SaveImageStatus.Canceled, string.Empty);
        }

        try
        {
            WritePng(image, NormalizePngPath(chosen));
        }
        catch
        {
            return new SaveImageOutcome(SaveImageStatus.Failed, SaveFailureMessage);
        }

        return new SaveImageOutcome(SaveImageStatus.Saved, string.Empty);
    }

    /// <summary>
    /// Writes <paramref name="image"/> as a PNG file to
    /// <paramref name="path"/>. The frozen <see cref="BitmapSource"/> and
    /// the encoder need no disposal; the file stream is closed and released
    /// before this method returns. No temporary file is created.
    /// </summary>
    private static void WritePng(BitmapSource image, string path)
    {
        var encoder = new PngBitmapEncoder();

        // BitmapFrame.Create carries the source's DpiX/DpiY into the frame,
        // so the PNG (its pHYs chunk) keeps the same DPI tag the M11
        // clipboard DIB carries.
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    /// <summary>
    /// Suggests a unique file name for a new capture, so repeated saves do
    /// not silently overwrite each other: CapParse-yyyy-MM-dd-HHmmss.png.
    /// </summary>
    internal static string SuggestedFileName(DateTime now)
        => $"CapParse-{now:yyyyMMdd-HHmmss}{PngExtension}";

    /// <summary>
    /// The file is always written as PNG, so the name is normalized to end
    /// in ".png": a missing extension is appended, and a different one is
    /// replaced, so the extension can never contradict the content.
    /// </summary>
    internal static string NormalizePngPath(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var extension = Path.GetExtension(path);
        if (string.IsNullOrEmpty(extension))
        {
            return path + PngExtension;
        }

        if (!extension.Equals(PngExtension, StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(
                Path.GetDirectoryName(path) ?? string.Empty,
                Path.GetFileNameWithoutExtension(path) + PngExtension);
        }

        return path;
    }
}
