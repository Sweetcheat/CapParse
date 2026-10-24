using CapParse.Configuration;
using CapParse.Platform;

namespace CapParse.Tests;

// The operational owner of the traditional capture hotkey's registration,
// exercised end to end with a fake registry: the mode that is *effectively*
// in force (the committed mode plus the settings window's pending mode) is
// what decides the registration. Direct mode — committed, or pending in the
// open settings window — never leaves the traditional hotkey registered;
// canceling a session restores the registration the committed mode
// prescribes; a failed transaction rolls both sides (capture and direct
// slots) back without the recovery chain ever bringing the traditional
// hotkey back while the mode is on. The OS interaction is faked, exactly
// like the applier's tests; the real RegisterHotKey/WM_HOTKEY pipeline is
// covered by the GlobalHotkey tests (see GlobalHotkeyStaleMessageTests).
public class CaptureHotkeyCoordinatorTests
{
    private static HotkeySetting CtrlShiftX()
        => new(
            (uint)GlobalHotkey.Modifier.Control | (uint)GlobalHotkey.Modifier.Shift,
            GlobalHotkey.KeyX);

    private static HotkeySetting CtrlA()
        => new((uint)GlobalHotkey.Modifier.Control, 0x41);

    private static HotkeySetting CtrlB()
        => new((uint)GlobalHotkey.Modifier.Control, 0x42);

    private static AppSettings Settings(
        bool directModeEnabled,
        HotkeySetting? capture = null,
        HotkeySetting? copyImage = null,
        HotkeySetting? copyText = null,
        HotkeySetting? saveImage = null)
    {
        var settings = new AppSettings
        {
            DirectModeEnabled = directModeEnabled,
            CaptureHotkey = capture ?? new AppSettings().CaptureHotkey
        };
        settings.SetDirectSlot(HotkeySlots.DirectCopyImage, copyImage);
        settings.SetDirectSlot(HotkeySlots.DirectCopyText, copyText);
        settings.SetDirectSlot(HotkeySlots.DirectSaveImage, saveImage);
        return settings;
    }

    // ------------------------------------------------------------------
    // 1) Startup
    // ------------------------------------------------------------------

    [Fact]
    public void Startup_TraditionalMode_RegistersTheSavedCombination()
    {
        var registry = new FakeCaptureRegistry();
        var coordinator = New(registry);

        coordinator.Start(Settings(false, capture: CtrlShiftX()));

        Assert.Equal(CtrlShiftX(), registry.Active);
    }

    [Fact]
    public void Startup_TraditionalMode_SavedCombinationTaken_FallsBackThroughTheChain()
    {
        // The saved combination is owned by another application: the
        // recovery chain falls back to the default, exactly as at the
        // application's startup.
        var registry = new FakeCaptureRegistry
        {
            FailRegistration = combo => combo.Equals(CtrlA())
        };
        var coordinator = New(registry);

        coordinator.Start(Settings(false, capture: CtrlA()));

        Assert.Equal(HotkeySetting.Default, registry.Active);
    }

    [Fact]
    public void Startup_DirectMode_NeverRegistersTheTraditionalCapture()
    {
        // A launch that is already configured for direct mode registers
        // only the direct slots (the application's job): the traditional
        // capture hotkey is not registered, before or after the direct
        // slots come up.
        var registry = new FakeCaptureRegistry();
        var coordinator = New(registry);

        coordinator.Start(Settings(true, capture: CtrlShiftX()));

        Assert.Null(registry.Active);
        Assert.Empty(registry.Calls);
    }

    // ------------------------------------------------------------------
    // 2) The settings window's pending mode (the shortcut mapping)
    // ------------------------------------------------------------------

