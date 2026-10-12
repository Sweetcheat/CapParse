using CapParse.Configuration;
using CapParse.Platform;

namespace CapParse.Tests;

// The recovery chain that keeps the application usable when a hotkey
// cannot be registered: the requested combination first, then the
// previous active one, then the default, and only then "no global
// hotkey" (the tray menu keeps working).
public class HotkeyRecoveryTests
{
    private static HotkeySetting CtrlShiftY()
        => new((uint)GlobalHotkey.Modifier.Control | (uint)GlobalHotkey.Modifier.Shift, 0x59);

    private static HotkeySetting CtrlA()
        => new((uint)GlobalHotkey.Modifier.Control, 0x41);

    private static Func<HotkeySetting, bool> FailingRegistration()
        => _ => false;

    [Fact]
    public void Requested_Succeeds_IsTheActiveHotkey()
    {
        var requested = CtrlShiftY();
        var attempts = new List<HotkeySetting>();
        var register = (HotkeySetting s) =>
        {
            attempts.Add(s);
            return true;
        };

        var active = HotkeyRecovery.Resolve(HotkeySetting.Default, requested, register);

        Assert.Equal(requested, active);
        Assert.Single(attempts);
        Assert.Equal(requested, attempts[0]);
    }

    [Fact]
    public void Requested_Fails_PreviousIsRestored()
    {
        var requested = CtrlShiftY();
        var previous = CtrlA();
        var attempts = new List<HotkeySetting>();
        var register = (HotkeySetting s) =>
        {
            attempts.Add(s);
            return s.Equals(previous);
        };

        var active = HotkeyRecovery.Resolve(previous, requested, register);

        Assert.Equal(previous, active);
        Assert.Equal(new[] { requested, previous }, attempts);
    }

    [Fact]
    public void RequestedAndPrevious_Fail_DefaultIsUsed()
    {
        var requested = CtrlShiftY();
        var previous = CtrlA();
        var register = (HotkeySetting s) => s.Equals(HotkeySetting.Default);

        var active = HotkeyRecovery.Resolve(previous, requested, register);

        Assert.Equal(HotkeySetting.Default, active);
    }

    [Fact]
    public void NothingCanBeRegistered_ReturnsNull()
    {
        var active = HotkeyRecovery.Resolve(
            CtrlA(), CtrlShiftY(), FailingRegistration());

        Assert.Null(active);
    }

    [Fact]
    public void NoPrevious_RequestedFails_DefaultIsUsed()
    {
        var register = (HotkeySetting s) => s.Equals(HotkeySetting.Default);

        var active = HotkeyRecovery.Resolve(null, CtrlShiftY(), register);

        Assert.Equal(HotkeySetting.Default, active);
    }

    [Fact]
    public void PreviousEqualToRequested_IsNotTriedAgain()
    {
        // When the requested combination is the one that was active
        // (and failed), re-trying it is pointless: the chain goes
        // straight to the default.
        var requested = CtrlShiftY();
        var attempts = new List<HotkeySetting>();
        var register = (HotkeySetting s) =>
        {
            attempts.Add(s);
            return false;
        };

        var active = HotkeyRecovery.Resolve(requested, requested, register);

        Assert.Null(active);
        Assert.Equal(new[] { requested, HotkeySetting.Default }, attempts);
    }
}
