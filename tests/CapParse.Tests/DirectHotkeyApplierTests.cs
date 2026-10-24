using CapParse.Configuration;
using CapParse.Platform;

namespace CapParse.Tests;

// The transactional application of the direct-mode hotkey registrations,
// against fakes of the Win32 register/unregister calls: disabled mode
// unregisters every slot (the configured combinations are preserved by
// the caller in the settings), enabled mode registers only the configured
// combinations, a slot already on the desired combination is left
// untouched, and a failed registration leaves the previous state
// restorable through Restore, so a failed change never leaves a partial
// registration behind. No Windows API is involved.
public class DirectHotkeyApplierTests
{
    private static HotkeySetting CtrlA()
        => new((uint)GlobalHotkey.Modifier.Control, 0x41);

    private static HotkeySetting CtrlB()
        => new((uint)GlobalHotkey.Modifier.Control, 0x42);

    private static HotkeySetting AltF9()
        => new((uint)GlobalHotkey.Modifier.Alt, 0x78);

    /// <summary>
    /// A fake of the GlobalHotkey registrations: it records every
    /// register/unregister call and tracks, per slot, the combination the
    /// slot is currently registered with — the same contract the real
    /// Win32 wrapper keeps (a failed replacement leaves the slot exactly
    /// as it found it).
    /// </summary>
    private sealed class FakeHotkeys
    {
        public Dictionary<int, HotkeySetting> Registered { get; } = new();

        public List<(int Slot, HotkeySetting Combo)> RegisterCalls { get; } = new();

        public List<int> UnregisterCalls { get; } = new();

        public Func<int, HotkeySetting, bool>? ShouldFail { get; set; }

        public bool Register(int slot, HotkeySetting combo)
        {
            RegisterCalls.Add((slot, combo));
            if (ShouldFail?.Invoke(slot, combo) == true)
            {
                return false;
            }

            Registered[slot] = combo;
            return true;
        }

        /// <summary>
        /// Puts the fake into an initial operational state (the "before"
        /// of a transaction) without recording it as a call of the
        /// transaction under test.
        /// </summary>
        public void Seed(DirectHotkeyState state)
        {
            foreach (var slot in HotkeySlots.Direct)
            {
                if (state[slot] is { Registered: true, Combination: not null } seeded)
                {
                    Registered[slot] = seeded.Combination;
                }
            }
        }

        public void Unregister(int slot)
        {
            UnregisterCalls.Add(slot);
            Registered.Remove(slot);
        }

        public DirectHotkeyState State()
        {
            var state = new DirectHotkeyState();
            foreach (var slot in HotkeySlots.Direct)
            {
                state[slot] = Registered.TryGetValue(slot, out var combo)
                    ? new DirectSlotState(true, combo)
                    : DirectSlotState.Off;
            }

            return state;
        }
    }

    private static DirectHotkeyState Before(
        HotkeySetting? copyImage = null,
        HotkeySetting? copyText = null,
        HotkeySetting? saveImage = null)
    {
        var state = new DirectHotkeyState();
        state[HotkeySlots.DirectCopyImage] = copyImage is null
            ? DirectSlotState.Off
            : new DirectSlotState(true, copyImage);
        state[HotkeySlots.DirectCopyText] = copyText is null
            ? DirectSlotState.Off
            : new DirectSlotState(true, copyText);
        state[HotkeySlots.DirectSaveImage] = saveImage is null
            ? DirectSlotState.Off
            : new DirectSlotState(true, saveImage);
        return state;
    }

    private static AppSettings Desired(
        bool enabled,
        HotkeySetting? copyImage = null,
        HotkeySetting? copyText = null,
        HotkeySetting? saveImage = null)
    {
        var settings = new AppSettings { DirectModeEnabled = enabled };
        settings.SetDirectSlot(HotkeySlots.DirectCopyImage, copyImage);
        settings.SetDirectSlot(HotkeySlots.DirectCopyText, copyText);
        settings.SetDirectSlot(HotkeySlots.DirectSaveImage, saveImage);
        return settings;
    }

    [Fact]
    public void Disabled_UnregistersEverySlot_IgnoringTheConfiguredCombinations()
    {
        // Disabling the mode removes every direct registration, even when
        // combinations are configured: the mode controls the
        // registrations, never the stored combinations.
        var before = Before(CtrlA(), CtrlB(), AltF9());
        var desired = Desired(false, CtrlA(), CtrlB(), AltF9());
        var hotkeys = new FakeHotkeys();

        var result = DirectHotkeyApplier.Apply(desired, before, hotkeys.Register, hotkeys.Unregister);

        Assert.True(result.Succeeded);
        Assert.Null(result.FailureMessage);

        Assert.Equal(
            new[] { HotkeySlots.DirectCopyImage, HotkeySlots.DirectCopyText, HotkeySlots.DirectSaveImage },
            hotkeys.UnregisterCalls);
        Assert.Empty(hotkeys.RegisterCalls);

        Assert.False(result.After[HotkeySlots.DirectCopyImage].Registered);
        Assert.False(result.After[HotkeySlots.DirectCopyText].Registered);
        Assert.False(result.After[HotkeySlots.DirectSaveImage].Registered);
    }

