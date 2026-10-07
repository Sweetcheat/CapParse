using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace CapParse.Platform;

/// <summary>
/// Registers a single global hotkey with the Win32 API and raises an action
/// on the UI thread when it is pressed, even when another application has
/// focus. Delivery goes through a hidden window that is never shown.
/// </summary>
public sealed class GlobalHotkey : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 1;
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

    private readonly Action _action;
    private readonly HwndSource _hwndSource;
    private bool _registered;

    public GlobalHotkey(Action action)
    {
        _action = action;

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
    /// Registers the hotkey globally.
    /// </summary>
    /// <returns>False when the combination is already taken by another application.</returns>
    public bool Register(Modifier modifiers, uint virtualKey)
    {
        _registered = RegisterHotKey(_hwndSource.Handle, HotkeyId, (uint)modifiers, virtualKey);
        return _registered;
    }

    public void Dispose()
    {
        if (_registered)
        {
            UnregisterHotKey(_hwndSource.Handle, HotkeyId);
            _registered = false;
        }

        _hwndSource.RemoveHook(OnWindowMessage);
        _hwndSource.Dispose();
    }

    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            _action();
            handled = true;
        }

        return IntPtr.Zero;
    }
}
