using System.Windows;
using Clipboard = System.Windows.Clipboard;

namespace CapParse.UI;

/// <summary>
/// Writes text to the system clipboard. The implementation must be called
/// on the WPF UI thread, where the clipboard APIs require it.
/// </summary>
public interface ITextClipboard
{
    /// <exception cref="System.Runtime.InteropServices.ExternalException">
    /// The clipboard is unavailable or locked by another application.</exception>
    void SetText(string text);
}

/// <summary>
/// <see cref="ITextClipboard"/> backed by the WPF clipboard.
/// </summary>
public sealed class WpfTextClipboard : ITextClipboard
{
    public void SetText(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        Clipboard.SetText(text);
    }
}
