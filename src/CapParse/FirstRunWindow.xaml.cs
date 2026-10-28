using System.Windows;

namespace CapParse;

/// <summary>
/// The mode the first-run presentation lets the user choose (ui.md §23):
/// the traditional flow (the result window and its manual actions), the
/// direct mode (an action runs right after the selection) or deferring the
/// decision (the traditional mode keeps being used; the choice can be made
/// later in the settings).
/// </summary>
public enum FirstRunModeChoice
{
    /// <summary>
    /// No choice was made (the window was closed or "Agora não" was
    /// clicked): the traditional mode is kept.
    /// </summary>
    Deferred,

    /// <summary>The traditional flow: the result window and its buttons.</summary>
    Traditional,

    /// <summary>
    /// The direct mode: an action runs right after the selection. The
    /// presentation never creates a hotkey for it — the shortcuts are
    /// configured later in the settings.
    /// </summary>
    Direct
}

/// <summary>
/// The short, non-mandatory first-run presentation (ui.md §23): what the
/// application does, how to start a capture (the active hotkey and the
/// tray menu) and the choice between the traditional mode and the direct
/// mode (or deferring the decision). It is shown only on the first run
/// (the app records that) and is skippable: closing the window without
/// choosing keeps the traditional mode, so a dismissed presentation never
/// changes a setting, never creates a hotkey and never blocks the normal
/// use of the application.
/// </summary>
public partial class FirstRunWindow : Window
{
    // Reserve of the window chrome (title bar) plus a small buffer, so the
    // window — chrome included, not just its content — always fits inside
    // the available desktop area. The window is centered on screen, so the
    // primary screen's work area is the one that matters.
    private const double ChromeReserve = 60;

    // The smallest content height the window is allowed to demand, so an
    // absurdly small work area (a very low screen at a large scale) cannot
    // collapse the content to nothing; below this the window simply cannot
    // fit and the screen is the limiting factor, not the content.
    private const double MinimumContentHeight = 120;

    // The mode the user chose in the presentation. Deferred is the
    // default: closing the window without choosing (the X button, Esc)
    // keeps the traditional mode, so the presentation can never leave a
    // partial change behind.
    public FirstRunModeChoice Choice { get; private set; } = FirstRunModeChoice.Deferred;

    /// <param name="howToCapture">
    /// The line that tells the user how to start a capture, already
    /// adapted to the hotkey that is active at startup.
    /// </param>
    public FirstRunWindow(string howToCapture)
    {
        InitializeComponent();
        HowToCaptureText.Text = howToCapture;

        // The window sizes itself to its content (SizeToContent="Height"),
        // which keeps every paragraph and the button visible. The
        // ScrollViewer caps that growth at the available desktop area:
        // when the content is taller than the screen, the window stops
        // growing and scrolls instead of extending past the work area.
        ContentScroller.MaxHeight = ContentMaxHeight(SystemParameters.WorkArea.Height);
    }

    /// <summary>
    /// The maximum height the window's content may demand, given the
    /// available desktop area: the work area minus the window chrome
    /// reserve, never below <see cref="MinimumContentHeight"/>. Pure so it
    /// is testable without a desktop session.
    /// </summary>
    internal static double ContentMaxHeight(double workAreaHeight)
        => Math.Max(MinimumContentHeight, workAreaHeight - ChromeReserve);

    private void TraditionalButton_Click(object sender, RoutedEventArgs e)
        => ChooseAndClose(FirstRunModeChoice.Traditional);

    private void DirectButton_Click(object sender, RoutedEventArgs e)
        => ShowDirectConfirmation();

    private void DeferButton_Click(object sender, RoutedEventArgs e)
        => ChooseAndClose(FirstRunModeChoice.Deferred);

    /// <summary>
    /// Choosing the direct mode does not close the window on the spot:
    /// the choice card is replaced by the confirmation card, so the user
    /// sees the activation acknowledged before the window closes (the tray
    /// balloon that reports the same fact is fire-and-forget and easy to
    /// miss). The choice is recorded right away, so closing the window in
    /// the confirmation state (the "Fechar" button or the close button)
    /// applies exactly the mode that was chosen.
    /// </summary>
    private void ShowDirectConfirmation()
    {
        Choice = FirstRunModeChoice.Direct;
        ChoiceCard.Visibility = Visibility.Collapsed;
        DirectConfirmationCard.Visibility = Visibility.Visible;
        CloseButton.Focus();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    private void ChooseAndClose(FirstRunModeChoice choice)
    {
        Choice = choice;
        Close();
    }
}
