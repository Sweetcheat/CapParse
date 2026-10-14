using System.Windows;
using CapParse.Configuration;
using Application = System.Windows.Application;

namespace CapParse.Themes;

/// <summary>
/// Applies the active theme to the application (V0.2): it resolves the
/// preference with a <see cref="ThemeResolver"/> and swaps the matching
/// shared-token dictionary in <see cref="Application.Resources"/>.
/// Swapping the merged dictionary re-skins every window that consumes
/// the shared tokens, so a preference change takes effect without a
/// restart.
///
/// The application creates one instance and is the only owner of the
/// swap: there is no second mechanism to change the theme. <see
/// cref="Apply"/> must be called on the UI thread (it is driven by the
/// UI: startup and the settings window).
/// </summary>
public sealed class ThemeService
{
    private readonly ThemeResolver _resolver;
    private ResourceDictionary? _currentDictionary;

    public ThemeService(ThemeResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    /// <summary>
    /// The concrete theme in effect after the last <see cref="Apply"/>
    /// (the resolved state — distinct from the stored preference).
    /// </summary>
    public ResolvedTheme ResolvedTheme { get; private set; }

    /// <summary>
    /// Resolves the preference and applies the matching token dictionary
    /// to the application resources, replacing the previously applied
    /// theme dictionary.
    /// </summary>
    public void Apply(ThemePreference preference)
    {
        ResolvedTheme = _resolver.Resolve(preference);

        var next = CreateThemeDictionary(ResolvedTheme);
        var merged = Application.Current.Resources.MergedDictionaries;

        if (_currentDictionary is not null)
        {
            merged.Remove(_currentDictionary);
        }

        merged.Add(next);
        _currentDictionary = next;
    }

    /// <summary>
    /// Creates the shared token dictionary of a theme. Static on purpose:
    /// tests (and the application) can work with the dictionaries without
    /// a running <see cref="Application"/>.
    /// </summary>
    public static ResourceDictionary CreateThemeDictionary(ResolvedTheme theme)
    {
        var resource = theme == ResolvedTheme.Light
            ? "Theme.Light.xaml"
            : "Theme.Dark.xaml";

        return new ResourceDictionary
        {
            Source = new Uri($"CapParse;component/Themes/{resource}", UriKind.Relative)
        };
    }
}