    [Fact]
    public void PendingDirectMode_SuspendsTheTraditionalCapture_ForTheMapping()
    {
        // The reported defect: with the direct mode toggled on in the
        // settings window (a pending change), pressing the traditional
        // combination must not start a traditional capture — the
        // registration is suspended for the duration of the mapping.
        var registry = new FakeCaptureRegistry();
        var coordinator = New(registry);
        var committed = Settings(false, capture: CtrlShiftX());

        coordinator.Start(committed);
        Assert.Equal(CtrlShiftX(), registry.Active);

        // The user toggles the direct mode on (pending, not saved).
        coordinator.ApplyPendingMode(committed, pendingDirectMode: true);

        Assert.Null(registry.Active);

        // ...and the saved preference is untouched: the suspension is a
        // registration state, never a settings change.
        Assert.Equal(CtrlShiftX(), committed.CaptureHotkey);
    }

    [Fact]
    public void PendingTraditionalMode_RestoresTheCapture_OnlyInCommittedTraditionalMode()
    {
        var registry = new FakeCaptureRegistry();
        var coordinator = New(registry);
        var committed = Settings(false, capture: CtrlShiftX());

        coordinator.Start(committed);
        coordinator.ApplyPendingMode(committed, pendingDirectMode: true);
        Assert.Null(registry.Active);

        // The user toggles the mode back off (pending): the committed mode
        // is traditional, so the registration comes back.
        coordinator.ApplyPendingMode(committed, pendingDirectMode: false);

        Assert.Equal(CtrlShiftX(), registry.Active);
    }

    [Fact]
    public void PendingTraditionalMode_WhileCommittedDirect_NeverRegistersTheCapture()
    {
        // Committed direct mode: even a pending traditional change (not
        // saved) cannot bring the traditional hotkey back — the mode that
        // is in force is the committed one.
        var registry = new FakeCaptureRegistry();
        var coordinator = New(registry);
        var committed = Settings(true, capture: CtrlShiftX());

        coordinator.Start(committed);
        coordinator.ApplyPendingMode(committed, pendingDirectMode: false);

        Assert.Null(registry.Active);
        Assert.Empty(registry.Calls);
    }

    // ------------------------------------------------------------------
    // 3) Closing the settings window
    // ------------------------------------------------------------------

    [Fact]
    public void CancelingTheWindow_RestoresTheRegistration_TheCommittedModePrescribes()
    {
        // Cancel (close without saving): the registration goes back to the
        // state the committed mode prescribes — traditional mode leaves
        // the capture hotkey registered, exactly as before the window
        // opened.
        var registry = new FakeCaptureRegistry();
        var coordinator = New(registry);
        var committed = Settings(false, capture: CtrlShiftX());

        coordinator.Start(committed);
        coordinator.ApplyPendingMode(committed, pendingDirectMode: true);
        Assert.Null(registry.Active);

        coordinator.SyncWithCommittedMode(committed);

        Assert.Equal(CtrlShiftX(), registry.Active);
    }

    [Fact]
    public void ClosingInCommittedDirectMode_LeavesTheCaptureUnregistered()
    {
        var registry = new FakeCaptureRegistry();
        var coordinator = New(registry);
        var committed = Settings(true, capture: CtrlShiftX());

        coordinator.Start(committed);

        coordinator.SyncWithCommittedMode(committed);

        Assert.Null(registry.Active);
    }

    [Fact]
    public void ClosingTheWindow_WithoutAnyPendingChange_IsANoOp()
    {
        // No mode change was pending: closing the window must not touch
        // the active registration at all (re-registering it could only
        // lose it).
        var registry = new FakeCaptureRegistry();
        var coordinator = New(registry);
        var committed = Settings(false, capture: CtrlShiftX());

        coordinator.Start(committed);
        var callsBefore = registry.Calls.Count;

        coordinator.SyncWithCommittedMode(committed);

        Assert.Equal(CtrlShiftX(), registry.Active);
        Assert.Equal(callsBefore, registry.Calls.Count);
    }

    // ------------------------------------------------------------------
    // 4) Saving a mode change (the transaction)
    // ------------------------------------------------------------------

