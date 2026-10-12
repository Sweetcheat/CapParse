using System.Windows;
using System.Windows.Input;
using CapParse.Configuration;
using CapParse.Platform;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace CapParse;

/// <summary>
/// The V0.1 settings window: a single setting — the capture hotkey.
///
/// The window shows the combination that is active right now and offers
/// three actions: record a new combination (a valid press switches to a
/// confirmation state, Esc cancels the change), reset to the default, and
/// close. A new combination is applied through
/// <see cref="App.ApplyCaptureHotkey"/>, which recovers to a working
/// combination when the requested one is taken by another application,
/// and is persisted only when it actually became active.
/// </summary>
public partial class SettingsWindow : Window
{
    // The combination recorded in the current change attempt, if any.
    private HotkeySetting? _candidate;

    public SettingsWindow()
    {
        InitializeComponent();
        ShowCurrent();
    }

    private App App => (App)Application.Current;

    private void ChangeButton_Click(object sender, RoutedEventArgs e)
        => ShowRecording();

    private void ResetButton_Click(object sender, RoutedEventArgs e)
        => ApplyCandidate(HotkeySetting.Default);

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_candidate is not null)
        {
            ApplyCandidate(_candidate);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _candidate = null;
        ShowCurrent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    /// <summary>
    /// Shows the current state (the active hotkey, or the saved one with a
    /// note when nothing is registered) and the change/reset actions.
    /// </summary>
    private void ShowCurrent()
    {
        var app = App;
        var active = app.ActiveHotkey;
        var saved = app.SavedCaptureHotkey;

        CurrentHotkeyText.Text = (active ?? saved).DisplayString;

        if (active is null)
        {
            CurrentNote.Text =
                "O atalho global não pôde ser registrado. " +
                "A captura continua disponível pelo menu da bandeja.";
            CurrentNote.Visibility = Visibility.Visible;
        }
        else if (!active.Equals(saved))
        {
            CurrentNote.Text =
                $"O atalho salvo ({saved.DisplayString}) não pôde ser registrado; " +
                $"o atalho ativo é {active.DisplayString}.";
            CurrentNote.Visibility = Visibility.Visible;
        }
        else
        {
            CurrentNote.Visibility = Visibility.Collapsed;
        }

        CurrentPanel.Visibility = Visibility.Visible;
        RecordingPanel.Visibility = Visibility.Collapsed;
        ConfirmPanel.Visibility = Visibility.Collapsed;
        CurrentButtons.Visibility = Visibility.Visible;
        ConfirmButtons.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Starts recording a new combination: the dashed border receives the
    /// keyboard focus and the next valid key press is recorded.
    /// </summary>
    private void ShowRecording()
    {
        RecordingError.Text = string.Empty;
        RecordingError.Visibility = Visibility.Collapsed;

        CurrentPanel.Visibility = Visibility.Collapsed;
        RecordingPanel.Visibility = Visibility.Visible;
        ConfirmPanel.Visibility = Visibility.Collapsed;
        CurrentButtons.Visibility = Visibility.Collapsed;
        ConfirmButtons.Visibility = Visibility.Collapsed;

        KeyCaptureTarget.Focus();
    }

    /// <summary>Shows the recorded combination and the save/cancel actions.</summary>
    private void ShowConfirm()
    {
        CandidateText.Text = _candidate!.DisplayString;

        CurrentPanel.Visibility = Visibility.Collapsed;
        RecordingPanel.Visibility = Visibility.Collapsed;
        ConfirmPanel.Visibility = Visibility.Visible;
        CurrentButtons.Visibility = Visibility.Collapsed;
        ConfirmButtons.Visibility = Visibility.Visible;

        // Drop the keyboard focus so key auto-repeat cannot re-enter the
        // recording handler.
        Keyboard.ClearFocus();
    }

    /// <summary>
    /// Handles a key press while recording a new combination. Modifier keys
    /// on their own only change the pending combination (the handler keeps
    /// listening for the actual key); a real key completes the attempt.
    /// </summary>
    private void KeyCaptureTarget_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        // When Alt is part of the combination, WPF reports the key through
        // SystemKey instead of Key.
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.None)
        {
            return;
        }

        if (IsModifierKey(key))
        {
            return;
        }

        if (key == Key.Escape)
        {
            // Esc without other modifiers cancels the change; with
            // modifiers it is an invalid combination (Esc is not a
            // supported hotkey key).
            if (CurrentModifiers() == (uint)GlobalHotkey.Modifier.None)
            {
                ShowCurrent();
                return;
            }

            ShowRecordingError();
            return;
        }

        var candidate = new HotkeySetting(CurrentModifiers(), (uint)KeyInterop.VirtualKeyFromKey(key));
        if (!candidate.IsValid)
        {
            ShowRecordingError();
            return;
        }

        _candidate = candidate;
        ShowConfirm();
    }

    private void ShowRecordingError()
    {
        RecordingError.Text =
            "Combinação inválida. Use Ctrl, Alt ou Windows com uma letra, um número ou uma tecla F.";
        RecordingError.Visibility = Visibility.Visible;

        // Keep the focus on the capture target so the user can retry.
        KeyCaptureTarget.Focus();
    }

    private static bool IsModifierKey(Key key)
        => key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt
            or Key.LWin or Key.RWin;

    private static uint CurrentModifiers()
    {
        var modifiers = GlobalHotkey.Modifier.None;
        var state = Keyboard.Modifiers;

        if (state.HasFlag(ModifierKeys.Control))
        {
            modifiers |= GlobalHotkey.Modifier.Control;
        }

        if (state.HasFlag(ModifierKeys.Shift))
        {
            modifiers |= GlobalHotkey.Modifier.Shift;
        }

        if (state.HasFlag(ModifierKeys.Alt))
        {
            modifiers |= GlobalHotkey.Modifier.Alt;
        }

        if (state.HasFlag(ModifierKeys.Windows))
        {
            modifiers |= GlobalHotkey.Modifier.Win;
        }

        return (uint)modifiers;
    }

    /// <summary>
    /// Applies a new hotkey through the application (which recovers to a
    /// working combination when the requested one is unavailable) and
    /// persists the settings only when the requested combination became
    /// active.
    /// </summary>
    private void ApplyCandidate(HotkeySetting requested)
    {
        var app = App;
        var active = app.ApplyCaptureHotkey(requested);
        _candidate = null;

        if (active is null)
        {
            ShowCurrent();
            MessageBox.Show(this,
                "Não foi possível registrar nenhum atalho global.\n" +
                "Outros aplicativos podem estar usando as combinações disponíveis.\n\n" +
                "A captura continua disponível pelo menu da bandeja.",
                "CapParse",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!active.Equals(requested))
        {
            // The requested combination is taken by another application:
            // the application recovered to a working one and the settings
            // are intentionally left unchanged.
            ShowCurrent();
            MessageBox.Show(this,
                $"O atalho {requested.DisplayString} não pôde ser registrado.\n" +
                "Outro aplicativo pode já estar usando esse atalho.\n\n" +
                $"O atalho ativo é {active.DisplayString}.",
                "CapParse",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!app.SaveSettings())
        {
            // The hotkey is active, but the file could not be written:
            // report it without rolling the change back.
            ShowCurrent();
            MessageBox.Show(this,
                "O atalho foi aplicado, mas as configurações não puderam ser salvas.\n" +
                "Ele voltará ao valor anterior na próxima inicialização.",
                "CapParse",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        ShowCurrent();
    }
}
