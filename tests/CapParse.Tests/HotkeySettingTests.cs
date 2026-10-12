using CapParse.Configuration;
using CapParse.Platform;

namespace CapParse.Tests;

public class HotkeySettingTests
{
    [Fact]
    public void Default_IsCtrlShiftX()
    {
        Assert.Equal(
            (uint)GlobalHotkey.Modifier.Control | (uint)GlobalHotkey.Modifier.Shift,
            HotkeySetting.DefaultModifiers);
        Assert.Equal(GlobalHotkey.KeyX, HotkeySetting.DefaultKey);
        Assert.True(HotkeySetting.Default.IsValid);
        Assert.Equal("Ctrl + Shift + X", HotkeySetting.Default.DisplayString);
    }

    [Theory]
    [InlineData(2, 0x58, "Ctrl + X")]
    [InlineData(6, 0x58, "Ctrl + Shift + X")]
    [InlineData(1, 0x41, "Alt + A")]
    [InlineData(8, 0x41, "Windows + A")]
    [InlineData(3, 0x41, "Ctrl + Alt + A")]
    [InlineData(15, 0x74, "Ctrl + Shift + Alt + Windows + F5")]
    [InlineData(6, 0x30, "Ctrl + Shift + 0")]
    [InlineData(6, 0x75, "Ctrl + Shift + F6")]
    [InlineData(14, 0x87, "Ctrl + Shift + Windows + F24")]
    public void DisplayString_UsesConventionalModifierOrder(uint modifiers, uint key, string expected)
    {
        var setting = new HotkeySetting(modifiers, key);
        Assert.Equal(expected, setting.DisplayString);
    }

    [Theory]
    [InlineData(2, 0x41)]     // Ctrl + A
    [InlineData(6, 0x39)]     // Ctrl + Shift + 9
    [InlineData(8, 0x5A)]     // Windows + Z
    [InlineData(1, 0x70)]     // Alt + F1
    [InlineData(2, 0x75)]     // Ctrl + F6
    [InlineData(15, 0x87)]    // all four modifiers + F24
    public void Valid_CombinationsAreAccepted(uint modifiers, uint key)
    {
        Assert.True(new HotkeySetting(modifiers, key).IsValid);
    }

    [Theory]
    [InlineData(0, 0x41)]     // no modifier at all
    [InlineData(4, 0x41)]     // Shift only: would fire while typing
    [InlineData(2, 0x0D)]     // Ctrl + Enter: unsupported key
    [InlineData(2, 0x1B)]     // Ctrl + Esc: Esc cancels captures
    [InlineData(2, 0x20)]     // Ctrl + Space
    [InlineData(2, 0x26)]     // Ctrl + Up arrow
    [InlineData(2, 0x2E)]     // Ctrl + Delete
    [InlineData(2, 0x5B)]     // Ctrl + { : unsupported symbol key
    [InlineData(2, 0x00)]     // no key
    [InlineData(16, 0x41)]    // unknown modifier flag
    [InlineData(22, 0x41)]    // known + unknown modifier flags
    public void Invalid_CombinationsAreRejected(uint modifiers, uint key)
    {
        Assert.False(new HotkeySetting(modifiers, key).IsValid);
    }

    // All F keys (F1-F24) are supported: their Win32 virtual key codes
    // (0x70-0x87) do not collide with any other key.
    [Fact]
    public void FKeys_AreSupported()
    {
        Assert.True(new HotkeySetting(2, 0x74).IsValid);   // Ctrl + F5
        Assert.True(new HotkeySetting(2, 0x76).IsValid);   // Ctrl + F7
        Assert.Equal("Ctrl + F7", new HotkeySetting(2, 0x76).DisplayString);
    }

    [Theory]
    [InlineData(0x30, true)]   // 0
    [InlineData(0x39, true)]   // 9
    [InlineData(0x3A, false)]  // ;
    [InlineData(0x41, true)]   // A
    [InlineData(0x5A, true)]   // Z
    [InlineData(0x5B, false)]  // {
    [InlineData(0x6F, false)]  // before F1
    [InlineData(0x70, true)]   // F1
    [InlineData(0x7B, true)]   // F12
    [InlineData(0x7C, true)]   // F13
    [InlineData(0x87, true)]   // F24
    [InlineData(0x88, false)]  // past F24
    [InlineData(0x1B, false)]  // Esc
    [InlineData(0x0D, false)]  // Enter
    [InlineData(0x26, false)]  // Up arrow
    [InlineData(0x00, false)]  // no key
    public void IsSupportedKey_CoversDigitsLettersAndFKeys(uint virtualKey, bool expected)
    {
        Assert.Equal(expected, HotkeySetting.IsSupportedKey(virtualKey));
    }

    [Fact]
    public void Equality_IsValueBased()
    {
        var a = new HotkeySetting(6, 0x58);
        var b = new HotkeySetting(6, 0x58);
        var c = new HotkeySetting(2, 0x58);

        Assert.True(a.Equals(b));
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a.Equals(c));
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_IsTheDisplayString()
    {
        Assert.Equal(HotkeySetting.Default.DisplayString, HotkeySetting.Default.ToString());
    }
}
