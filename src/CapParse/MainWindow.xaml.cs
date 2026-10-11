using System.Windows;
using CapParse.OCR;
using CapParse.UI;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace CapParse;

/// <summary>
/// Interaction logic for MainWindow.xaml. The window is a view of the
/// application's <see cref="OcrRunState"/>: it refreshes itself
/// whenever the app publishes a state change (always on the UI thread)
/// and offers the explicit "copy text" action. It owns no capture or OCR
/// logic of its own.
/// </summary>
public partial class MainWindow : Window
{
    private readonly OcrTextCopier _copier;

    public MainWindow()
        : this(new WpfTextClipboard())
    {
    }

    /// <summary>
    /// Creates the window with an explicit clipboard, so tests can
    /// substitute a fake.
    /// </summary>
    public MainWindow(ITextClipboard clipboard)
    {
        _copier = new OcrTextCopier(clipboard);
        InitializeComponent();

        var app = (App)Application.Current;
        app.OcrStateChanged += OnOcrStateChanged;
        RefreshOcrState();
    }

    private void OnOcrStateChanged(object? sender, EventArgs e)
        => RefreshOcrState();

    private void RefreshOcrState()
    {
        var state = ((App)Application.Current).OcrState;

        // A new run clears any "Copied" feedback from the previous one.
        CopyFeedback.Text = string.Empty;

        switch (state.Phase)
        {
            case OcrPhase.Idle:
                ShowStatus("Ainda não há resultado. Capture uma região para reconhecer o texto.");
                break;

            case OcrPhase.Processing:
                ShowStatus("Reconhecendo a região selecionada…");
                break;

            // An image without text is a valid, empty result, not an error.
            case OcrPhase.NoText:
                ShowStatus("Nenhum texto foi encontrado nesta região.");
                break;

            case OcrPhase.Failed:
                ShowStatus("Não foi possível reconhecer o texto na região capturada.");
                break;

            case OcrPhase.Succeeded:
                StatusText.Visibility = Visibility.Collapsed;
                ResultText.Text = state.Text;
                ResultPanel.Visibility = Visibility.Visible;
                CopyButton.IsEnabled = true;
                break;
        }
    }

    private void ShowStatus(string message)
    {
        StatusText.Text = message;
        StatusText.Visibility = Visibility.Visible;
        ResultPanel.Visibility = Visibility.Collapsed;
        CopyButton.IsEnabled = false;
    }

    private void CopyText_Click(object sender, RoutedEventArgs e)
    {
        var outcome = _copier.TryCopy(((App)Application.Current).OcrState);

        if (outcome.Succeeded)
        {
            CopyFeedback.Text = "Texto copiado!";
            return;
        }

        MessageBox.Show(this, outcome.Message, "CapParse",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
