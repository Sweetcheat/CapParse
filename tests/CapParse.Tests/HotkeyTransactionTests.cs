using CapParse.Configuration;
using CapParse.Platform;

namespace CapParse.Tests;

// The mode-aware decisions of the hotkey side of a settings transaction:
// what the startup and each mode transition must do to the traditional
// capture hotkey, and which combinations may conflict given the active
// mode. Pure logic over the settings values — no Windows API is involved,
// so the lifecycle (traditional mode keeps the capture hotkey, direct
// mode unregisters it without deleting the saved combination, the
// recovery chain can never re-register it while the mode is on) is
// verified deterministically.
public class HotkeyTransactionTests
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
    // 1) Traditional mode keeps the capture hotkey
    // ------------------------------------------------------------------

    [Fact]
    public void Startup_TraditionalMode_KeepsTheCaptureHotkey()
    {
        Assert.True(HotkeyTransaction.RegisterCaptureAtStartup(Settings(false)));
    }

    [Fact]
    public void TraditionalMode_UnchangedCombination_LeavesTheCaptureHotkeyUntouched()
    {
        // The registration is already active with the saved combination:
        // re-registering it could only lose it.
        var current = Settings(false, capture: CtrlShiftX());
        var pending = Settings(false, capture: CtrlShiftX());

        Assert.Equal(CaptureHotkeyPlan.Keep, HotkeyTransaction.PlanCapture(current, pending));
    }

    [Fact]
    public void TraditionalMode_ChangedCombination_RegistersIt()
    {
        var current = Settings(false, capture: CtrlShiftX());
        var pending = Settings(false, capture: CtrlA());

        var plan = HotkeyTransaction.PlanCapture(current, pending);

        Assert.Equal(CaptureHotkeyPlan.Action.Register, plan.Kind);
        Assert.Equal(CtrlA(), plan.Combination);
    }

    // ------------------------------------------------------------------
    // 2) Direct mode unregisters the capture hotkey, keeps the preference
    // ------------------------------------------------------------------

    [Fact]
    public void DirectMode_UnregistersTheCaptureHotkey_KeepsTheSavedCombination()
    {
        var current = Settings(false, capture: CtrlShiftX());
        var pending = Settings(true, capture: CtrlShiftX());

        var plan = HotkeyTransaction.PlanCapture(current, pending);

        Assert.Equal(CaptureHotkeyPlan.Action.Unregister, plan.Kind);

        // Unregistering the hotkey never deletes the stored preference:
        // the pending (and, on commit, the saved) settings keep it.
        Assert.Equal(CtrlShiftX(), pending.CaptureHotkey);
    }

    [Fact]
    public void DirectMode_AlreadyActive_KeepsTheCaptureHotkeyUnregistered()
    {
        // A transaction that stays in direct mode (for example changing a
        // direct slot) must not bring the capture hotkey back: the plan
        // is an unregister, never a register, so the recovery chain has
        // nothing to register.
        var current = Settings(true, capture: CtrlShiftX());
        var pending = Settings(true, capture: CtrlShiftX(), copyImage: CtrlA());

        var plan = HotkeyTransaction.PlanCapture(current, pending);

        Assert.Equal(CaptureHotkeyPlan.Action.Unregister, plan.Kind);
    }

    // ------------------------------------------------------------------
    // 4) Startup in direct mode never registers the traditional hotkey
    // ------------------------------------------------------------------

    [Fact]
    public void Startup_DirectMode_DoesNotRegisterTheCaptureHotkey()
    {
        // A launch that is already configured for direct mode registers
        // only the direct slots; the traditional shortcut stays off
        // (its saved combination is free for a direct slot).
        Assert.False(HotkeyTransaction.RegisterCaptureAtStartup(Settings(true, capture: CtrlShiftX())));
    }

    // ------------------------------------------------------------------
    // 5) Disabling direct mode restores the traditional hotkey
    // ------------------------------------------------------------------

    [Fact]
    public void DisablingDirectMode_RegistersTheSavedCaptureCombination()
    {
        var current = Settings(true, capture: CtrlShiftX(), copyImage: CtrlA());
        var pending = Settings(false, capture: CtrlShiftX(), copyImage: CtrlA());

        var plan = HotkeyTransaction.PlanCapture(current, pending);

        Assert.Equal(CaptureHotkeyPlan.Action.Register, plan.Kind);
        Assert.Equal(CtrlShiftX(), plan.Combination);
    }

    [Fact]
    public void DisablingDirectMode_UnregistersTheDirectSlots_ThroughTheApplier()
    {
        // The other half of the transition: the direct slots go off. The
        // applier (already covered on its own) unregisters every slot
        // when the mode is disabled; this pins the two halves together in
        // one transactional shape.
        var pending = Settings(false, copyImage: CtrlA());
        var before = new DirectHotkeyState();
        before[HotkeySlots.DirectCopyImage] = new DirectSlotState(true, CtrlA());

        var calls = new List<string>();
        var result = DirectHotkeyApplier.Apply(
            pending,
            before,
            (slot, combination) => { calls.Add($"register:{slot}"); return true; },
            slot => calls.Add($"unregister:{slot}"));

        Assert.True(result.Succeeded);
        Assert.Equal(new[] { "unregister:2" }, calls);
        Assert.False(result.After[HotkeySlots.DirectCopyImage].Registered);
    }

    // ------------------------------------------------------------------
    // 3) The saved traditional combination is free for a direct slot
    // ------------------------------------------------------------------

    [Fact]
    public void DirectMode_TheSavedCaptureCombination_DoesNotBlockADirectSlot()
    {
        // While direct mode is active the traditional hotkey is not
        // registered, so its saved combination (Ctrl + Shift + X, the
        // default) can be assigned to one of the direct slots.
        var settings = Settings(
            true,
            capture: CtrlShiftX(),
            copyImage: CtrlShiftX());

        Assert.Empty(HotkeyTransaction.FindConflicts(settings));
    }

    [Fact]
    public void DirectMode_TheSameCombinationOnTwoDirectSlots_StillConflicts()
    {
        // Freeing the capture slot never loosens the direct slots'
        // mutual exclusivity: two direct actions can never share a
        // combination.
        var settings = Settings(true, copyImage: CtrlShiftX(), copyText: CtrlShiftX());

        var conflicts = HotkeyTransaction.FindConflicts(settings);

        Assert.Single(conflicts);
        Assert.Equal(CtrlShiftX(), conflicts[0]);
    }

    [Fact]
    public void TraditionalMode_TheCaptureCombination_DoesNotBlockADirectSlot()
    {
        // In traditional mode the direct slots are not registered, so a
        // saved direct combination may be identical to the capture
        // hotkey's: the modes are mutually exclusive and never active at
        // the same time, so there is no conflict to report and switching
        // back to the traditional mode is never blocked by it.
        var settings = Settings(false, capture: CtrlShiftX(), copyImage: CtrlShiftX());

        Assert.Empty(HotkeyTransaction.FindConflicts(settings));
    }

    [Fact]
    public void TraditionalMode_EveryDirectSlotMayShareTheCaptureCombination()
    {
        // All three direct slots can carry the capture combination: in
        // traditional mode only the capture hotkey is registered, so the
        // saved direct slots cannot conflict with it — no matter how many
        // of them repeat the combination.
        var settings = Settings(
            false,
            capture: CtrlShiftX(),
            copyImage: CtrlShiftX(),
            copyText: CtrlShiftX(),
            saveImage: CtrlShiftX());

        Assert.Empty(HotkeyTransaction.FindConflicts(settings));
    }

    // ------------------------------------------------------------------
    // Mode switches with a combination shared by both families
    // ------------------------------------------------------------------

    [Fact]
    public void TraditionalToDirect_SwitchWithSharedCombination_IsAllowed()
    {
        // The reported scenario, first half: the traditional mode is
        // committed with the capture hotkey on Ctrl + Shift + X, and a
        // direct slot carries the same combination. Enabling the direct
        // mode must not be rejected as a conflict: the capture hotkey is
        // unregistered by the switch, so the shared combination ends on
        // exactly one active registration.
        var current = Settings(false, capture: CtrlShiftX(), saveImage: CtrlShiftX());
        var pending = Settings(true, capture: CtrlShiftX(), saveImage: CtrlShiftX());

        Assert.Empty(HotkeyTransaction.FindConflicts(pending));
        Assert.Equal(
            CaptureHotkeyPlan.Action.Unregister,
            HotkeyTransaction.PlanCapture(current, pending).Kind);
    }

    [Fact]
    public void DirectToTraditional_SwitchWithSharedCombination_IsAllowed()
    {
        // The reported scenario, second half: the direct mode is committed
        // with a slot on Ctrl + Shift + X, the same combination the
        // capture hotkey carries. Disabling the direct mode must not be
        // rejected as a conflict — the direct slots go off and the saved
        // traditional combination becomes active again, exactly as saved.
        var current = Settings(true, capture: CtrlShiftX(), saveImage: CtrlShiftX());
        var pending = Settings(false, capture: CtrlShiftX(), saveImage: CtrlShiftX());

        Assert.Empty(HotkeyTransaction.FindConflicts(pending));
        var plan = HotkeyTransaction.PlanCapture(current, pending);
        Assert.Equal(CaptureHotkeyPlan.Action.Register, plan.Kind);
        Assert.Equal(CtrlShiftX(), plan.Combination);
    }

    [Fact]
    public void DirectToTraditional_SwitchWithSharedCombination_LeavesOnlyTheCaptureActive()
    {
        // The reported defect, end to end at the transaction level: the
        // committed direct mode carries a slot on the very combination the
        // capture hotkey is saved with. Saving the traditional mode must
        // pass validation (the modes are mutually exclusive), and the
        // transaction must end with only the destination mode's
        // registration active — the capture hotkey on the shared
        // combination, every direct slot unregistered.
        var shared = CtrlShiftX();
        var hotkeys = new FakeSlotRegistry();

        var current = Settings(true, capture: shared, saveImage: shared);
        var pending = Settings(false, capture: shared, saveImage: shared);

        Assert.Empty(HotkeyTransaction.FindConflicts(pending));

        // The committed operational state: the slot registered on the
        // shared combination, the capture hotkey unregistered.
        var beforeDirect = new DirectHotkeyState();
        beforeDirect[HotkeySlots.DirectSaveImage] = new DirectSlotState(true, shared);
        var beforeCapture = (HotkeySetting?)null;

        // The transaction, in the order the application runs it: the
        // capture side through the recovery chain, then the direct side.
        var plan = HotkeyTransaction.PlanCapture(current, pending);
        Assert.Equal(CaptureHotkeyPlan.Action.Register, plan.Kind);
        var captureActive = HotkeyRecovery.Resolve(
            beforeCapture, plan.Combination!, hotkeys.TryRegisterCapture);
        var direct = DirectHotkeyApplier.Apply(
            pending, beforeDirect, hotkeys.Register, hotkeys.Unregister);

        Assert.True(direct.Succeeded);
        Assert.Equal(shared, captureActive);
        Assert.Equal(shared, hotkeys.Capture);
        Assert.Empty(hotkeys.Direct);
    }

    [Fact]
    public void TraditionalToDirect_SwitchWithSharedCombination_LeavesOnlyTheDirectSlotsActive()
    {
        // The mirror of the previous transaction: the committed traditional
        // mode has the capture hotkey registered on the shared combination
        // and no direct slot. Saving the direct mode must pass validation
        // and end with only the destination mode's registrations active —
        // the capture hotkey unregistered, the configured slot on the
        // shared combination.
        var shared = CtrlShiftX();
        var hotkeys = new FakeSlotRegistry { Capture = shared };

        var current = Settings(false, capture: shared, saveImage: shared);
        var pending = Settings(true, capture: shared, saveImage: shared);

        Assert.Empty(HotkeyTransaction.FindConflicts(pending));

        var beforeDirect = new DirectHotkeyState();
        var beforeCapture = hotkeys.Capture;
        Assert.Equal(shared, beforeCapture);

        var plan = HotkeyTransaction.PlanCapture(current, pending);
        Assert.Equal(CaptureHotkeyPlan.Action.Unregister, plan.Kind);
        hotkeys.UnregisterCapture();

        var direct = DirectHotkeyApplier.Apply(
            pending, beforeDirect, hotkeys.Register, hotkeys.Unregister);

        Assert.True(direct.Succeeded);
        Assert.Null(hotkeys.Capture);
        Assert.Equal(shared, hotkeys.Direct[HotkeySlots.DirectSaveImage]);
        Assert.Single(hotkeys.Direct);
    }

    [Fact]
    public void SharedCombination_WhoseRegistrationFails_IsAnOperationalFailure_NotAConflict()
    {
        // The shared combination is taken by another application: the
        // validation must not report a conflict between the modes (it is
        // a logical check, before any registration), and the failed OS
        // registration is an operational failure — reported as such, with
        // the previous operational state restored by the rollback.
        var shared = CtrlShiftX();
        var hotkeys = new FakeSlotRegistry
        {
            FailRegistration = (_, combo) => combo.Equals(shared)
        };

        var current = Settings(true, capture: shared, copyImage: CtrlA());
        var pending = Settings(true, capture: shared, copyImage: CtrlA(), saveImage: shared);

        // No logical conflict: inside the direct mode the capture
        // combination does not participate in the check, whatever the OS
        // says about it.
        Assert.Empty(HotkeyTransaction.FindConflicts(pending));

        var beforeDirect = new DirectHotkeyState();
        beforeDirect[HotkeySlots.DirectCopyImage] = new DirectSlotState(true, CtrlA());

        var direct = DirectHotkeyApplier.Apply(
            pending, beforeDirect, hotkeys.Register, hotkeys.Unregister);

        // The OS refused the shared combination (an external owner owns
        // it): an operational failure, not a mode conflict.
        Assert.False(direct.Succeeded);
        Assert.NotNull(direct.FailureMessage);

        // The rollback, exactly as the application performs it on failure.
        DirectHotkeyApplier.Restore(beforeDirect, hotkeys.Register, hotkeys.Unregister);

        // The previous operational state is back: the working slot is
        // registered, the refused one is off.
        Assert.Equal(CtrlA(), hotkeys.Direct[HotkeySlots.DirectCopyImage]);
        Assert.False(hotkeys.Direct.ContainsKey(HotkeySlots.DirectSaveImage));
    }

    // ------------------------------------------------------------------
    // The traditional capture's callback guard
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(false, false, true)]  // traditional in force: the press captures
    [InlineData(true, false, false)]  // direct mode committed: never
    [InlineData(false, true, false)]  // pending direct mode in the open settings window: never
    [InlineData(true, true, false)]   // both: never
    public void TraditionalCaptureAllowed_OnlyWhileTheTraditionalModeIsInForce(
        bool committedDirectMode,
        bool pendingDirectMode,
        bool expected)
    {
        // The callback of the traditional capture hotkey validates the
        // mode before starting a capture: a stale WM_HOTKEY queued before
        // the suspension, or a registration that should not exist, can
        // never start a traditional capture while the direct mode is in
        // force (in either form).
        Assert.Equal(
            expected,
            HotkeyTransaction.TraditionalCaptureAllowed(committedDirectMode, pendingDirectMode));
    }

    // ------------------------------------------------------------------
    // 6) A failed transaction leaves no partial registration behind
    // ------------------------------------------------------------------

    [Fact]
    public void FailedDirectRegistration_Rollback_RestoresCaptureAndDirectSlots()
    {
        // The transactional shape ApplySettings executes, end to end:
        // plan the capture side, apply the direct side, and on failure
        // restore both halves to the state they had before the attempt.
        //
        // Before: traditional mode — the capture hotkey is registered
        // with the saved combination, no direct slot is registered.
        // Transaction: enable direct mode, two slots configured, one of
        // them taken by another application (registration fails).
        var capture = CtrlShiftX();
        var beforeDirect = new DirectHotkeyState();

        var pending = Settings(true, capture: capture, copyImage: CtrlA(), copyText: CtrlB());

        // The capture side of the transaction: direct mode unregisters
        // the capture hotkey (it cannot fail).
        var capturePlan = HotkeyTransaction.PlanCapture(Settings(false, capture: capture), pending);
        Assert.Equal(CaptureHotkeyPlan.Action.Unregister, capturePlan.Kind);

        var hotkeys = new FakeSlotRegistry { Capture = capture };

        // The direct side: the first slot registers, the second fails.
        hotkeys.FailRegistration = (_, combo) => combo.Equals(CtrlB());
        var result = DirectHotkeyApplier.Apply(
            pending,
            beforeDirect,
            hotkeys.Register,
            hotkeys.Unregister);

        Assert.False(result.Succeeded);
        Assert.True(hotkeys.Direct.ContainsKey(HotkeySlots.DirectCopyImage));

        // The rollback, exactly as ApplySettings performs it: the direct
        // slots go back to `beforeDirect` (all off), and the capture
        // hotkey goes back to the state it had before the attempt
        // (registered with the saved combination) through the same
        // recovery chain the application uses.
        DirectHotkeyApplier.Restore(beforeDirect, hotkeys.Register, hotkeys.Unregister);
        var restoredCapture = HotkeyRecovery.Resolve(null, capture, hotkeys.TryRegisterCapture);

        // No partial registration survives: the app is exactly where it
        // was before the failed change.
        Assert.Equal(capture, hotkeys.Capture);
        Assert.Equal(capture, restoredCapture);
        Assert.Empty(hotkeys.Direct);
    }

    /// <summary>
    /// The operational hotkey state a rollback test runs against: the
    /// capture registration plus the direct slots, mirroring the pair of
    /// GlobalHotkey registrations the application keeps (one id each).
    /// </summary>
    private sealed class FakeSlotRegistry
    {
        public HotkeySetting? Capture { get; set; }
        public Dictionary<int, HotkeySetting> Direct { get; } = new();
        public Func<int, HotkeySetting, bool>? FailRegistration { get; set; }

        public bool TryRegisterCapture(HotkeySetting combo)
        {
            Capture = combo;
            return true;
        }

        public void UnregisterCapture() => Capture = null;

        public bool Register(int slot, HotkeySetting combo)
        {
            if (FailRegistration?.Invoke(slot, combo) == true)
            {
                return false;
            }

            Direct[slot] = combo;
            return true;
        }

        public void Unregister(int slot) => Direct.Remove(slot);
    }
}