    [Fact]
    public void SavingDirectMode_LeavesOnlyTheConfiguredDirectSlotsRegistered()
    {
        // Before: traditional mode, the capture hotkey registered with the
        // saved combination, no direct slot registered. Save: direct mode
        // on, one slot configured with the traditional combination (now
        // free), the other two unset.
        var captureRegistry = new FakeCaptureRegistry();
        var directRegistry = new FakeDirectRegistry();
        var coordinator = New(captureRegistry);
        var committed = Settings(false, capture: CtrlShiftX());
        var pending = Settings(true, capture: CtrlShiftX(), copyImage: CtrlShiftX());

        coordinator.Start(committed);
        var beforeCapture = coordinator.Active;
        var beforeDirect = new DirectHotkeyState();

        var captureOk = coordinator.ApplyPlan(committed, pending);
        var direct = DirectHotkeyApplier.Apply(
            pending,
            beforeDirect,
            directRegistry.Register,
            directRegistry.Unregister);

        Assert.True(captureOk);
        Assert.True(direct.Succeeded);

        // The traditional capture is off for the whole session from now
        // on; only the configured direct slot is registered.
        Assert.Null(captureRegistry.Active);
        Assert.Equal(new[] { HotkeySlots.DirectCopyImage }, directRegistry.Active.Keys);
        Assert.Equal(CtrlShiftX(), directRegistry.Active[HotkeySlots.DirectCopyImage]);

        // The saved preference survived the whole transaction.
        Assert.Equal(CtrlShiftX(), pending.CaptureHotkey);
    }

    [Fact]
    public void SavingTraditionalMode_UnregistersTheDirectSlots_RestoresTheCapture()
    {
        // Before: direct mode committed, one slot registered, the capture
        // hotkey off. Save: direct mode off. The direct slots go off and
        // the saved traditional combination is registered again.
        var captureRegistry = new FakeCaptureRegistry();
        var directRegistry = new FakeDirectRegistry();
        var coordinator = New(captureRegistry);
        var committed = Settings(true, capture: CtrlShiftX(), copyImage: CtrlA());
        var pending = Settings(false, capture: CtrlShiftX(), copyImage: CtrlA());

        coordinator.Start(committed);
        directRegistry.Register(HotkeySlots.DirectCopyImage, CtrlA());
        var beforeCapture = coordinator.Active;
        var beforeDirect = Snapshot(directRegistry);

        var captureOk = coordinator.ApplyPlan(committed, pending);
        var direct = DirectHotkeyApplier.Apply(
            pending,
            beforeDirect,
            directRegistry.Register,
            directRegistry.Unregister);

        Assert.True(captureOk);
        Assert.True(direct.Succeeded);

        Assert.Equal(CtrlShiftX(), captureRegistry.Active);
        Assert.Empty(directRegistry.Active);
    }

    // ------------------------------------------------------------------
    // 5) A failed transaction never leaves a partial state behind
    // ------------------------------------------------------------------

    [Fact]
    public void FailedTransactionInDirectMode_TheRecoveryChainCannotReactivateTheCapture()
    {
        // Committed direct mode (capture off, one slot registered). A
        // change that adds a second slot which cannot be registered (taken
        // by another application) fails: the rollback restores the direct
        // slots and — the invariant of the mode — the recovery chain never
        // brings the traditional capture back.
        var captureRegistry = new FakeCaptureRegistry();
        var directRegistry = new FakeDirectRegistry
        {
            FailRegistration = (_, combo) => combo.Equals(CtrlB())
        };
        var coordinator = New(captureRegistry);
        var committed = Settings(true, capture: CtrlShiftX(), copyImage: CtrlA());
        var pending = Settings(true, capture: CtrlShiftX(), copyImage: CtrlA(), copyText: CtrlB());

        coordinator.Start(committed);
        directRegistry.Register(HotkeySlots.DirectCopyImage, CtrlA());
        var beforeCapture = coordinator.Active;
        Assert.Null(beforeCapture);
        var beforeDirect = Snapshot(directRegistry);

        var captureOk = coordinator.ApplyPlan(committed, pending);
        var direct = DirectHotkeyApplier.Apply(
            pending,
            beforeDirect,
            directRegistry.Register,
            directRegistry.Unregister);

        Assert.True(captureOk);
        Assert.False(direct.Succeeded);

        // The rollback, exactly as the application performs it.
        DirectHotkeyApplier.Restore(beforeDirect, directRegistry.Register, directRegistry.Unregister);
        coordinator.Restore(beforeCapture);

        // The traditional capture is still unregistered: a registration
        // failure can never reactivate it while the mode is on.
        Assert.Null(captureRegistry.Active);
        // The slot that registered before the failed attempt is restored.
        Assert.Equal(CtrlA(), directRegistry.Active[HotkeySlots.DirectCopyImage]);
        Assert.False(directRegistry.Active.ContainsKey(HotkeySlots.DirectCopyText));
    }

