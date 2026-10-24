namespace CapParse.Configuration;

/// <summary>
/// The operational owner of the traditional capture hotkey's registration:
/// the combination it is registered with right now (<see cref="Active"/>,
/// null when it is unregistered) and the mode-aware operations over it.
///
/// The registration follows the mode that is *effectively* in force, which
/// is the committed mode plus — while the settings window is open — the
/// pending mode it is editing: a pending direct mode suspends the
/// registration for the duration of the shortcut mapping (so the
/// traditional combination, for example Ctrl + Shift + X, can be recorded
/// into a direct slot without starting a traditional capture), a pending
/// traditional mode restores it only when the committed mode is
/// traditional, and a closed (saved or canceled) window leaves the
/// registration in the state the committed mode prescribes. The saved
/// combination in the settings is a preference: suspending and restoring
/// the registration never touches it.
///
/// All OS interaction goes through the injected <c>tryRegister</c> and
/// <c>unregister</c> functions, so the transactional behavior — startup in
/// either mode, the settings window's pending mode, saving, canceling and
/// rolling back a failed transaction — is testable without Windows.
/// </summary>
public sealed class CaptureHotkeyCoordinator
{
    private readonly Func<HotkeySetting, bool> _tryRegister;
    private readonly Action _unregister;

    public CaptureHotkeyCoordinator(Func<HotkeySetting, bool> tryRegister, Action unregister)
    {
        _tryRegister = tryRegister ?? throw new ArgumentNullException(nameof(tryRegister));
        _unregister = unregister ?? throw new ArgumentNullException(nameof(unregister));
    }

    /// <summary>
    /// The combination the traditional capture hotkey is registered with
    /// right now, or null when it is unregistered. The single operational
    /// fact the application and the views read; it is distinct from the
    /// saved combination in the settings (the preference), which survives
    /// every suspension.
    /// </summary>
    public HotkeySetting? Active { get; private set; }

    /// <summary>
    /// The startup: traditional mode registers the saved combination
    /// through the recovery chain (the requested combination, then the
    /// default); direct mode registers nothing — a launch that is already
    /// configured for direct mode never brings the traditional shortcut
    /// back to life.
    /// </summary>
    public void Start(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.DirectModeEnabled)
        {
            return;
        }

        Active = HotkeyRecovery.Resolve(null, settings.CaptureHotkey, _tryRegister);
    }

    /// <summary>
    /// The settings window's pending mode change (its direct-mode toggle):
    /// a pending direct mode suspends the registration — unregistering
    /// cannot fail, and a registration the mode says must not exist is
    /// released even when the shadow state did not know about it; a
    /// pending traditional mode re-registers through the recovery chain
    /// only when the committed mode is traditional, while the committed
    /// direct mode keeps the hotkey unregistered no matter what the window
    /// is about to save.
    /// </summary>
    public void ApplyPendingMode(AppSettings committed, bool pendingDirectMode)
    {
        ArgumentNullException.ThrowIfNull(committed);

        if (pendingDirectMode)
        {
            Suspend();
        }
        else if (!committed.DirectModeEnabled)
        {
            EnsureRegistered(committed.CaptureHotkey);
        }
    }

    /// <summary>
    /// The settings window closed without saving (cancel, or the X): the
    /// registration goes back to the state the committed mode prescribes —
    /// registered in traditional mode, unregistered in direct mode — so a
    /// canceled session never leaves a suspended or a stray registration
    /// behind.
    /// </summary>
    public void SyncWithCommittedMode(AppSettings committed)
    {
        ArgumentNullException.ThrowIfNull(committed);

        if (committed.DirectModeEnabled)
        {
            Suspend();
        }
        else
        {
            EnsureRegistered(committed.CaptureHotkey);
        }
    }

    /// <summary>
    /// The capture half of the save-time transaction (see
    /// <see cref="HotkeyTransaction.PlanCapture"/>): direct mode
    /// unregisters (cannot fail); traditional mode registers the
    /// combination when it changed, through the recovery chain. Returns
    /// whether the capture side is consistent with
    /// <paramref name="pending"/> — the transaction rolls back when it is
    /// not.
    /// </summary>
    public bool ApplyPlan(AppSettings committed, AppSettings pending)
    {
        ArgumentNullException.ThrowIfNull(committed);
        ArgumentNullException.ThrowIfNull(pending);

        var plan = HotkeyTransaction.PlanCapture(committed, pending);

        switch (plan.Kind)
        {
            case CaptureHotkeyPlan.Action.Unregister:
                Suspend();
                return true;

            case CaptureHotkeyPlan.Action.Register:
                var requested = plan.Combination!;
                if (requested.Equals(Active))
                {
                    return true;
                }

                Active = HotkeyRecovery.Resolve(Active, requested, _tryRegister);
                return Active is { } active && active.Equals(requested);

            default:
                // NoChange: the registration is left exactly as it is.
                return true;
        }
    }

    /// <summary>
    /// The transaction's rollback half: the registration goes back to
    /// <paramref name="previous"/> (the state before the attempt) through
    /// the recovery chain. A null previous state means the hotkey was
    /// unregistered before the attempt — in direct mode that is the
    /// invariant state, so the rollback unregisters (a no-op when it is
    /// already off) and the recovery chain can never bring the traditional
    /// hotkey back while the mode is on.
    /// </summary>
    public void Restore(HotkeySetting? previous)
    {
        if (previous is null)
        {
            Suspend();
            return;
        }

        if (Active == previous)
        {
            return;
        }

        Active = HotkeyRecovery.Resolve(Active, previous, _tryRegister);
    }

    // The suspend always releases the OS registration (a no-op when it is
    // already off), so a registration the shadow state does not know about
    // can never survive a mode transition.
    private void Suspend()
    {
        _unregister();
        Active = null;
    }

    private void EnsureRegistered(HotkeySetting requested)
    {
        if (Active is not null)
        {
            return;
        }

        Active = HotkeyRecovery.Resolve(null, requested, _tryRegister);
    }
}