    [Fact]
    public void Enabled_RegistersOnlyTheConfiguredCombinations()
    {
        // An unset slot (null) registers nothing: enabling the mode never
        // invents a combination the user did not configure.
        var desired = Desired(true, copyImage: CtrlA(), copyText: null, saveImage: AltF9());
        var hotkeys = new FakeHotkeys();

        var result = DirectHotkeyApplier.Apply(desired, Before(), hotkeys.Register, hotkeys.Unregister);

        Assert.True(result.Succeeded);

        Assert.Equal(
            new[]
            {
                (HotkeySlots.DirectCopyImage, CtrlA()),
                (HotkeySlots.DirectSaveImage, AltF9())
            },
            hotkeys.RegisterCalls);
        Assert.Empty(hotkeys.UnregisterCalls);

        Assert.Equal(CtrlA(), result.After[HotkeySlots.DirectCopyImage].Combination);
        Assert.False(result.After[HotkeySlots.DirectCopyText].Registered);
        Assert.Equal(AltF9(), result.After[HotkeySlots.DirectSaveImage].Combination);
    }

    [Fact]
    public void AllSlotsUnset_RegistersNothing()
    {
        var desired = Desired(true);
        var hotkeys = new FakeHotkeys();

        var result = DirectHotkeyApplier.Apply(desired, Before(), hotkeys.Register, hotkeys.Unregister);

        Assert.True(result.Succeeded);
        Assert.Empty(hotkeys.RegisterCalls);
        Assert.Empty(hotkeys.UnregisterCalls);
    }

    [Fact]
    public void SlotAlreadyOnTheDesiredCombination_IsLeftUntouched()
    {
        // Re-registering the same combination could only lose it (another
        // application may have taken it in the meantime), so the slot is
        // left exactly as it is.
        var before = Before(copyImage: CtrlA());
        var desired = Desired(true, copyImage: CtrlA());
        var hotkeys = new FakeHotkeys();

        var result = DirectHotkeyApplier.Apply(desired, before, hotkeys.Register, hotkeys.Unregister);

        Assert.True(result.Succeeded);
        Assert.Empty(hotkeys.RegisterCalls);
        Assert.Empty(hotkeys.UnregisterCalls);
        Assert.Equal(CtrlA(), result.After[HotkeySlots.DirectCopyImage].Combination);
    }

    [Fact]
    public void Disabled_KeepsTheConfiguredCombinationsInTheSettings()
    {
        // The applier never mutates the settings it is given: clearing the
        // registrations leaves every stored combination intact, so
        // re-enabling the mode restores the user's shortcuts.
        var desired = Desired(false, CtrlA(), AltF9());
        var hotkeys = new FakeHotkeys();

        DirectHotkeyApplier.Apply(desired, Before(CtrlA(), AltF9()), hotkeys.Register, hotkeys.Unregister);

        Assert.Equal(CtrlA(), desired.DirectSlot(HotkeySlots.DirectCopyImage));
        Assert.Equal(AltF9(), desired.DirectSlot(HotkeySlots.DirectCopyText));
        Assert.Null(desired.DirectSlot(HotkeySlots.DirectSaveImage));
    }

    [Fact]
    public void RegistrationFailure_ReportsTheFailedCombination_LeavesThePreviousStateRestorable()
    {
        // The requested combination is taken by another application: the
        // apply fails, the slot that was already registered keeps its
        // previous combination, and Restore brings every slot back to the
        // state it had before the attempt.
        var before = Before(copyImage: CtrlA());
        var desired = Desired(true, copyImage: CtrlB(), copyText: AltF9());
        var hotkeys = new FakeHotkeys();
        hotkeys.Seed(before);
        hotkeys.ShouldFail = (_, combo) => combo.Equals(CtrlB());

        var result = DirectHotkeyApplier.Apply(desired, before, hotkeys.Register, hotkeys.Unregister);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureMessage);
        Assert.Contains(CtrlB().DisplayString, result.FailureMessage);

        // The untouched slot keeps its previous combination...
        Assert.Equal(CtrlA(), hotkeys.Registered[HotkeySlots.DirectCopyImage]);
        // ...and the slot the failed attempt touched is left restorable.
        Assert.True(hotkeys.Registered.ContainsKey(HotkeySlots.DirectCopyText));

