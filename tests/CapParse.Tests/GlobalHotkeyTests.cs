using CapParse.Configuration;
using CapParse.Platform;

namespace CapParse.Tests;

// The values below are a contract with the Win32 RegisterHotKey API: if any
// of them changes, the default Ctrl + Shift + X hotkey silently stops working.
public class GlobalHotkeyTests
{
    [Fact]
    public void ModifierFlags_MatchWin32RegisterHotKeyValues()
    {
        Assert.Equal(0x0002u, (uint)GlobalHotkey.Modifier.Control);
        Assert.Equal(0x0004u, (uint)GlobalHotkey.Modifier.Shift);
    }

    [Fact]
    public void DefaultHotkeyKey_IsTheVirtualKeyCodeOfX()
    {
        Assert.Equal(0x58u, GlobalHotkey.KeyX);
    }
}

// Multi-id registration tests against the real Win32 API: the hidden window
// and the HwndSource it hosts need an STA thread (xUnit's test threads are
// MTA), so every test runs on the dedicated STA thread the first-run tests
// use. The combinations are deliberately obscure (Ctrl + Alt + F13..F15) so
// they do not clash with other applications. All registration tests live in
// this single collection on purpose: RegisterHotKey is process-wide, so the
// classes that register real combinations must never run in parallel with
// each other (the Win32Hotkeys collection serializes them).
[Collection("Win32Hotkeys")]
public class GlobalHotkeyMultiIdTests
{
    private static readonly GlobalHotkey.Modifier CtrlAlt =
        GlobalHotkey.Modifier.Control | GlobalHotkey.Modifier.Alt;

    // F13, F14, F15 virtual-key codes.
    private const uint F13 = 0x7C;
    private const uint F14 = 0x7D;
    private const uint F15 = 0x7E;

    [Fact]
    public void TwoDistinctIds_BothRegister()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            using var hotkey = new GlobalHotkey();

            Assert.True(hotkey.Register(1, CtrlAlt, F13, () => { }));
            Assert.True(hotkey.Register(2, CtrlAlt, F14, () => { }));

            Assert.True(hotkey.IsRegistered(1));
            Assert.True(hotkey.IsRegistered(2));
        });
    }

    [Fact]
    public void DuplicateCombination_SecondIdFails_AndStaysUnregistered()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            using var hotkey = new GlobalHotkey();

            Assert.True(hotkey.Register(1, CtrlAlt, F13, () => { }));

            // The same combination cannot be owned by a second id.
            Assert.False(hotkey.Register(2, CtrlAlt, F13, () => { }));
            Assert.False(hotkey.IsRegistered(2));
            Assert.True(hotkey.IsRegistered(1));
        });
    }

    [Fact]
    public void ReRegisteringAnId_ReplacesItsCombinationAndReleasesTheOldOne()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            using var hotkey = new GlobalHotkey();

            Assert.True(hotkey.Register(1, CtrlAlt, F13, () => { }));
            Assert.True(hotkey.Register(1, CtrlAlt, F14, () => { }));

            // The replacement freed F13: it can be taken by another id now.
            Assert.True(hotkey.Register(2, CtrlAlt, F13, () => { }));
            // ...while F14 is still owned by id 1.
            Assert.False(hotkey.Register(3, CtrlAlt, F14, () => { }));

            Assert.True(hotkey.IsRegistered(1));
            Assert.True(hotkey.IsRegistered(2));
            Assert.False(hotkey.IsRegistered(3));
        });
    }

    [Fact]
    public void FailedReplacement_KeepsThePreviousCombination()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            using var hotkey = new GlobalHotkey();

            Assert.True(hotkey.Register(1, CtrlAlt, F13, () => { }));
            Assert.True(hotkey.Register(2, CtrlAlt, F14, () => { }));

            // F14 is taken by id 2: id 1's attempt must fail and leave id 1
            // exactly as it was (still on F13).
            Assert.False(hotkey.Register(1, CtrlAlt, F14, () => { }));
            Assert.True(hotkey.IsRegistered(1));

            // Proof that id 1 is still on F13, not F14: releasing id 2 frees
            // F14 (registerable elsewhere), while F13 stays owned by id 1.
            hotkey.Unregister(2);
            Assert.True(hotkey.Register(3, CtrlAlt, F14, () => { }));
            Assert.False(hotkey.Register(4, CtrlAlt, F13, () => { }));

            Assert.False(hotkey.IsRegistered(4));
        });
    }

    [Fact]
    public void Unregister_ReleasesOnlyThatId()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            using var hotkey = new GlobalHotkey();

            Assert.True(hotkey.Register(1, CtrlAlt, F13, () => { }));
            Assert.True(hotkey.Register(2, CtrlAlt, F14, () => { }));

            hotkey.Unregister(1);

            Assert.False(hotkey.IsRegistered(1));
            Assert.True(hotkey.IsRegistered(2));

            // F13 is free; F14 is not (id 2 keeps it).
            Assert.True(hotkey.Register(3, CtrlAlt, F13, () => { }));
            Assert.False(hotkey.Register(4, CtrlAlt, F14, () => { }));
        });
    }

    [Fact]
    public void Unregister_UnknownId_IsANoOp()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            using var hotkey = new GlobalHotkey();

            Assert.True(hotkey.Register(1, CtrlAlt, F13, () => { }));

            hotkey.Unregister(99);

            Assert.True(hotkey.IsRegistered(1));
        });
    }

    [Fact]
    public void Dispose_ReleasesEveryRegistration()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            var hotkey = new GlobalHotkey();
            Assert.True(hotkey.Register(1, CtrlAlt, F13, () => { }));
            Assert.True(hotkey.Register(2, CtrlAlt, F14, () => { }));
            hotkey.Dispose();

            // No orphan registration: both combinations are free again.
            var fresh = new GlobalHotkey();
            try
            {
                Assert.True(fresh.Register(1, CtrlAlt, F13, () => { }));
                Assert.True(fresh.Register(2, CtrlAlt, F14, () => { }));
            }
            finally
            {
                fresh.Dispose();
            }
        });
    }

    [Fact]
    public void Register_NullAction_Throws()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            using var hotkey = new GlobalHotkey();
            Assert.Throws<ArgumentNullException>(() => hotkey.Register(1, CtrlAlt, F13, null!));
        });
    }
}
