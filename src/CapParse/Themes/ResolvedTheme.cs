namespace CapParse.Themes;

/// <summary>
/// The concrete appearance the application uses (V0.2): the result of
/// resolving a <see cref="Configuration.ThemePreference"/>. A
/// <c>System</c> preference resolves to whichever of these the Windows
/// "apps" light/dark setting indicates; <c>Light</c> and <c>Dark</c>
/// preferences resolve to themselves. Exactly one of the two matching
/// token dictionaries (<c>Theme.Light.xaml</c> / <c>Theme.Dark.xaml</c>)
/// is merged into the application resources at a time.
/// </summary>
public enum ResolvedTheme
{
    Light,
    Dark
}