    [Fact]
    public void FailedTransactionInTraditionalMode_RestoresThePreviousCaptureCombination()
    {
        // Committed traditional mode (capture registered with the saved
        // combination). A change to a combination another application owns
        // fails: the recovery chain keeps the previous combination and the
        // rollback is a no-op — the app is exactly where it was.
        var registry = new FakeCaptureRegistry
        {
            FailRegistration = combo => combo.Equals(CtrlA())
        };
        var coordinator = New(registry);
        var committed = Settings(false, capture: CtrlShiftX());
        var pending = Settings(false, capture: CtrlA());

        coordinator.Start(committed);
        var beforeCapture = coordinator.Active;

        var captureOk = coordinator.ApplyPlan(committed, pending);

        Assert.False(captureOk);
        Assert.Equal(CtrlShiftX(), registry.Active);

        coordinator.Restore(beforeCapture);

        Assert.Equal(CtrlShiftX(), registry.Active);
    }

    [Fact]
    public void Suspend_ReleasesARegistrationTheShadowStateDidNotKnowAbout()
    {
        // A leaked registration (the OS has the hotkey, the shadow state
        // says it does not) can never survive a mode transition: the
        // suspend releases the OS registration unconditionally.
        var registry = new FakeCaptureRegistry();
        var coordinator = New(registry);
        var committed = Settings(true, capture: CtrlShiftX());

        coordinator.Start(committed);
        Assert.Null(coordinator.Active);

        // The leak: the OS registration exists without the shadow state.
        registry.Active = CtrlShiftX();

        coordinator.SyncWithCommittedMode(committed);

        Assert.Null(registry.Active);
        Assert.Null(coordinator.Active);
    }

    // ------------------------------------------------------------------
    // 6) The release phase: the old regime gives up its combinations
    //    before the new regime registers (shared combinations between the
    //    mutually exclusive modes)
    // ------------------------------------------------------------------

    [Fact]
    public void DirectToTraditional_SharedCombination_ReleasedFirst_TheCaptureRegisters()
    {
        // The reported defect, fixed order: the direct slot that is about
        // to go off releases its combination before the traditional
        // capture hotkey — which carries the very same combination — is
        // registered, so the OS accepts the registration and the switch
        // commits with only the destination mode's registration active.
        var shared = CtrlShiftX();
        var os = new SharedCombinationRegistry();
        var coordinator = new CaptureHotkeyCoordinator(os.TryRegisterCapture, os.UnregisterCapture);

        var committed = Settings(true, capture: shared, saveImage: shared);
        var pending = Settings(false, capture: shared, saveImage: shared);

        coordinator.Start(committed); // direct mode: registers nothing
        os.RegisterDirect(HotkeySlots.DirectSaveImage, shared);
        var beforeDirect = Snapshot(os);
        var beforeCapture = coordinator.Active;
        Assert.Null(beforeCapture);

        // The transaction, in the order the application runs it: release
        // the direct slots the destination does not want, then the
        // capture side, then the remaining direct side.
        DirectHotkeyApplier.Release(pending, beforeDirect, os.UnregisterDirect);
        var captureOk = coordinator.ApplyPlan(committed, pending);
        var direct = DirectHotkeyApplier.Apply(
            pending, beforeDirect, os.RegisterDirect, os.UnregisterDirect);

        Assert.True(captureOk);
        Assert.True(direct.Succeeded);
        // Only the destination mode's registration remains: the capture
        // hotkey on the shared combination, every direct slot off.
        Assert.Equal(shared, os.CaptureActive);
        Assert.Empty(os.DirectActive);
    }

