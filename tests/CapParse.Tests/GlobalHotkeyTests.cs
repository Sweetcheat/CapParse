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
