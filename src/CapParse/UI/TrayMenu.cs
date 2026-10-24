using System.Windows.Forms;

namespace CapParse.UI;

/// <summary>
/// The tray icon's context menu and the operational state of its items.
///
/// The "Capturar" item starts the traditional capture workflow. The direct
/// mode disables it: the application keeps its enabled state in sync with
/// the mode that is effectively in force (<see cref="SetCaptureEnabled"/>),
/// and the item's callback is the application's guarded traditional-capture
/// entry point, which applies the same guard — so the visual state and the
/// action can never disagree: a disabled item reached by a stale click, an
/// old callback, or any other path can never start a traditional capture
/// while the direct mode is in force. The other items (settings, exit) are
/// mode-independent and always available.
///
/// All items are plain <see cref="ToolStripMenuItem"/>s with injected
/// callbacks, so the menu's real behavior (which item does what, the
/// enabled state, the click dispatch) is testable on an STA thread without
/// the application and without the tray icon (the application keeps the
/// <c>NotifyIcon</c> and attaches <see cref="Menu"/> to it).
/// </summary>
public sealed class TrayMenu
{
    public TrayMenu(Action onCapture, Action onSettings, Action onExit)
    {
        ArgumentNullException.ThrowIfNull(onCapture);
        ArgumentNullException.ThrowIfNull(onSettings);
        ArgumentNullException.ThrowIfNull(onExit);

        CaptureItem = new ToolStripMenuItem("Capturar");
        CaptureItem.Click += (_, _) => onCapture();

        SettingsItem = new ToolStripMenuItem("Configurações");
        SettingsItem.Click += (_, _) => onSettings();

        ExitItem = new ToolStripMenuItem("Sair");
        ExitItem.Click += (_, _) => onExit();

        Menu.Items.Add(CaptureItem);
        Menu.Items.Add(SettingsItem);
        Menu.Items.Add(ExitItem);
    }

    /// <summary>The menu the application attaches to the tray icon.</summary>
    public ContextMenuStrip Menu { get; } = new();

    /// <summary>
    /// The "Capturar" item: it starts the traditional capture. The
    /// application disables it while the direct mode is effectively in
    /// force and enables it in the traditional mode.
    /// </summary>
    public ToolStripMenuItem CaptureItem { get; }

    /// <summary>The "Configurações" item: it opens the settings window.</summary>
    public ToolStripMenuItem SettingsItem { get; }

    /// <summary>The "Sair" item: it terminates the application.</summary>
    public ToolStripMenuItem ExitItem { get; }

    /// <summary>
    /// Sets the "Capturar" item's enabled state: enabled in the traditional
    /// mode, disabled while the direct mode is in force.
    /// </summary>
    public void SetCaptureEnabled(bool enabled)
        => CaptureItem.Enabled = enabled;
}
