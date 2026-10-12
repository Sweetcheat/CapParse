namespace CapParse.Configuration;

/// <summary>
/// The hotkey recovery chain shared by the application startup and the
/// Settings window: try the requested combination first; when it cannot be
/// registered (another application owns it), restore the previous active
/// combination and, if that is also unavailable, the default. Only when
/// none of them can be registered is the application left without a global
/// hotkey — the tray menu keeps working in that case.
///
/// The chain is a pure function of the registration attempts, so it is
/// testable without Windows; the application supplies the function that
/// performs the real RegisterHotKey calls.
/// </summary>
public static class HotkeyRecovery
{
    public static HotkeySetting? Resolve(
        HotkeySetting? previous,
        HotkeySetting requested,
        Func<HotkeySetting, bool> tryRegister)
    {
        if (tryRegister(requested))
        {
            return requested;
        }

        if (previous is not null && !previous.Equals(requested) && tryRegister(previous))
        {
            return previous;
        }

        if (tryRegister(HotkeySetting.Default))
        {
            return HotkeySetting.Default;
        }

        return null;
    }
}
