using CapParse.Configuration;

namespace CapParse.Themes;

/// <summary>
/// Resolves a theme preference to the concrete theme the application
/// should use (V0.2). The source of the Windows system preference is
/// injected, so the resolution is deterministic and testable without
/// depending on the machine running the test (the application supplies
/// <see cref="WindowsSystemTheme.CurrentAppTheme"/>).
/// </summary>
public sealed class ThemeResolver
{
    private readonly Func<ResolvedTheme> _systemThemeSource;

    public ThemeResolver(Func<ResolvedTheme> systemThemeSource)
    {
        _systemThemeSource = systemThemeSource
            ?? throw new ArgumentNullException(nameof(systemThemeSource));
    }

    /// <summary>
    /// The concrete theme for a preference: a forced preference wins
    /// over the system; a <c>System</c> preference defers to the injected
    /// system source (consulted only in that case, so reading the
    /// registry is skipped when it is not needed).
    /// </summary>
    public ResolvedTheme Resolve(ThemePreference preference)
    {
        return preference switch
        {
            ThemePreference.Light => ResolvedTheme.Light,
            ThemePreference.Dark => ResolvedTheme.Dark,
            _ => _systemThemeSource()
        };
    }
}