    [Fact]
    public void DirectToTraditional_SharedCombination_RegisteringBeforeReleasing_Fails()
    {
        // Control for the reported defect: with the old order (the capture
        // hotkey registered before the direct slot is released), the
        // shared combination is still held by the process's own direct
        // slot when RegisterHotKey runs, so the OS refuses it and the
        // transaction would roll back — the exact false failure the user
        // saw ("outro aplicativo", when it was the process's own slot).
        var shared = CtrlShiftX();
        var os = new SharedCombinationRegistry();
        var coordinator = new CaptureHotkeyCoordinator(os.TryRegisterCapture, os.UnregisterCapture);

        var committed = Settings(true, capture: shared, saveImage: shared);
        var pending = Settings(false, capture: shared, saveImage: shared);

        coordinator.Start(committed);
        os.RegisterDirect(HotkeySlots.DirectSaveImage, shared);

        // The old order: no release phase before the capture side.
        var captureOk = coordinator.ApplyPlan(committed, pending);

        Assert.False(captureOk);
        Assert.Null(os.CaptureActive);
        // The direct slot still holds the combination: nothing was
        // released, and the rollback would restore exactly this state.
        Assert.Equal(shared, os.DirectActive[HotkeySlots.DirectSaveImage]);
    }

    [Fact]
    public void TraditionalToDirect_SharedCombination_TheCaptureIsSuspendedBeforeTheDirectSlotRegisters()
    {
        // The mirror transition: the capture hotkey is suspended by the
        // capture side before the direct side registers the same
        // combination, so the slot's registration cannot collide with the
        // process's own capture registration.
        var shared = CtrlShiftX();
        var os = new SharedCombinationRegistry();
        var coordinator = new CaptureHotkeyCoordinator(os.TryRegisterCapture, os.UnregisterCapture);

        var committed = Settings(false, capture: shared, saveImage: shared);
        var pending = Settings(true, capture: shared, saveImage: shared);

        coordinator.Start(committed);
        Assert.Equal(shared, os.CaptureActive);
        var beforeDirect = Snapshot(os);

        DirectHotkeyApplier.Release(pending, beforeDirect, os.UnregisterDirect);
        var captureOk = coordinator.ApplyPlan(committed, pending);
        var direct = DirectHotkeyApplier.Apply(
            pending, beforeDirect, os.RegisterDirect, os.UnregisterDirect);

        Assert.True(captureOk);
        Assert.True(direct.Succeeded);
        Assert.Null(os.CaptureActive);
        Assert.Equal(shared, os.DirectActive[HotkeySlots.DirectSaveImage]);
    }

    [Fact]
    public void DirectModeActive_ChangingTheTraditionalPreference_NeverAttemptsARegistration()
    {
        // The traditional preference can be edited and saved while the
        // direct mode stays on: the capture side of the transaction is an
        // unregister (a no-op while suspended), so the dormant preference
        // never competes for a global registration.
        var os = new SharedCombinationRegistry();
        var coordinator = new CaptureHotkeyCoordinator(os.TryRegisterCapture, os.UnregisterCapture);

        var committed = Settings(true, capture: CtrlShiftX(), copyImage: CtrlA());
        var pending = Settings(true, capture: CtrlB(), copyImage: CtrlA());

        coordinator.Start(committed);
        os.RegisterDirect(HotkeySlots.DirectCopyImage, CtrlA());

        var callsBefore = os.Calls.Count;
        var captureOk = coordinator.ApplyPlan(committed, pending);

        Assert.True(captureOk);
        // The only operational move is the (no-op) suspend: no
        // registration attempt of any kind for the dormant preference.
        Assert.Equal(new[] { "capture:off" }, os.Calls.Skip(callsBefore).ToArray());
        Assert.Null(os.CaptureActive);
        // The live direct slot is untouched.
        Assert.Equal(CtrlA(), os.DirectActive[HotkeySlots.DirectCopyImage]);
    }

