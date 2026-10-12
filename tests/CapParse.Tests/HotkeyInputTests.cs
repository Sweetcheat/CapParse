using System.Windows.Input;
using CapParse.Configuration;
using CapParse.Platform;

namespace CapParse.Tests;

// The interpretation of a key press while recording a new hotkey: the
// exact logic the Settings window runs on every PreviewKeyDown. It is
// pure (no window, no focus), so these tests validate the decisions the
// window makes; the delivery of the events to the focused control is a
// WPF behavior that requires the manual/interactive validation.
public class HotkeyInputTests
{
    private static readonly ModifierKeys None = ModifierKeys.None;

    [Fact]
    public void MainKey_WithModifiers_IsRecorded()
    {
        var result = HotkeyInput.Interpret(Key.K, Key.None, ModifierKeys.Control | ModifierKeys.Alt, out var candidate);

        Assert.Equal(HotkeyInput.Interpretation.Recorded, result);
        Assert.NotNull(candidate);
        Assert.Equal("Ctrl + Alt + K", candidate!.DisplayString);
        Assert.Equal(
            (uint)GlobalHotkey.Modifier.Control | (uint)GlobalHotkey.Modifier.Alt,
            candidate.Modifiers);
        Assert.Equal(0x4Bu, candidate.Key);
    }

    // The modifiers are read from the keyboard state, not from the key
    // event: pressing the main key after the modifiers (the normal way)
    // is exactly this call — the state carries the pending combination.
    [Fact]
    public void MainKey_PressedAfterTheModifiers_UsesThePendingModifierState()
    {
        var result = HotkeyInput.Interpret(Key.Y, Key.None,
            ModifierKeys.Control | ModifierKeys.Shift, out var candidate);

        Assert.Equal(HotkeyInput.Interpretation.Recorded, result);
        Assert.Equal("Ctrl + Shift + Y", candidate!.DisplayString);
    }

    [Fact]
    public void Esc_Alive_CancelsTheChange()
    {
        var result = HotkeyInput.Interpret(Key.Escape, Key.None, None, out var candidate);

        Assert.Equal(HotkeyInput.Interpretation.Canceled, result);
        Assert.Null(candidate);
    }

    [Fact]
    public void Esc_WithModifiers_IsInvalid()
    {
        var result = HotkeyInput.Interpret(Key.Escape, Key.None, ModifierKeys.Control, out _);

        Assert.Equal(HotkeyInput.Interpretation.Invalid, result);
    }

    [Theory]
    [InlineData(ModifierKeys.None)]    // bare key
    [InlineData(ModifierKeys.Shift)]   // Shift only: would fire while typing
    public void Invalid_Combinations_AreRejected(ModifierKeys state)
    {
        var result = HotkeyInput.Interpret(Key.X, Key.None, state, out _);

        Assert.Equal(HotkeyInput.Interpretation.Invalid, result);
    }

    [Theory]
    [InlineData(Key.Enter)]
    [InlineData(Key.Space)]
    [InlineData(Key.Up)]
    [InlineData(Key.Delete)]
    public void Unsupported_Keys_AreRejected(Key key)
    {
        var result = HotkeyInput.Interpret(key, Key.None, ModifierKeys.Control, out _);

        Assert.Equal(HotkeyInput.Interpretation.Invalid, result);
    }

    [Fact]
    public void KeySystem_ReportsTheRealKeyThroughSystemKey()
    {
        // Alt combinations arrive as Key == Key.System, SystemKey == key.
        var result = HotkeyInput.Interpret(Key.System, Key.K, ModifierKeys.Alt, out var candidate);

        Assert.Equal(HotkeyInput.Interpretation.Recorded, result);
        Assert.Equal("Alt + K", candidate!.DisplayString);
    }

    [Theory]
    [InlineData(Key.LeftCtrl)]
    [InlineData(Key.RightCtrl)]
    [InlineData(Key.LeftShift)]
    [InlineData(Key.RightShift)]
    [InlineData(Key.LeftAlt)]
    [InlineData(Key.RightAlt)]
    [InlineData(Key.LWin)]
    [InlineData(Key.RWin)]
    public void ModifierKeysOnTheirOwn_KeepWaiting(Key key)
    {
        var result = HotkeyInput.Interpret(key, Key.None, ModifierKeys.Control, out var candidate);

        Assert.Equal(HotkeyInput.Interpretation.Waiting, result);
        Assert.Null(candidate);
    }

    [Fact]
    public void NoKey_KeepsWaiting()
    {
        Assert.Equal(HotkeyInput.Interpretation.Waiting,
            HotkeyInput.Interpret(Key.None, Key.None, None, out _));
    }

    [Fact]
    public void FKeys_AreRecordedWithTheirNames()
    {
        Assert.Equal(HotkeyInput.Interpretation.Recorded,
            HotkeyInput.Interpret(Key.F1, Key.None, ModifierKeys.Control, out var f1));
        Assert.Equal("Ctrl + F1", f1!.DisplayString);

        Assert.Equal(HotkeyInput.Interpretation.Recorded,
            HotkeyInput.Interpret(Key.F13, Key.None, ModifierKeys.Control, out var f13));
        Assert.Equal("Ctrl + F13", f13!.DisplayString);

        Assert.Equal(HotkeyInput.Interpretation.Recorded,
            HotkeyInput.Interpret(Key.F24, Key.None, ModifierKeys.Control, out var f24));
        Assert.Equal("Ctrl + F24", f24!.DisplayString);
    }

    [Fact]
    public void F7_IsRecordedAsF7()
    {
        // F7's Win32 virtual key code (0x76) is unique: the combination
        // is recorded and displayed as F7.
        var result = HotkeyInput.Interpret(Key.F7, Key.None, ModifierKeys.Control, out var candidate);

        Assert.Equal(HotkeyInput.Interpretation.Recorded, result);
        Assert.Equal("Ctrl + F7", candidate!.DisplayString);
    }

    [Fact]
    public void AllFourModifiers_AreAccepted()
    {
        var result = HotkeyInput.Interpret(Key.D0, Key.None,
            ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Windows,
            out var candidate);

        Assert.Equal(HotkeyInput.Interpretation.Recorded, result);
        Assert.Equal("Ctrl + Shift + Alt + Windows + 0", candidate!.DisplayString);
    }

    [Theory]
    [InlineData(ModifierKeys.None, 0u)]
    [InlineData(ModifierKeys.Control, 2u)]
    [InlineData(ModifierKeys.Shift, 4u)]
    [InlineData(ModifierKeys.Alt, 1u)]
    [InlineData(ModifierKeys.Windows, 8u)]
    [InlineData(ModifierKeys.Control | ModifierKeys.Shift, 6u)]
    [InlineData(ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Windows, 15u)]
    public void ToModifierFlags_MapsToWin32RegisterHotKeyValues(ModifierKeys state, uint expected)
    {
        Assert.Equal(expected, HotkeyInput.ToModifierFlags(state));
    }
}
