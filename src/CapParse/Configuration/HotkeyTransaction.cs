namespace CapParse.Configuration;

/// <summary>
/// What a settings transaction (or the startup) must do to the
/// traditional capture hotkey. The decision is pure — no OS interaction —
/// so it is testable without Windows; the application executes the plan
/// through its <c>GlobalHotkey</c> and rolls it back with the same
/// snapshot the direct-mode registrations use.
/// </summary>
public sealed record CaptureHotkeyPlan
{
    /// <summary>What to do with the traditional capture hotkey.</summary>
    public enum Action
    {
        /// <summary>Leave the current registration exactly as it is.</summary>
        NoChange,

        /// <summary>
        /// The capture hotkey must end registered with
        /// <see cref="Combination"/> (through the recovery chain: the
        /// requested combination, then the previous active one, then the
        /// default).
        /// </summary>
        Register,

        /// <summary>
        /// The capture hotkey must end unregistered (direct mode is
        /// active). The saved combination is preserved in the settings:
        /// unregistering never deletes the preference, and the
        /// combination becomes free for a direct slot.
        /// </summary>
        Unregister
    }

    public Action Kind { get; }
    public HotkeySetting? Combination { get; }

    private CaptureHotkeyPlan(Action kind, HotkeySetting? combination)
    {
        Kind = kind;
        Combination = combination;
    }

    /// <summary>The capture hotkey stays as it is (nothing to do).</summary>
    public static CaptureHotkeyPlan Keep { get; } = new(Action.NoChange, null);

    public static CaptureHotkeyPlan RegisterCapture(HotkeySetting combination)
        => new(Action.Register, combination);

    public static CaptureHotkeyPlan UnregisterCapture()
        => new(Action.Unregister, null);
}

/// <summary>
/// The mode-aware decisions of the hotkey side of a settings
/// transaction: which hotkey family the mode activates, what the
/// traditional capture hotkey must become, and which combinations may
/// conflict with each other. Pure functions over the settings values, so
/// the transaction's correctness (startup, switching the mode on or off,
/// rollback) is testable without Windows.
///
/// The rule both methods implement: in direct mode the traditional
/// capture hotkey is not registered — only the direct slots are — so the
/// saved traditional combination neither blocks a direct slot nor can be
/// re-registered by the recovery chain; in traditional mode the capture
/// hotkey is registered and the direct slots are not.
/// </summary>
public static class HotkeyTransaction
{
    /// <summary>
    /// Whether the startup registers the traditional capture hotkey. In
    /// direct mode the startup registers only the direct slots, so a
    /// launch that is already configured for direct mode never brings the
    /// traditional shortcut back to life.
    /// </summary>
    public static bool RegisterCaptureAtStartup(AppSettings settings)
        => !settings.DirectModeEnabled;

    /// <summary>
    /// What the transaction from <paramref name="current"/> (the committed
    /// settings) to <paramref name="pending"/> must do to the traditional
    /// capture hotkey: direct mode unregisters it (the saved combination
    /// is kept in the settings); traditional mode registers the
    /// combination when it changed — or when the mode comes back from
    /// direct mode, where the hotkey is unregistered by definition — and
    /// leaves an unchanged traditional registration untouched.
    /// </summary>
    public static CaptureHotkeyPlan PlanCapture(AppSettings current, AppSettings pending)
    {
        if (pending.DirectModeEnabled)
        {
            return CaptureHotkeyPlan.UnregisterCapture();
        }

        if (current.DirectModeEnabled
            || !pending.CaptureHotkey.Equals(current.CaptureHotkey))
        {
            return CaptureHotkeyPlan.RegisterCapture(pending.CaptureHotkey);
        }

        return CaptureHotkeyPlan.Keep;
    }

    /// <summary>
    /// The traditional capture hotkey's callback guard: a press of the
    /// traditional combination may start a capture only when the
    /// traditional mode is effectively in force — the committed mode is
    /// traditional and the open settings window is not holding a pending
    /// direct mode (which suspends the registration for the duration of
    /// the shortcut mapping). While the direct mode is in effect in either
    /// form the callback must stay silent, whatever the OS registration
    /// says: a stale WM_HOTKEY queued before the suspension, or a
    /// registration that should not exist, can never start a traditional
    /// capture.
    /// </summary>
    public static bool TraditionalCaptureAllowed(bool committedDirectMode, bool pendingDirectMode)
        => !committedDirectMode && !pendingDirectMode;

    /// <summary>
    /// The combinations (each reported once) that more than one slot
    /// *that will be active in the destination mode* carries. The modes
    /// are mutually exclusive — only one family is registered at a time —
    /// so a combination saved in both families is valid in both and can
    /// never point one shortcut at two actions: in direct mode only the
    /// direct slots check against each other (the traditional capture
    /// hotkey is not registered, so its saved combination does not block
    /// any direct slot); in traditional mode only the capture hotkey is
    /// registered (the direct slots are not, so their saved combinations
    /// do not conflict with it). Switching modes therefore never requires
    /// changing a shortcut, and a real registration failure (another
    /// application owns the combination) is reported by the OS, not here.
    /// </summary>
    public static List<HotkeySetting> FindConflicts(AppSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var slots = new Dictionary<int, HotkeySetting?>();
        if (settings.DirectModeEnabled)
        {
            slots[HotkeySlots.DirectCopyImage] = settings.DirectSlot(HotkeySlots.DirectCopyImage);
            slots[HotkeySlots.DirectCopyText] = settings.DirectSlot(HotkeySlots.DirectCopyText);
            slots[HotkeySlots.DirectSaveImage] = settings.DirectSlot(HotkeySlots.DirectSaveImage);
        }
        else
        {
            slots[HotkeySlots.Capture] = settings.CaptureHotkey;
        }

        return HotkeyConflictValidator.FindConflicts(slots);
    }
}