    [Fact]
    public void DirectSlotHoldingTheSharedCombination_DoesNotBlockTheTraditionalPreference()
    {
        // While a direct slot is registered on the combination the
        // traditional preference carries, editing and saving that
        // preference (direct mode stays on) must succeed: the slot keeps
        // its registration and the preference is saved without occupying
        // any global registration.
        var shared = CtrlShiftX();
        var os = new SharedCombinationRegistry();
        var coordinator = new CaptureHotkeyCoordinator(os.TryRegisterCapture, os.UnregisterCapture);

        var committed = Settings(true, capture: shared, copyImage: shared);
        var pending = Settings(true, capture: CtrlB(), copyImage: shared);

        coordinator.Start(committed);
        os.RegisterDirect(HotkeySlots.DirectCopyImage, shared);
        var beforeDirect = Snapshot(os);

        DirectHotkeyApplier.Release(pending, beforeDirect, os.UnregisterDirect);
        var captureOk = coordinator.ApplyPlan(committed, pending);
        var direct = DirectHotkeyApplier.Apply(
            pending, beforeDirect, os.RegisterDirect, os.UnregisterDirect);

        Assert.True(captureOk);
        Assert.True(direct.Succeeded);
        Assert.Equal(shared, os.DirectActive[HotkeySlots.DirectCopyImage]);
        Assert.Null(os.CaptureActive);
    }

