using System.Runtime.InteropServices;
using System.Windows.Threading;
using CapParse.Platform;

namespace CapParse.Tests;

// The neutralization of a stale WM_HOTKEY, verified on the real delivery
// pipeline: a WM_HOTKEY message is posted to the real hidden window (the
// same message the OS posts when the combination is pressed) and the real
// message loop is pumped. A message that the OS enqueued before a
// hotkey's unregistration is delivered after it — and it must not trigger
// the hotkey's action, because the delivery looks up the registration at
// delivery time and an unregistered id finds no entry. This is what keeps
// a press of the traditional combination (for example Ctrl + Shift + X)
// from starting a traditional capture after the transition to the direct
// mode, even when the key was pressed in the same instant as the
// transition.
//
// The OS's own key-to-hotkey generation (a real key press) is the manual
// part; everything from the window's message queue to the hook is real
// here. The tests post to the coordinator's exact window handle (the
// internal test seam), so a parallel test class's identically-titled
// hidden window can never receive the message by mistake.
//
// Real RegisterHotKey is process-wide, so this class shares the
// Win32Hotkeys collection with the other real-registration tests (the
// classes are serialized) and uses combinations they do not use
// (Ctrl + Alt + F18/F19).
[Collection("Win32Hotkeys")]
public class GlobalHotkeyStaleMessageTests
{
    private static readonly GlobalHotkey.Modifier CtrlAlt =
        GlobalHotkey.Modifier.Control | GlobalHotkey.Modifier.Alt;

    // F18 and F19 virtual-key codes: obscure enough not to clash with
    // other applications or with the other real-registration tests.
    private const uint F18 = 0x81;
    private const uint F19 = 0x82;

    private const uint WmHotkey = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [Fact]
    public void StaleHotkeyMessage_AfterUnregister_DoesNotTriggerTheAction()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            using var hotkey = new GlobalHotkey();

            var captureFired = 0;
            Assert.True(hotkey.Register(1, CtrlAlt, F18, () => captureFired++));

            // The transition to the direct mode: the traditional capture
            // hotkey is unregistered while a WM_HOTKEY for it is already
            // in the window's message queue (posted, like the OS's own
            // message, before the unregistration took effect).
            hotkey.Unregister(1);
            PostMessage(hotkey.WindowHandle, WmHotkey, (IntPtr)1, IntPtr.Zero);
            Pump();

            Assert.Equal(0, captureFired);
            Assert.False(hotkey.IsRegistered(1));
        });
    }

    [Fact]
    public void HotkeyMessage_ForARegisteredId_StillTriggersTheAction()
    {
        // The positive control: the same pipeline delivers a message for an
        // id that is registered, so the negative result above proves a
        // neutralization, not a dead delivery path.
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            using var hotkey = new GlobalHotkey();

            var fired = 0;
            Assert.True(hotkey.Register(2, CtrlAlt, F19, () => fired++));

            PostMessage(hotkey.WindowHandle, WmHotkey, (IntPtr)2, IntPtr.Zero);
            Pump();

            Assert.Equal(1, fired);
            Assert.True(hotkey.IsRegistered(2));
        });
    }

    /// <summary>
    /// Runs a nested dispatcher loop until the background-priority
    /// operation that ends it executes: the messages posted to the hidden
    /// window (including the WM_HOTKEY under test) are delivered by this
    /// pump, exactly as the application's message loop would deliver them.
    /// </summary>
    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