        DirectHotkeyApplier.Restore(before, hotkeys.Register, hotkeys.Unregister);

        Assert.Equal(before, hotkeys.State());
    }

    [Fact]
    public void RegistrationFailure_Rollback_UnregistersTheSlotsTheAttemptRegistered()
    {
        // Nothing was registered before the attempt: a partially applied
        // change must end with nothing registered, not with the slots the
        // attempt managed to register.
        var before = Before();
        var desired = Desired(true, copyImage: CtrlA(), copyText: CtrlB());
        var hotkeys = new FakeHotkeys();
        hotkeys.ShouldFail = (_, combo) => combo.Equals(CtrlB());

        var result = DirectHotkeyApplier.Apply(desired, before, hotkeys.Register, hotkeys.Unregister);

        Assert.False(result.Succeeded);
        // The first slot was registered by the attempt before the second
        // one failed.
        Assert.True(hotkeys.Registered.ContainsKey(HotkeySlots.DirectCopyImage));

        DirectHotkeyApplier.Restore(before, hotkeys.Register, hotkeys.Unregister);

        Assert.Empty(hotkeys.Registered);
        Assert.Equal(before, hotkeys.State());
    }

    [Fact]
    public void Restore_ForcesEverySlotToTheTargetState()
    {
        var target = Before(copyText: AltF9());
        var hotkeys = new FakeHotkeys();
        // The current state differs from the target in both directions:
        // one slot registered that the target does not want, one wanted
        // slot not registered.
        hotkeys.Register(HotkeySlots.DirectCopyImage, CtrlA());

        DirectHotkeyApplier.Restore(target, hotkeys.Register, hotkeys.Unregister);

        Assert.Equal(target, hotkeys.State());
        Assert.Contains(HotkeySlots.DirectCopyImage, hotkeys.UnregisterCalls);
        Assert.Contains((HotkeySlots.DirectCopyText, AltF9()), hotkeys.RegisterCalls);
    }

    [Fact]
    public void Apply_WithNoDifferences_ProducesNoCalls()
    {
        var before = Before(copyImage: CtrlA());
        var desired = Desired(true, copyImage: CtrlA());
        var hotkeys = new FakeHotkeys();

        var result = DirectHotkeyApplier.Apply(desired, before, hotkeys.Register, hotkeys.Unregister);

        Assert.True(result.Succeeded);
        Assert.Empty(hotkeys.RegisterCalls);
        Assert.Empty(hotkeys.UnregisterCalls);
    }

    // ------------------------------------------------------------------
    // The release phase (the transaction runs it before the capture side,
    // so a combination a direct slot is about to give up is free when the
    // capture hotkey — which may carry the same combination — registers)
    // ------------------------------------------------------------------

    [Fact]
    public void Release_ModeDisabled_UnregistersEveryRegisteredSlot()
    {
        // Switching to the traditional mode releases every direct
        // registration up front, so the combinations are free when the
        // capture hotkey is registered: the release touches only the
        // slots that are registered, in the fixed slot order.
        var desired = Desired(false, CtrlA(), CtrlB());
        var before = Before(copyImage: CtrlA(), copyText: CtrlB());
        var hotkeys = new FakeHotkeys();

        DirectHotkeyApplier.Release(desired, before, hotkeys.Unregister);

        Assert.Equal(
            new[] { HotkeySlots.DirectCopyImage, HotkeySlots.DirectCopyText },
            hotkeys.UnregisterCalls);
        Assert.Empty(hotkeys.RegisterCalls);
    }

    [Fact]
    public void Release_ModeEnabled_UnregistersOnlyTheClearedSlots()
    {
        // The mode stays on: only the slot the destination state clears is
        // released; the slots that stay registered keep their combinations
        // untouched (re-registering them could only lose them).
        var desired = Desired(true, copyImage: CtrlA(), copyText: null);
        var before = Before(copyImage: CtrlA(), copyText: CtrlB());
        var hotkeys = new FakeHotkeys();

        DirectHotkeyApplier.Release(desired, before, hotkeys.Unregister);

        Assert.Equal(new[] { HotkeySlots.DirectCopyText }, hotkeys.UnregisterCalls);
        Assert.Empty(hotkeys.RegisterCalls);
    }

    [Fact]
    public void Release_NothingToRelease_MakesNoCalls()
    {
        // Every slot the destination wants is already in the state it
        // wants: the release phase is a no-op.
        var desired = Desired(true, copyImage: CtrlA());
        var before = Before(copyImage: CtrlA());
        var hotkeys = new FakeHotkeys();

        DirectHotkeyApplier.Release(desired, before, hotkeys.Unregister);

        Assert.Empty(hotkeys.UnregisterCalls);
        Assert.Empty(hotkeys.RegisterCalls);
    }
}
