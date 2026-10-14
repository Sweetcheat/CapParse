namespace CapParse.Configuration;

/// <summary>
/// The user's theme preference, persisted in the settings file (V0.2).
/// <see cref="System"/> follows the Windows "apps" light/dark setting;
/// <see cref="Light"/> and <see cref="Dark"/> force the theme regardless
/// of the system.
///
/// This is the *preference*: what the application actually renders is the
/// <see cref="Themes.ResolvedTheme"/> that a preference resolves to (a
/// <c>System</c> preference resolves to whichever theme Windows reports).
/// Keeping the two distinct is what lets the settings UI offer three
/// choices while the interface only has two appearances.
/// </summary>
public enum ThemePreference
{
    /// <summary>Follow the Windows light/dark apps setting.</summary>
    System = 0,

    /// <summary>Always use the light theme.</summary>
    Light,

    /// <summary>Always use the dark theme.</summary>
    Dark
}
