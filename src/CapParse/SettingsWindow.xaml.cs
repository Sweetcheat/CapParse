using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CapParse.Configuration;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace CapParse;

/// <summary>
/// The settings window: the capture hotkey, the theme preference and,
/// since the direct mode, the three direct hotkey slots, the direct
/// mode's enabled flag, the save policy and the automatic save folder.
///
/// The window is transactional: every control edits only a pending copy
/// of the settings (a clone of the saved ones). Nothing is applied or
/// persisted while the window is open; "Salvar" commits the whole pending
/// change through <see cref="App.ApplySettings"/>, which validates it
/// (hotkey conflicts, the automatic policy's folder), applies it
/// (registrations, theme) and persists it as a unit — a failure restores
/// the previous state and persists nothing. "Cancelar", like closing the
/// window, discards the pending change and keeps the settings and the
/// registrations that were active before.
///
/// The hotkey slots are recorded with the same interaction: a focusable
/// dashed target captures the next valid key press (Esc cancels). One
/// shared recording target serves the capture slot, another serves the
/// direct slots; the slot that started the recording decides where the
/// recorded combination is stored.
/// </summary>
public partial class SettingsWindow : Window
{
    // The pending change: a clone of the saved settings that the controls
    // edit. It is committed to the application only on "Salvar"; closing
    // or canceling the window drops it, so no control can change the
    // active settings or registrations on its own.
    private AppSettings _pending;

    // The slot whose combination is being recorded right now
    // (HotkeySlots.Capture or one of the direct slots); it decides which
    // of the two recording targets is shown and where the recorded
    // combination is stored.
    private int _recordingSlot = HotkeySlots.Capture;

    // Set while the constructor positions the controls on the saved
    // settings, so that initialisation does not count as a user change.
    private bool _initializing;

    public SettingsWindow()
    {
        InitializeComponent();
        // The running application always owns the settings; when the
        // window is constructed without it (the test host), the same
        // settings file the App constructor loads is the source of truth.
        _pending = App?.SavedSettingsSnapshot() ?? AppSettings.Load();

        // The window sizes itself to its content (SizeToContent="Height"),
        // which keeps every pane visible. The ScrollViewer caps that
        // growth at the available desktop area: when the content is taller
        // than the screen, the window stops growing and scrolls instead of
        // extending past the work area (the first-run window's pattern).
        ContentScroller.MaxHeight =
            FirstRunWindow.ContentMaxHeight(SystemParameters.WorkArea.Height);

        _initializing = true;
        ShowState();
        _initializing = false;
    }

    // Null when the window is constructed without the running application
    // (the test host creates a bare Application); production always has it.
    private App? App => Application.Current as App;

    // --------------------------------------------------------------------
    // Capture hotkey
    // --------------------------------------------------------------------

