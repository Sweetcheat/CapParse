namespace CapParse.UI;

/// <summary>
/// The small, timer-agnostic state machine behind the transient capture
/// confirmation: which message is on screen, whether the toast is visible,
/// and how a new result replaces the current one. It is the testable core
/// of <see cref="CaptureToastWindow"/>: the window is only its
/// presentation (the WPF chrome, the fade, the real auto-hide timer), so
/// the behavior that matters — a result is shown, a newer result replaces
/// the older one in place, the toast hides, a later result shows it
/// again — can be verified without a desktop, without a shown window and
/// without a real timer.
///
/// It owns no window, no dispatcher and no clock: the presentation decides
/// when the auto-hide elapses and calls <see cref="AutoHide"/>; everything
/// else is a plain state transition.
/// </summary>
public sealed class CaptureToastPresenter
{
    private CaptureConfirmation? _current;
    private bool _visible;

    /// <summary>
    /// True while a confirmation is on screen. The presentation sets it
    /// through <see cref="Present"/> and <see cref="AutoHide"/> only.
    /// </summary>
    public bool IsVisible => _visible;

    /// <summary>
    /// The confirmation currently on screen (the last one presented, kept
    /// after the toast hides so the message is not lost between
    /// confirmations), or null while nothing has been presented.
    /// </summary>
    public CaptureConfirmation? Current => _current;

    /// <summary>
    /// The message of <see cref="Current"/>, or the empty string while
    /// nothing has been presented: a read-only view for the presentation
    /// and the tests.
    /// </summary>
    public string CurrentMessage => _current?.Message ?? string.Empty;

    /// <summary>
    /// Brings <paramref name="confirmation"/> on screen. While a
    /// confirmation is already visible it replaces it in place — the same
    /// "window", a newer message, the older one gone — so an older
    /// operation's message can never survive a newer result. The
    /// presentation restarts the auto-hide when it calls this.
    /// </summary>
    public void Present(CaptureConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        _current = confirmation;
        _visible = true;
    }

    /// <summary>
    /// Hides the toast: the auto-hide interval elapsed. The last message
    /// is kept (see <see cref="Current"/>) so it is not lost, and a later
    /// <see cref="Present"/> shows it again.
    /// </summary>
    public void AutoHide()
    {
        _visible = false;
    }
}
