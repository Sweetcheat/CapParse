using Microsoft.Win32;

namespace CapParse.Themes;

/// <summary>
/// The Windows "apps use light theme" setting, read from
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize</c>
/// (the <c>AppsUseLightTheme</c> DWORD, V0.2). That value is what
/// Windows writes for the "Apps use light theme" toggle in Settings, and
/// it is the source a "System" theme preference should follow: there is
/// no dedicated API for the system-wide apps preference, and the
/// per-window dark-mode attribute (<c>DwmSetWindowAttribute</c>)
/// describes individual windows, not the setting the user chose.
///
/// The read is defensive: a missing key, a value of an unexpected type,
/// or any exception falls back to <see cref="ResolvedTheme.Light"/> —
/// the state of a Windows that predates the dark-apps setting, so a
/// machine without the key never gets an appearance change it did not
/// ask for.
/// </summary>
public static class WindowsSystemTheme
{
    /// <summary>
    /// Interprets the raw <c>AppsUseLightTheme</c> value: 1 = light,
    /// 0 = dark. Anything else — including "the key is absent" (null) —
    /// falls back to light, per the class documentation.
    /// </summary>
    public static ResolvedTheme Interpret(int? appsUseLightTheme)
    {
        if (appsUseLightTheme == 1)
        {
            return ResolvedTheme.Light;
        }

        if (appsUseLightTheme == 0)
        {
            return ResolvedTheme.Dark;
        }

        return ResolvedTheme.Light;
    }

    /// <summary>
    /// Reads the current Windows apps light/dark preference, with the
    /// safe fallback documented in the class summary. Never throws.
    /// </summary>
    public static ResolvedTheme CurrentAppTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            return Interpret(
                key?.GetValue("AppsUseLightTheme") is int value ? value : null);
        }
        catch
        {
            // A machine where the value cannot be read must never break
            // startup: fall back to light (the pre-dark-mode default).
            return ResolvedTheme.Light;
        }
    }
}
