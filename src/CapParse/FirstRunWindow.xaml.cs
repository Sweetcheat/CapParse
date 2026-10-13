using System.Windows;

namespace CapParse;

/// <summary>
/// The short, non-mandatory first-run presentation (ui.md §23): what the
/// application does, how to start a capture (the active hotkey and the
/// tray menu) and what happens after a selection. It is shown only on the
/// first run (the app records that), is skippable (the close button
/// dismisses it) and needs no configuration step: a single "Começar"
/// button closes it.
/// </summary>
public partial class FirstRunWindow : Window
{
    /// <param name="howToCapture">
    /// The line that tells the user how to start a capture, already
    /// adapted to the hotkey that is active at startup.
    /// </param>
    public FirstRunWindow(string howToCapture)
    {
        InitializeComponent();
        HowToCaptureText.Text = howToCapture;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
        => Close();
}
