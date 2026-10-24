using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace CapParse.Platform;

/// <summary>
/// Registers global hotkeys with the Win32 API and raises each hotkey's
/// action on the UI thread when it is pressed, even when another
/// application has focus. Delivery goes through a hidden window that is
/// never shown.
///
/// Multiple hotkeys are supported, one registration per id: the
/// application owns one id per hotkey slot (see
/// <c>CapParse.Configuration.HotkeySlots</c>). A re-registration of an id
/// replaces that id's combination and action; the other ids are untouched.
/// A failed registration is never left half-applied: the id keeps the
/// combination it had before the attempt (or none, when it was not
/// registered). <see cref="Dispose"/> releases every active registration,
/// so the process can never leave an orphan hotkey behind.
///
/// All members must be called on the UI thread (the hidden window's
/// HwndSource lives there).
/// </summary>
public sealed class GlobalHotkey : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int WindowStylePopup = unchecked((int)0x80000000); // WS_POPUP

    /// <summary>RegisterHotKey modifier flags (fsModifiers).</summary>
    [Flags]
    public enum Modifier : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008
    }

    // Virtual-key code of the "X" key.
    public const uint KeyX = 0x58;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // One entry per active registration: the action to run when the id
    // fires, plus the combination that is actually registered, so a
    // failed replacement can restore the id's previous state.
    private readonly Dictionary<int, Entry> _entries = new();
    private readonly HwndSource _hwndSource;

    private sealed record Entry(Action Action, Modifier Modifiers, uint VirtualKey);

    public GlobalHotkey()
    {
        // A hidden popup window is all Windows needs to deliver WM_HOTKEY;
        // it is created on the UI thread and never shown.
        // WS_POPUP without WS_VISIBLE: the window is created hidden and
        // HwndSource never calls ShowWindow on it.
        var parameters = new HwndSourceParameters("CapParse hotkey", 0, 0)
        {
            WindowStyle = WindowStylePopup
        };
        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(OnWindowMessage);
    }

    /// <summary>
    /// Registers <paramref name="virtualKey"/> with <paramref name="modifiers"/>
    /// under <paramref name="id"/>. When the id is already registered, its
    /// previous combination and action are replaced by the new ones.
    /// </summary>
    /// <returns>
    /// False when the combination is already taken (by another id of this
    /// process or by another application). In that case the id is left
    /// exactly as it was before the attempt: a previously registered id
    /// keeps its old combination and action, an unregistered id stays
    /// unregistered.
    /// </returns>
    public bool Register(int id, Modifier modifiers, uint virtualKey, Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var previous = _entries.GetValueOrDefault(id);
        if (previous is not null)
        {
            UnregisterHotKey(_hwndSource.Handle, id);
        }

        if (RegisterHotKey(_hwndSource.Handle, id, (uint)modifiers, virtualKey))
        {
            _entries[id] = new Entry(action, modifiers, virtualKey);
            return true;
        }

        // The new combination is unavailable: restore the id's previous
        // state so the attempt is never left half-applied. If restoring
        // also fails (the previous combination was taken in the meantime),
        // the id simply ends unregistered — it can never be left pointing
        // at a combination it does not own.
        if (previous is not null &&
            !RegisterHotKey(_hwndSource.Handle, id, (uint)previous.Modifiers, previous.VirtualKey))
        {
            _entries.Remove(id);
        }

        return false;
    }

    /// <summary>
    /// Releases the registration of <paramref name="id"/> only; the other
    /// ids keep working. A release of an id that is not registered is a
    /// no-op.
    /// </summary>
    public void Unregister(int id)
    {
        if (_entries.Remove(id))
        {
            UnregisterHotKey(_hwndSource.Handle, id);
        }
    }

    /// <summary>True when <paramref name="id"/> currently has a registration.</summary>
    public bool IsRegistered(int id) => _entries.ContainsKey(id);

    /// <summary>
    /// The handle of the hidden window that receives the WM_HOTKEY
    /// messages. A test seam (the tests are the only consumers, through
    /// <c>InternalsVisibleTo</c>): it lets a test post a real WM_HOTKEY
    /// message to the exact window that the OS would deliver it to, and
    /// pump the real message loop, so the neutralization of a stale
    /// message after an unregister is verified on the real delivery
    /// pipeline.
    /// </summary>
    internal IntPtr WindowHandle => _hwndSource.Handle;

    /// <summary>Releases every active registration and the hidden window.</summary>
    public void Dispose()
    {
        // Snapshot the ids: Unregister mutates the dictionary.
        foreach (var id in _entries.Keys.ToArray())
        {
            UnregisterHotKey(_hwndSource.Handle, id);
        }

        _entries.Clear();
        _hwndSource.RemoveHook(OnWindowMessage);
        _hwndSource.Dispose();
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        var id = wParam.ToInt32();
        if (msg == WmHotkey && _entries.TryGetValue(id, out var entry))
        {
            entry.Action();
            handled = true;
        }

        return IntPtr.Zero;
    }
}
