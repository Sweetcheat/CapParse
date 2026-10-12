using CapParse.Platform;

namespace CapParse.Configuration;

/// <summary>
/// The capture hotkey as a Win32 combination: the RegisterHotKey modifier
/// flags plus the virtual key code of the key. This is the value persisted
/// in the settings file and the value the application registers globally.
/// It is plain data: it has no dependency on WPF, so the settings logic is
/// testable without a desktop session.
/// </summary>
public sealed class HotkeySetting : IEquatable<HotkeySetting>
{
    /// <summary>The default modifiers: Ctrl + Shift.</summary>
    public const uint DefaultModifiers =
        (uint)GlobalHotkey.Modifier.Control | (uint)GlobalHotkey.Modifier.Shift;

    /// <summary>The default key: the virtual key code of "X".</summary>
    public const uint DefaultKey = GlobalHotkey.KeyX;

    /// <summary>The default hotkey: Ctrl + Shift + X.</summary>
    public static HotkeySetting Default { get; } = new(DefaultModifiers, DefaultKey);

    // The only modifier flags a valid setting may carry; anything else
    // (a corrupted file, a future flag) is rejected.
    private const uint KnownModifiers =
        (uint)GlobalHotkey.Modifier.Control |
        (uint)GlobalHotkey.Modifier.Shift |
        (uint)GlobalHotkey.Modifier.Alt |
        (uint)GlobalHotkey.Modifier.Win;

    // A valid hotkey must contain one of these. A combination with Shift
    // only (or no modifier at all) would fire while the user types in
    // other applications, so it is rejected.
    private const uint RequiredModifiers =
        (uint)GlobalHotkey.Modifier.Control |
        (uint)GlobalHotkey.Modifier.Alt |
        (uint)GlobalHotkey.Modifier.Win;

    /// <summary>Creates an empty setting (used by the JSON deserializer).</summary>
    public HotkeySetting()
    {
    }

    public HotkeySetting(uint modifiers, uint key)
    {
        Modifiers = modifiers;
        Key = key;
    }

    /// <summary>The Win32 RegisterHotKey modifier flags (a subset of Ctrl/Shift/Alt/Win).</summary>
    public uint Modifiers { get; set; }

    /// <summary>The virtual key code of the key.</summary>
    public uint Key { get; set; }

    /// <summary>
    /// True when the combination is acceptable: only known modifiers, at
    /// least one of Ctrl/Alt/Windows, and a supported key.
    /// </summary>
    public bool IsValid
    {
        get
        {
            if ((Modifiers & ~KnownModifiers) != 0)
            {
                return false;
            }

            if ((Modifiers & RequiredModifiers) == 0)
            {
                return false;
            }

            return IsSupportedKey(Key);
        }
    }

    /// <summary>
    /// The keys the settings UI can offer: digits, letters and F1-F24.
    /// The range is deliberately small; it covers the combinations users
    /// actually choose for a capture shortcut and keeps the validation
    /// and the display names trivial. The virtual key codes are the Win32
    /// RegisterHotKey values (the same ones WPF's KeyInterop produces):
    /// 0-9 = 0x30-0x39, A-Z = 0x41-0x5A, F1-F12 = 0x70-0x7B,
    /// F13-F24 = 0x7C-0x87.
    /// </summary>
    public static bool IsSupportedKey(uint virtualKey)
        => (virtualKey >= 0x30 && virtualKey <= 0x39)   // 0-9
        || (virtualKey >= 0x41 && virtualKey <= 0x5A)   // A-Z
        || (virtualKey >= 0x70 && virtualKey <= 0x87);  // F1-F24

    /// <summary>The human-readable combination, e.g. "Ctrl + Shift + X".</summary>
    public string DisplayString
    {
        get
        {
            var parts = new List<string>(5);

            if (HasModifier(GlobalHotkey.Modifier.Control))
            {
                parts.Add("Ctrl");
            }

            if (HasModifier(GlobalHotkey.Modifier.Shift))
            {
                parts.Add("Shift");
            }

            if (HasModifier(GlobalHotkey.Modifier.Alt))
            {
                parts.Add("Alt");
            }

            if (HasModifier(GlobalHotkey.Modifier.Win))
            {
                parts.Add("Windows");
            }

            parts.Add(KeyName);
            return string.Join(" + ", parts);
        }
    }

    private bool HasModifier(GlobalHotkey.Modifier modifier)
        => (Modifiers & (uint)modifier) != 0;

    private string KeyName
    {
        get
        {
            if (Key >= 0x41 && Key <= 0x5A)
            {
                return char.ToUpperInvariant((char)Key).ToString();
            }

            if (Key >= 0x30 && Key <= 0x39)
            {
                return ((char)Key).ToString();
            }

            if (Key >= 0x70 && Key <= 0x7B)
            {
                return "F" + (Key - 0x70 + 1);
            }

            if (Key >= 0x7C && Key <= 0x87)
            {
                return "F" + (Key - 0x7C + 13);
            }

            return "?";
        }
    }

    public bool Equals(HotkeySetting? other)
        => other is not null && Modifiers == other.Modifiers && Key == other.Key;

    public override bool Equals(object? obj) => Equals(obj as HotkeySetting);

    public override int GetHashCode() => HashCode.Combine(Modifiers, Key);

    public static bool operator ==(HotkeySetting? left, HotkeySetting? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(HotkeySetting? left, HotkeySetting? right)
        => !(left == right);

    public override string ToString() => DisplayString;
}