    private void ChangeButton_Click(object sender, RoutedEventArgs e)
        => StartRecording(HotkeySlots.Capture);

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _pending.CaptureHotkey = HotkeySetting.Default;
        ShowState();
    }

    // --------------------------------------------------------------------
    // Direct mode
    // --------------------------------------------------------------------

    private void DirectModeToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        // Toggling the mode only flips the flag: the configured
        // combinations are kept (the mode controls the registrations,
        // never the stored combinations), and everything stays pending
        // until the user saves.
        _pending.DirectModeEnabled = DirectModeToggle.IsChecked == true;

        // The traditional capture hotkey follows the pending mode
        // immediately: a pending direct mode suspends it for the duration
        // of the mapping (pressing its combination then records the slot
        // instead of starting a capture), a pending traditional mode
        // restores it when the committed mode is traditional. Saving
        // commits the state; canceling the window reverts it, as the
        // committed mode prescribes.
        App?.SettingsWindowPendingDirectModeChanged(_pending.DirectModeEnabled);

        ShowState();
    }

    private void SlotChangeButton_Click(object sender, RoutedEventArgs e)
    {
        if (SlotOf(sender) is { } slot)
        {
            StartRecording(slot);
        }
    }

    private void SlotClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (SlotOf(sender) is { } slot)
        {
            _pending.SetDirectSlot(slot, null);
            ShowState();
        }
    }

    private void DirectPolicySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        _pending.DirectSavePolicy = (ImageSavePolicy)DirectPolicySelector.SelectedIndex;

        // The folder row is only relevant (and required) with the
        // automatic policy.
        ShowState();
    }

    private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Escolha a pasta de salvamento automático"
        };

        if (!string.IsNullOrWhiteSpace(_pending.DirectSaveFolder))
        {
            dialog.InitialDirectory = _pending.DirectSaveFolder;
        }

        if (dialog.ShowDialog(this) == true)
        {
            _pending.DirectSaveFolder = dialog.FolderName;
            ShowState();
        }
    }

    // --------------------------------------------------------------------
    // Theme
    // --------------------------------------------------------------------

    private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing)
        {
            return;
        }

        // The list items are in enum order (System, Light, Dark), so the
        // selected index is the preference. The selection is pending: the
        // theme is applied when the settings are saved.
        _pending.ThemePreference = (ThemePreference)ThemeSelector.SelectedIndex;
    }

    // --------------------------------------------------------------------
    // The transaction's footer
    // --------------------------------------------------------------------

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // The save action is only reachable from the running application
        // (the button lives in a window the App creates): without it
        // there is nothing to apply the settings to.
        var app = App!;
        var result = app.ApplySettings(_pending);

        switch (result.Outcome)
        {
            case App.SettingsApplyOutcome.Applied:
                // Everything is active and persisted: the window has done
                // its job.
                Close();
                break;

            case App.SettingsApplyOutcome.SaveFailed:
                // The change is active for this session; only the file
                // could not be written.
                MessageBox.Show(this,
                    "As configurações foram aplicadas, mas o arquivo não pôde ser salvo.\n" +
                    "Elas valem apenas para esta sessão; na próxima inicialização as configurações anteriores voltam.",
                    "CapParse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Close();
                break;

            case App.SettingsApplyOutcome.Rejected:
                // The application already restored the previous state:
                // discard the pending change, show the explanation, and
                // let the user fix it (for example pick a free
                // combination or an existing folder).
                _pending = app.SavedSettingsSnapshot();
                ShowState();
                MessageBox.Show(this,
                    result.Message,
                    "CapParse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                break;
        }
    }

    // Cancel — and closing the window with the X — discard the pending
    // change: no setting is modified and no registration is touched.
    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => Close();

    // --------------------------------------------------------------------
    // State display and recording
    // --------------------------------------------------------------------

    /// <summary>
    /// Refreshes every control from the pending settings and the
    /// application's operational state (the active capture hotkey, the
    /// direct slots that are registered right now). Called after any
    /// user change and after a rejected save; the controls set from here
    /// never trigger handlers (the "only when different" guards and
    /// <c>_initializing</c>), so the display and the user's edits cannot
    /// feed back into each other.
    /// </summary>
    private void ShowState()
    {
        var app = App;
        // With no running application (the test host) the settings file
        // is the source of truth, like the constructor's fallback.
        var saved = app?.SavedSettingsSnapshot() ?? AppSettings.Load();

        // Capture hotkey: the pending combination, with notes about the
        // operational state (a saved combination that could not be
        // registered) and about the pending change itself.
        var pendingCapture = _pending.CaptureHotkey;
        CurrentHotkeyText.Text = pendingCapture.DisplayString;

        var notes = new List<string>();
        var active = app?.ActiveHotkey;
        if (active is null)
        {
            notes.Add("O atalho global não pôde ser registrado. " +
                      "A captura continua disponível pelo menu da bandeja.");
        }
        else if (!active.Equals(saved.CaptureHotkey))
        {
            notes.Add($"O atalho salvo ({saved.CaptureHotkey.DisplayString}) não pôde ser registrado; " +
                      $"o atalho ativo é {active.DisplayString}.");
        }

        if (!pendingCapture.Equals(saved.CaptureHotkey))
        {
            notes.Add("O novo atalho será aplicado ao salvar as configurações.");
        }

        CurrentNote.Text = string.Join("\n", notes);
        CurrentNote.Visibility = notes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Back to the normal display (also exits a recording in progress).
        CurrentPanel.Visibility = Visibility.Visible;
        RecordingPanel.Visibility = Visibility.Collapsed;
        DirectSlotsPanel.Visibility = Visibility.Visible;
        DirectRecordingPanel.Visibility = Visibility.Collapsed;

        // Direct mode.
        if (DirectModeToggle.IsChecked != _pending.DirectModeEnabled)
        {
            DirectModeToggle.IsChecked = _pending.DirectModeEnabled;
        }

        DirectContent.IsEnabled = _pending.DirectModeEnabled;

        // The dynamic state of the direct mode: what the pending
        // configuration contains and which registrations are live, so the
        // user sees what is configured and what is missing (the per-slot
        // notes carry the individual registration failures). Hidden while
        // the mode is off.
        var status = DirectModeStatus.Message(
            _pending.DirectModeEnabled,
            PendingDirectSlots(),
            slot => app?.DirectSlotOperationalState(slot).Registered ?? false);
        DirectModeStateText.Text = status ?? string.Empty;
        DirectModeStateText.Visibility =
            _pending.DirectModeEnabled ? Visibility.Visible : Visibility.Collapsed;

        ShowSlot(HotkeySlots.DirectCopyImage, DirectCopyImageText, DirectCopyImageClearButton, DirectCopyImageNote);
        ShowSlot(HotkeySlots.DirectCopyText, DirectCopyTextText, DirectCopyTextClearButton, DirectCopyTextNote);
        ShowSlot(HotkeySlots.DirectSaveImage, DirectSaveImageText, DirectSaveImageClearButton, DirectSaveImageNote);

        if (DirectPolicySelector.SelectedIndex != (int)_pending.DirectSavePolicy)
        {
            DirectPolicySelector.SelectedIndex = (int)_pending.DirectSavePolicy;
        }

        // The folder row is shown (and required) only with the automatic
        // policy.
        var automatic = _pending.DirectSavePolicy == ImageSavePolicy.Automatic;
        DirectFolderPanel.Visibility = automatic ? Visibility.Visible : Visibility.Collapsed;
        if (DirectSaveFolderText.Text != _pending.DirectSaveFolder)
        {
            DirectSaveFolderText.Text = _pending.DirectSaveFolder;
        }

        if (ThemeSelector.SelectedIndex != (int)_pending.ThemePreference)
        {
            ThemeSelector.SelectedIndex = (int)_pending.ThemePreference;
        }
    }

    /// <summary>
    /// Shows one direct slot: its pending combination (or "not
    /// configured"), the clear action (only when something is configured)
    /// and a note when the mode is enabled and the slot is configured but
    /// its combination is not registered right now (another application
    /// owns it).
    /// </summary>
    private void ShowSlot(int slot, TextBlock display, Button clearButton, TextBlock note)
    {
        var combination = _pending.DirectSlot(slot);
        display.Text = combination?.DisplayString ?? "Não configurado";
        display.Foreground = combination is null
            ? (System.Windows.Media.Brush)FindResource("TextMutedBrush")
            : (System.Windows.Media.Brush)FindResource("TextPrimaryBrush");
        clearButton.IsEnabled = combination is not null;

        // The warning reflects the running application's state — the
        // committed settings and the registration the OS holds right now
        // — not the pending edit: a not-yet-saved combination cannot have
        // failed to register, and a slot that registered successfully must
        // not keep showing a previous failure.
        var app = App;
        var saved = app?.SavedSettingsSnapshot() ?? AppSettings.Load();
        var registered = app?.DirectSlotOperationalState(slot).Registered ?? false;
        if (DirectModeStatus.SlotWarningVisible(
            saved.DirectModeEnabled, saved.DirectSlot(slot), registered))
        {
            note.Text = "Este atalho não pôde ser registrado; outro aplicativo pode estar usando essa combinação.";
            note.Visibility = Visibility.Visible;
        }
        else
        {
            note.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>The pending direct-slot configuration, as the status message consumes it.</summary>
    private static Dictionary<int, HotkeySetting?> PendingSlotsOf(AppSettings settings)
        => new()
        {
            [HotkeySlots.DirectCopyImage] = settings.DirectSlot(HotkeySlots.DirectCopyImage),
            [HotkeySlots.DirectCopyText] = settings.DirectSlot(HotkeySlots.DirectCopyText),
            [HotkeySlots.DirectSaveImage] = settings.DirectSlot(HotkeySlots.DirectSaveImage)
        };

    /// <summary>The pending direct-slot configuration of this window.</summary>
    private Dictionary<int, HotkeySetting?> PendingDirectSlots() => PendingSlotsOf(_pending);

    /// <summary>The slot id a tagged button carries (its XAML Tag).</summary>
    private static int? SlotOf(object? sender)
        => sender is FrameworkElement { Tag: string tag }
           && int.TryParse(tag, out var slot)
           ? slot
           : null;

    /// <summary>The human-readable name of a direct slot, for the recording prompt.</summary>
    private static string SlotLabel(int slot)
        => slot == HotkeySlots.Capture
            ? "captura"
            : DirectModeStatus.SlotName(slot);

    /// <summary>
    /// Starts recording a new combination for <paramref name="slot"/>:
    /// the matching dashed target receives the keyboard focus and the
    /// next valid key press is recorded (Esc cancels). The capture slot
    /// uses the capture pane's target; the direct slots share the direct
    /// pane's target.
    /// </summary>
    private void StartRecording(int slot)
    {
        _recordingSlot = slot;

        if (slot == HotkeySlots.Capture)
        {
            RecordingError.Text = string.Empty;
            RecordingError.Visibility = Visibility.Collapsed;

            CurrentPanel.Visibility = Visibility.Collapsed;
            RecordingPanel.Visibility = Visibility.Visible;

            KeyCaptureTarget.Focus();
        }
        else
        {
            RecordingDirectError.Text = string.Empty;
            RecordingDirectError.Visibility = Visibility.Collapsed;

            RecordingDirectText.Text =
                $"Pressione a combinação de teclas do novo atalho de {SlotLabel(slot)}.";

            DirectSlotsPanel.Visibility = Visibility.Collapsed;
            DirectRecordingPanel.Visibility = Visibility.Visible;

            KeyCaptureTargetDirect.Focus();
        }
    }

    /// <summary>
    /// Handles a key press while recording a new combination. The
    /// interpretation (key/system key/modifier state → decision) lives in
    /// <see cref="HotkeyInput"/> so it is testable without a window; this
    /// handler only applies the decision to the window's states. The
    /// recorded combination is stored in the slot that started the
    /// recording — as a pending change, like every other edit.
    /// </summary>
    private void RecordingTarget_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        switch (HotkeyInput.Interpret(e.Key, e.SystemKey, Keyboard.Modifiers, out var candidate))
        {
            case HotkeyInput.Interpretation.Waiting:
                break;

            case HotkeyInput.Interpretation.Canceled:
                ShowState();
                break;

            case HotkeyInput.Interpretation.Invalid:
                ShowRecordingError(sender);
                break;

            case HotkeyInput.Interpretation.Recorded:
                // Interpret only reports Recorded with a valid candidate.
                if (candidate is not { } recorded)
                {
                    break;
                }

                if (_recordingSlot == HotkeySlots.Capture)
                {
                    _pending.CaptureHotkey = recorded;
                }
                else
                {
                    _pending.SetDirectSlot(_recordingSlot, recorded);
                }

                ShowState();
                break;
        }
    }

    private void ShowRecordingError(object? sender)
    {
        var message =
            "Combinação inválida. Use Ctrl, Alt ou Windows com uma letra, um número ou uma tecla F.";

        if (sender == KeyCaptureTargetDirect)
        {
            RecordingDirectError.Text = message;
            RecordingDirectError.Visibility = Visibility.Visible;
            KeyCaptureTargetDirect.Focus();
        }
        else
        {
            RecordingError.Text = message;
            RecordingError.Visibility = Visibility.Visible;
            KeyCaptureTarget.Focus();
        }
    }
}
