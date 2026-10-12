using System.IO;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace CapParse.UI;

/// <summary>
/// The standard Windows "save file" dialog for the captured image. The
/// implementation must be shown on the WPF UI thread, where modal dialogs
/// run. Returning null means the user did not choose a file (cancel or
/// close), which is a normal outcome, not an error.
/// </summary>
public interface ICropImageSaveDialog
{
    /// <summary>
    /// Shows the save dialog, pre-filled with
    /// <paramref name="suggestedFileName"/>.
    /// </summary>
    /// <returns>The chosen file path when the user confirmed, or null when
    /// the user canceled or closed the dialog.</returns>
    string? ShowDialog(string suggestedFileName);
}

/// <summary>
/// <see cref="ICropImageSaveDialog"/> backed by the WPF
/// <see cref="Microsoft.Win32.SaveFileDialog"/>. The dialog offers only the
/// PNG format, the only format this application writes; the initial
/// directory is the user's Pictures folder when it exists.
/// </summary>
public sealed class WpfCropImageSaveDialog : ICropImageSaveDialog
{
    public string? ShowDialog(string suggestedFileName)
    {
        if (suggestedFileName is null)
        {
            throw new ArgumentNullException(nameof(suggestedFileName));
        }

        var dialog = new SaveFileDialog
        {
            Title = "Salvar imagem",
            Filter = "Imagem PNG (*.png)|*.png",
            DefaultExt = ".png",
            AddExtension = true,
            FileName = suggestedFileName
        };

        // A predictable default location for saved images; the dialog still
        // lets the user navigate anywhere. Skipped when the folder does not
        // exist (for example on a headless machine).
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (Directory.Exists(pictures))
        {
            dialog.InitialDirectory = pictures;
        }

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