    [Fact]
    public void DirectToTraditional_TheCaptureCombinationIsExternallyOwned_TheRollbackRestoresThePreviousState()
    {
        // A real registration failure (another application owns the
        // combination the traditional mode wants) is not masked by the
        // release phase: the switch fails, and the rollback restores the
        // state the direct mode had — the slots registered, the capture
        // hotkey off. The preference change is not persisted by the
        // application in this case (the transaction was rejected).
        var shared = CtrlShiftX(); // also HotkeySetting.Default
        var os = new SharedCombinationRegistry { ExternalOwners = { shared } };
        var coordinator = new CaptureHotkeyCoordinator(os.TryRegisterCapture, os.UnregisterCapture);

        var committed = Settings(true, capture: shared, copyImage: CtrlA());
        var pending = Settings(false, capture: shared, copyImage: CtrlA());

        coordinator.Start(committed);
        os.RegisterDirect(HotkeySlots.DirectCopyImage, CtrlA());
        var beforeDirect = Snapshot(os);
        var beforeCapture = coordinator.Active;

        DirectHotkeyApplier.Release(pending, beforeDirect, os.UnregisterDirect);
        var captureOk = coordinator.ApplyPlan(committed, pending);
        var direct = DirectHotkeyApplier.Apply(
            pending, beforeDirect, os.RegisterDirect, os.UnregisterDirect);

        Assert.False(captureOk);
        Assert.True(direct.Succeeded);

        // The rollback, exactly as the application performs it.
        DirectHotkeyApplier.Restore(beforeDirect, os.RegisterDirect, os.UnregisterDirect);
        coordinator.Restore(beforeCapture);

        // The direct mode's state is back: the slot registered on its
        // combination, the capture hotkey unregistered.
        Assert.Equal(CtrlA(), os.DirectActive[HotkeySlots.DirectCopyImage]);
        Assert.Null(os.CaptureActive);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static CaptureHotkeyCoordinator New(FakeCaptureRegistry registry)
        => new(registry.TryRegister, registry.Unregister);

    private static DirectHotkeyState Snapshot(FakeDirectRegistry registry)
    {
        var state = new DirectHotkeyState();
        foreach (var (slot, combination) in registry.Active)
        {
            state[slot] = new DirectSlotState(true, combination);
        }

        return state;
    }

    private static DirectHotkeyState Snapshot(SharedCombinationRegistry registry)
    {
        var state = new DirectHotkeyState();
        foreach (var (slot, combination) in registry.DirectActive)
        {
            state[slot] = new DirectSlotState(true, combination);
        }

        return state;
    }

    /// <summary>
    /// The fake OS registration of the traditional capture hotkey: the
    /// combination it is registered with (or null) and the call log, the
    /// pair of registrations the application keeps mirrored in its shadow
    /// state.
    /// </summary>
    private sealed class FakeCaptureRegistry
    {
        public HotkeySetting? Active { get; set; }
        public List<string> Calls { get; } = new();
        public Func<HotkeySetting, bool>? FailRegistration { get; set; }

        public bool TryRegister(HotkeySetting combination)
        {
            Calls.Add($"register:{combination}");
            if (FailRegistration?.Invoke(combination) == true)
            {
                return false;
            }

            Active = combination;
            return true;
        }

        public void Unregister()
        {
            Calls.Add("unregister");
            Active = null;
        }
    }

    /// <summary>
    /// The fake OS registration of the direct-mode slots, the same shape
    /// the applier's tests use.
    /// </summary>
    private sealed class FakeDirectRegistry
    {
        public Dictionary<int, HotkeySetting> Active { get; } = new();
        public Func<int, HotkeySetting, bool>? FailRegistration { get; set; }

        public bool Register(int slot, HotkeySetting combination)
        {
            if (FailRegistration?.Invoke(slot, combination) == true)
            {
                return false;
            }

            Active[slot] = combination;
            return true;
        }

        public void Unregister(int slot) => Active.Remove(slot);
    }

    /// <summary>
    /// A fake OS that models the in-process combination space shared by
    /// the capture hotkey and the direct slots: a combination can be held
    /// by at most one live registration at a time, whatever its id —
    /// exactly what RegisterHotKey enforces, a second id asking for a
    /// combination any id of the process already holds is refused — plus
    /// combinations owned by external applications. This is what makes
    /// the ordering of the transaction (release before register) visible
    /// to the tests without Windows.
    /// </summary>
    private sealed class SharedCombinationRegistry
    {
        public Dictionary<HotkeySetting, int> Owners { get; } = new();
        public HashSet<HotkeySetting> ExternalOwners { get; } = new();

        public HotkeySetting? CaptureActive { get; private set; }
        public Dictionary<int, HotkeySetting> DirectActive { get; } = new();
        public List<string> Calls { get; } = new();

        public bool TryRegisterCapture(HotkeySetting combo)
        {
            Calls.Add($"capture:{combo}");
            if (ExternalOwners.Contains(combo) || Owners.ContainsKey(combo))
            {
                return false;
            }

            CaptureActive = combo;
            Owners[combo] = HotkeySlots.Capture;
            return true;
        }

        public void UnregisterCapture()
        {
            Calls.Add("capture:off");
            if (CaptureActive is { } combo && Owners.TryGetValue(combo, out var owner)
                && owner == HotkeySlots.Capture)
            {
                Owners.Remove(combo);
            }

            CaptureActive = null;
        }

        public bool RegisterDirect(int slot, HotkeySetting combo)
        {
            Calls.Add($"direct:{slot}:{combo}");
            if (ExternalOwners.Contains(combo) || Owners.ContainsKey(combo))
            {
                return false;
            }

            DirectActive[slot] = combo;
            Owners[combo] = slot;
            return true;
        }

        public void UnregisterDirect(int slot)
        {
            Calls.Add($"direct:{slot}:off");
            if (DirectActive.TryGetValue(slot, out var combo))
            {
                Owners.Remove(combo);
                DirectActive.Remove(slot);
            }
        }
    }
}
