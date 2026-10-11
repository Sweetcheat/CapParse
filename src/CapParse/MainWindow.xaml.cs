using System.Windows;
using System.Windows.Controls;
using CapParse.OCR;
using CapParse.UI;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace CapParse;

/// <summary>
/// Interaction logic for MainWindow.xaml. The window is a view of the
/// application's <see cref="OcrRunState"/> (the current result) and of its
/// <see cref="OcrHistory"/> (previous results): it refreshes itself whenever
/// the app publishes a state change (always on the UI thread) and offers the
/// explicit copy actions. It owns no capture or OCR logic of its own.
///
/// The current result and the history are presented independently: selecting
/// a history entry only updates the history view and never changes
/// <see cref="App.LastOcrResult"/>, <see cref="App.OcrState"/> or the run
/// state.
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
        Refresh();
    }

    private void OnOcrStateChanged(object? sender, EventArgs e)
        => Refresh();

    private void Refresh()
    {
        RefreshOcrState();
        RefreshHistory();
    }

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

    /// <summary>
    /// Rebuilds the history list from the app's <see cref="OcrHistory"/>.
    /// Called on the UI thread whenever the app publishes a state change,
    /// which is also when a new entry is recorded.
    /// </summary>
    private void RefreshHistory()
    {
        var history = ((App)Application.Current).History;

        // Resetting the source drops any stale selection after a refresh.
        HistoryList.ItemsSource = null;
        HistoryList.ItemsSource = history.Entries;
        HistoryList.IsEnabled = !history.IsEmpty;

        HistoryCopyFeedback.Text = string.Empty;
        UpdateHistorySelection();
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateHistorySelection();

    /// <summary>
    /// Shows the selected history entry's full text (or the empty/placeholder
    /// message) and enables the copy action only when an entry is selected.
    /// This only updates the history view; it never touches the current
    /// result or the run state.
    /// </summary>
    private void UpdateHistorySelection()
    {
        var history = ((App)Application.Current).History;

        if (history.IsEmpty)
        {
            HistoryEmpty.Text = "Nenhum resultado anterior nesta sessão.";
            HistoryEmpty.Visibility = Visibility.Visible;
            HistoryText.Visibility = Visibility.Collapsed;
            HistoryCopyButton.IsEnabled = false;
            return;
        }

        if (HistoryList.SelectedItem is OcrHistoryEntry entry)
        {
            HistoryText.Text = entry.Text;
            HistoryText.Visibility = Visibility.Visible;
            HistoryEmpty.Visibility = Visibility.Collapsed;
            HistoryCopyButton.IsEnabled = true;
        }
        else
        {
            HistoryEmpty.Text = "Selecione um resultado para ver o texto completo.";
            HistoryEmpty.Visibility = Visibility.Visible;
            HistoryText.Visibility = Visibility.Collapsed;
            HistoryCopyButton.IsEnabled = false;
        }
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

    private void HistoryCopy_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryList.SelectedItem is not OcrHistoryEntry entry)
        {
            return;
        }

        var outcome = _copier.TryCopyText(entry.Text);

        if (outcome.Succeeded)
        {
            HistoryCopyFeedback.Text = "Texto copiado!";
            return;
        }

        MessageBox.Show(this, outcome.Message, "CapParse",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
