using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace CapParse.UI;

/// <summary>
/// The application's own transient confirmation: a small, theme-aware
/// toast that reports the result of a capture operation without
/// depending on the Windows notification stack. It is the single
/// presentation of a capture result: the legacy tray balloon is
/// fire-and-forget and has been observed closing early on this machine
/// (see docs/development.md §71.3), so it no longer mirrors a capture
/// result — it is kept only for the first-run mode notice (§73).
///
/// The window is the presentation only: what to say is decided elsewhere
/// (<see cref="CaptureConfirmation"/>) and the show/replace/hide state
/// lives in the timer-agnostic <see cref="CaptureToastPresenter"/>, which
/// is what the tests exercise. The application creates one window
/// instance and reuses it for every confirmation: a new confirmation
/// replaces the current one in place (content swap, timer restart), so
/// an older operation's message can never survive a newer one, and no
/// second toast window is ever created.
///
/// It never takes focus (<c>ShowActivated=false</c>,
/// <c>Focusable=false</c>), never appears in the taskbar, never receives
/// input (<c>IsHitTestVisible=false</c>), and hides itself after a short
/// interval. Appearing and disappearing are plain opacity fades, so the
/// toast never flashes. It is anchored to the bottom-right corner of the
/// primary monitor's work area — next to the tray icon, away from the
/// region the user just captured — and the application closes it on
/// exit, so it can never outlive the process.
/// </summary>
public sealed class CaptureToastWindow : Window
{
    // The margin from the work area's edges: clears the taskbar and the
    // screen edge without hugging either.
    private const double EdgeMargin = 24;

    private const double FadeInSeconds = 0.12;
    private const double FadeOutSeconds = 0.15;

    // The time a confirmation stays on screen: long enough to be read,
    // short enough not to sit in the corner between captures.
    private static readonly TimeSpan AutoHideAfter = TimeSpan.FromSeconds(3);

    // The system icon font the main window already uses for its title-row
    // glyph (Segoe Fluent Icons on Windows 11, Segoe MDL2 Assets on
    // Windows 10 — both carry the same glyphs at these code points).
    private static readonly System.Windows.Media.FontFamily IconFontFamily =
        new("Segoe Fluent Icons, Segoe MDL2 Assets");

    private const string GlyphSuccess = "\uE73E"; // check mark
    private const string GlyphInfo = "\uE94A";   // info
    private const string GlyphFailure = "\uE783"; // error circle

    private readonly CaptureToastPresenter _presenter = new();
    private readonly DispatcherTimer _hideTimer;
    private readonly TextBlock _glyph;
    private readonly TextBlock _message;

    /// <summary>
    /// The message of the confirmation currently on screen (the last one
    /// presented, kept while the toast is hidden between confirmations):
    /// a read-only view of the presented state, so tests can assert on it
    /// without reaching into the window's visual tree.
    /// </summary>
    public string CurrentMessage => _presenter.CurrentMessage;

    /// <summary>
    /// Creates the toast window (not shown).
    /// </summary>
    public CaptureToastWindow()
    {
        // A chromeless, transparent, non-activating, non-interactive
        // top-level window: everything the confirmation needs and nothing
        // it should not have (no focus, no taskbar entry, no input).
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = System.Windows.Media.Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Focusable = false;
        IsHitTestVisible = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        SizeToContent = SizeToContent.WidthAndHeight;

        _glyph = new TextBlock
        {
            FontFamily = IconFontFamily,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center
        };

        _message = new TextBlock
        {
            FontSize = 13,
            MaxWidth = 300,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        _message.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");

        var panel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        panel.Children.Add(_glyph);
        panel.Children.Add(_message);

        var border = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 9, 12, 9),
            Child = panel,
            // A soft shadow separates the toast from any screen content
            // behind it, on both themes, without a second window.
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.35,
                BlurRadius = 14,
                ShadowDepth = 0
            }
        };
        border.SetResourceReference(Border.BackgroundProperty, "SurfaceBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

        Content = border;

        // A UI-thread timer: it starts, restarts and stops on the same
        // thread that shows and hides the window, so the lifecycle can
        // never race itself across threads.
        _hideTimer = new DispatcherTimer
        {
            Interval = AutoHideAfter
        };
        _hideTimer.Tick += (_, _) => BeginHide();

        // The content size changes when a new (longer or shorter) message
        // is presented: re-anchor while on screen so the corner stays
        // pinned as the toast grows or shrinks.
        SizeChanged += (_, _) =>
        {
            if (IsVisible)
            {
                PositionAtWorkAreaCorner();
            }
        };
    }

    /// <summary>
    /// Shows (or replaces, while visible) the toast with
    /// <paramref name="confirmation"/> and (re)starts its auto-hide.
    /// A call while a fade-out is running cancels it: the replacement is
    /// immediate and predictable. Call on the UI thread.
    /// </summary>
    public void Present(CaptureConfirmation confirmation)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        // The state transition (what is on screen, whether it is visible)
        // is the presenter's; the window only renders it.
        _presenter.Present(confirmation);

        _message.Text = confirmation.Message;
        ApplyGlyph(confirmation.Kind);

        // Measure the (possibly new) content before positioning, so the
        // window is placed correctly on its very first frame — no visible
        // jump from a default position to the corner.
        UpdateLayout();
        PositionAtWorkAreaCorner();

        if (!IsVisible)
        {
            // Start transparent and fade in: the window's first visible
            // frame is already its final content, at its final position.
            Opacity = 0;
            Show();
        }

        FadeTo(1.0, FadeInSeconds, null);

        _hideTimer.Stop();
        _hideTimer.Start();
    }

    private void ApplyGlyph(CaptureConfirmationKind kind)
    {
        var (glyph, brushKey) = kind switch
        {
            CaptureConfirmationKind.Success => (GlyphSuccess, "SuccessBrush"),
            CaptureConfirmationKind.Info => (GlyphInfo, "TextSecondaryBrush"),
            _ => (GlyphFailure, "ErrorBrush")
        };

        _glyph.Text = glyph;
        // Setting the resource reference again (with a new key) replaces
        // the previous one on the same property.
        _glyph.SetResourceReference(TextBlock.ForegroundProperty, brushKey);
    }

    /// <summary>
    /// Anchors the toast to the bottom-right corner of the primary
    /// monitor's work area. The work area (taskbar excluded) is what
    /// <see cref="SystemParameters.WorkArea"/> reports, in the primary
    /// monitor's DIPs — the same coordinate space the window's
    /// <c>Left</c>/<c>Top</c> use, so no DPI conversion is involved and
    /// the corner is correct at any scaling. The toast belongs to the
    /// application's tray presence, which lives on the primary monitor,
    /// and sits above the taskbar, clear of the captured region.
    /// </summary>
    private void PositionAtWorkAreaCorner()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - EdgeMargin - ActualWidth;
        Top = workArea.Bottom - EdgeMargin - ActualHeight;
    }

    private void BeginHide()
    {
        _presenter.AutoHide();
        _hideTimer.Stop();
        FadeTo(0.0, FadeOutSeconds, Hide);
    }

    private void FadeTo(double target, double seconds, Action? onCompleted)
    {
        var animation = new DoubleAnimation(target, TimeSpan.FromSeconds(seconds));

        // Commit the base value when the fade finishes: without it the
        // property would snap back to its pre-animation value the moment
        // the animation ends (the toast would vanish right after the
        // fade-in, or reappear after the fade-out).
        animation.Completed += (_, _) =>
        {
            Opacity = target;
            onCompleted?.Invoke();
        };

        BeginAnimation(OpacityProperty, animation);
    }
}
