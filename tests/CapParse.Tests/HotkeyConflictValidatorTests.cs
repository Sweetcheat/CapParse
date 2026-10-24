using CapParse.Configuration;
using CapParse.Platform;

namespace CapParse.Tests;

// Internal-conflict rules for the hotkey slots: two slots of the
// application must never carry the same combination, so one shortcut can
// never point at two actions. Unset slots (null) register nothing and
// cannot conflict. Pure logic: no Windows API is involved.
public class HotkeyConflictValidatorTests
{
    private static HotkeySetting CtrlShift(uint key)
        => new(
            (uint)GlobalHotkey.Modifier.Control | (uint)GlobalHotkey.Modifier.Shift,
            key);

    private static HotkeySetting CtrlA()
        => new((uint)GlobalHotkey.Modifier.Control, 0x41);

    private static HotkeySetting AltF9()
        => new((uint)GlobalHotkey.Modifier.Alt, 0x78);

    [Fact]
    public void Slots_AreDistinctIds()
    {
        // The ids key the Win32 registrations: two slots sharing an id
        // would drive one registration, so the ids must never collide.
        var ids = new[]
        {
            HotkeySlots.Capture,
            HotkeySlots.DirectCopyImage,
            HotkeySlots.DirectCopyText,
            HotkeySlots.DirectSaveImage
        };
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void EmptySlotMap_HasNoConflict()
    {
        Assert.Empty(HotkeyConflictValidator.FindConflicts(new Dictionary<int, HotkeySetting?>()));
    }

    [Fact]
    public void AllSlotsUnset_HasNoConflict()
    {
        var slots = new Dictionary<int, HotkeySetting?>
        {
            [HotkeySlots.Capture] = null,
            [HotkeySlots.DirectCopyImage] = null,
            [HotkeySlots.DirectCopyText] = null,
            [HotkeySlots.DirectSaveImage] = null
        };

        Assert.Empty(HotkeyConflictValidator.FindConflicts(slots));
    }

    [Fact]
    public void DistinctCombinations_HaveNoConflict()
    {
        var slots = new Dictionary<int, HotkeySetting?>
        {
            [HotkeySlots.Capture] = CtrlShift(0x59),
            [HotkeySlots.DirectCopyImage] = CtrlA(),
            [HotkeySlots.DirectCopyText] = AltF9(),
            [HotkeySlots.DirectSaveImage] = null
        };

        Assert.Empty(HotkeyConflictValidator.FindConflicts(slots));
    }

    [Fact]
    public void CaptureAndDirectSlot_SharingACombination_IsReported()
    {
        var shared = CtrlShift(0x42);
        var slots = new Dictionary<int, HotkeySetting?>
        {
            [HotkeySlots.Capture] = shared,
            [HotkeySlots.DirectCopyText] = shared,
            [HotkeySlots.DirectSaveImage] = CtrlA()
        };

        var conflicts = HotkeyConflictValidator.FindConflicts(slots);

        var conflict = Assert.Single(conflicts);
        Assert.Equal(shared, conflict);
    }

    [Fact]
    public void TwoDirectSlots_SharingACombination_IsReported()
    {
        var shared = CtrlA();
        var slots = new Dictionary<int, HotkeySetting?>
        {
            [HotkeySlots.DirectCopyImage] = shared,
            [HotkeySlots.DirectSaveImage] = shared
        };

        var conflict = Assert.Single(HotkeyConflictValidator.FindConflicts(slots));
        Assert.Equal(shared, conflict);
    }

    [Fact]
    public void ThreeSlots_SharingACombination_IsReportedOnce()
    {
        var shared = CtrlShift(0x30);
        var slots = new Dictionary<int, HotkeySetting?>
        {
            [HotkeySlots.Capture] = shared,
            [HotkeySlots.DirectCopyImage] = shared,
            [HotkeySlots.DirectCopyText] = shared
        };

        // One entry per conflicting combination, no matter how many slots
        // carry it: the caller needs to know which combinations clash, not
        // how many times.
        var conflict = Assert.Single(HotkeyConflictValidator.FindConflicts(slots));
        Assert.Equal(shared, conflict);
    }

    [Fact]
    public void TwoIndependentDups_BothAreReported()
    {
        var slots = new Dictionary<int, HotkeySetting?>
        {
            [HotkeySlots.Capture] = CtrlA(),
            [HotkeySlots.DirectCopyImage] = CtrlA(),
            [HotkeySlots.DirectCopyText] = AltF9(),
            [HotkeySlots.DirectSaveImage] = AltF9()
        };

        var conflicts = HotkeyConflictValidator.FindConflicts(slots);

        Assert.Equal(2, conflicts.Count);
        Assert.Contains(slots[HotkeySlots.Capture], conflicts);
        Assert.Contains(slots[HotkeySlots.DirectCopyText], conflicts);
    }
}
