using System.Windows.Input;
using CapParse.Platform;

namespace CapParse.Configuration;

/// <summary>
/// Interpretation of a key press while recording a new capture hotkey:
/// turns the raw WPF key event data (key, system key, modifier state)
/// into a decision — keep waiting, invalid combination, cancel, or a
/// recorded candidate. It is pure logic with no window or focus
/// dependency, so it is testable without a desktop session; the settings
/// window feeds it the live <c>Keyboard.Modifiers</c> state.
/// </summary>
public static class HotkeyInput
{
    public enum Interpretation
    {
        /// <summary>No action: no key, or a modifier key on its own (the pending combination only changed).</summary>
        Waiting,

        /// <summary>The combination is not a valid hotkey; keep recording.</summary>
        Invalid,

        /// <summary>Esc without other modifiers: cancel the change.</summary>
        Canceled,

        /// <summary>A valid combination was recorded (see <c>candidate</c>).</summary>
        Recorded
    }

    public static Interpretation Interpret(
        Key key,
        Key systemKey,
        ModifierKeys modifierState,
        out HotkeySetting? candidate)
    {
        candidate = null;

        // When Alt is part of the combination, WPF reports the key through
        // SystemKey and Key == Key.System.
        var actual = key == Key.System ? systemKey : key;

        if (actual == Key.None)
        {
            return Interpretation.Waiting;
        }

        if (IsModifierKey(actual))
        {
            // A modifier on its own (pressed before the main key) only
            // changes the pending combination: keep listening.
            return Interpretation.Waiting;
        }

        if (actual == Key.Escape)
        {
            // Esc without other modifiers cancels the change; with
            // modifiers it is an invalid combination (Esc is not a
            // supported hotkey key).
            return ToModifierFlags(modifierState) == (uint)GlobalHotkey.Modifier.None
                ? Interpretation.Canceled
                : Interpretation.Invalid;
        }

        var setting = new HotkeySetting(
            ToModifierFlags(modifierState),
            (uint)KeyInterop.VirtualKeyFromKey(actual));

        if (!setting.IsValid)
        {
            return Interpretation.Invalid;
        }

        candidate = setting;
        return Interpretation.Recorded;
    }

    /// <summary>The modifier keys a hotkey can use (Ctrl/Shift/Alt/Windows) as Win32 flags.</summary>
    public static uint ToModifierFlags(ModifierKeys state)
    {
        var flags = GlobalHotkey.Modifier.None;

        if (state.HasFlag(ModifierKeys.Control))
        {
            flags |= GlobalHotkey.Modifier.Control;
        }

        if (state.HasFlag(ModifierKeys.Shift))
        {
            flags |= GlobalHotkey.Modifier.Shift;
        }

        if (state.HasFlag(ModifierKeys.Alt))
        {
            flags |= GlobalHotkey.Modifier.Alt;
        }

        if (state.HasFlag(ModifierKeys.Windows))
        {
            flags |= GlobalHotkey.Modifier.Win;
        }

        return (uint)flags;
    }

    /// <summary>Keys that only change the pending combination, never complete it.</summary>
    public static bool IsModifierKey(Key key)
        => key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt
            or Key.LWin or Key.RWin;
}
