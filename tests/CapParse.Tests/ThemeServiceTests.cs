using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using CapParse.Configuration;
using CapParse.Themes;
using Application = System.Windows.Application;
using ResourceDictionary = System.Windows.ResourceDictionary;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace CapParse.Tests;

// A bare Application (no window, no message loop) is enough to exercise
// the resource swap that ThemeService performs. WPF allows only one
// Application instance per process, and the application itself owns a
// single theme service for its whole lifetime, so the class mirrors that:
// one static Application and one static ThemeService, shared by all the
// tests. Tests inside a class run sequentially, and every test applies its
// own preference before asserting, so no test depends on the state left by
// a previous one. The Application instance is adopted through
// TestApplication, which also serves the other test class that needs one:
// whoever creates it first wins, the rest adopt it, and no parallel
// creation race can occur. The class shares the WpfApp collection with
// that other class: both mutate the shared Application's theme
// dictionaries, which are not thread-safe, so the classes run
// sequentially.
[Collection("WpfApp")]
public class ThemeServiceTests
{
    static ThemeServiceTests()
    {
        // WPF registers the "pack" URI scheme lazily (the static
        // constructors of its PackUriHelper classes), and a test host does
        // not guarantee that happens before the first pack URI is created.
        // Register it explicitly — exactly as WPF itself does — so the
        // theme dictionaries can be loaded from the CapParse assembly
        // deterministically. Declared before the App field so the scheme
        // is known before the Application instance is created.
        if (!UriParser.IsKnownScheme("pack"))
        {
            UriParser.Register(
                new GenericUriParser(GenericUriParserOptions.GenericAuthority),
                "pack", -1);
        }
    }

    // Created once for the whole test process; adopted through
    // TestApplication so a parallel test class cannot race to create the
    // process's single Application instance first.
    private static readonly Application App = TestApplication.Ensure();

    // One service for the whole test process, like the application has
    // exactly one: it tracks the dictionary it applied last, so a new
    // Apply always replaces the previous one.
    private static readonly ThemeService Service =
        new(new ThemeResolver(() => ResolvedTheme.Dark));

    [Fact]
    public void Apply_ForcedLight_MergesTheLightDictionary()
    {
        Service.Apply(ThemePreference.Light);

        Assert.Equal(ResolvedTheme.Light, Service.ResolvedTheme);
        var dictionary = App.Resources.MergedDictionaries.SingleOrDefault();
        Assert.NotNull(dictionary);
        Assert.EndsWith("Theme.Light.xaml", dictionary.Source.OriginalString);
    }

    [Fact]
    public void Apply_ForcedDark_MergesTheDarkDictionary()
    {
        Service.Apply(ThemePreference.Dark);

        Assert.Equal(ResolvedTheme.Dark, Service.ResolvedTheme);
        var dictionary = App.Resources.MergedDictionaries.SingleOrDefault();
        Assert.NotNull(dictionary);
        Assert.EndsWith("Theme.Dark.xaml", dictionary.Source.OriginalString);
    }

    [Fact]
    public void Apply_SystemPreference_FollowsTheInjectedSystemSource()
    {
        Service.Apply(ThemePreference.System);

        // The shared service resolves System with the injected source
        // (dark), whatever preference the previous test left behind.
        Assert.Equal(ResolvedTheme.Dark, Service.ResolvedTheme);
        var dictionary = App.Resources.MergedDictionaries.SingleOrDefault();
        Assert.NotNull(dictionary);
        Assert.EndsWith("Theme.Dark.xaml", dictionary.Source.OriginalString);
    }

    [Fact]
    public void Apply_ASecondPreference_ReplacesThePreviousDictionary()
    {
        Service.Apply(ThemePreference.Light);
        Service.Apply(ThemePreference.System);

        Assert.Equal(ResolvedTheme.Dark, Service.ResolvedTheme);
        var dictionaries = App.Resources.MergedDictionaries;
        Assert.Single(dictionaries);
        Assert.EndsWith("Theme.Dark.xaml", dictionaries[0].Source.OriginalString);
    }

    [Fact]
    public void ThemeDictionaries_DefineTheSameSemanticTokens()
    {
        var light = ThemeService.CreateThemeDictionary(ResolvedTheme.Light);
        var dark = ThemeService.CreateThemeDictionary(ResolvedTheme.Dark);

        var expected = new[]
        {
            "BackgroundBrush", "SurfaceBrush", "SurfaceSecondaryBrush",
            "BorderBrush", "BorderStrongBrush",
            "TextPrimaryBrush", "TextSecondaryBrush", "TextMutedBrush",
            "AccentBrush", "OnAccentBrush", "AccentHoverBrush", "AccentPressedBrush",
            "SubtleOverlayBrush", "SubtlePressedBrush", "AccentSubtleBrush",
            "SuccessBrush", "WarningBrush", "ErrorBrush",
            "FocusBorderBrush",
            "OverlayDimBrush"
        };

        foreach (var key in expected)
        {
            Assert.True(light.Contains(key), $"the light theme is missing {key}");
            Assert.True(dark.Contains(key), $"the dark theme is missing {key}");
            Assert.IsType<SolidColorBrush>(light[key]);
            Assert.IsType<SolidColorBrush>(dark[key]);
        }

        // The two themes are swap-compatible: they must define exactly the
        // same keys, so a preference change never leaves a view without a
        // token (or with a token the other theme does not know).
        Assert.Equal(
            light.Keys.Cast<string>().OrderBy(key => key),
            dark.Keys.Cast<string>().OrderBy(key => key));
    }

    [Fact]
    public void SettingsWindow_OnlyUsesTokensThatExistInBothThemes()
    {
        AssertWindowUsesSharedTokens("SettingsWindow.xaml",
            ReadWindowXaml("SettingsWindow.xaml"));
    }

    [Fact]
    public void FirstRunWindow_OnlyUsesTokensThatExistInBothThemes()
    {
        AssertWindowUsesSharedTokens("FirstRunWindow.xaml",
            ReadWindowXaml("FirstRunWindow.xaml"));
    }

    [Fact]
    public void MainWindow_OnlyUsesTokensThatExistInBothThemes()
    {
        AssertWindowUsesSharedTokens("MainWindow.xaml",
            ReadWindowXaml("MainWindow.xaml"));
    }

    // Every DynamicResource reference a window makes must be a shared
    // semantic token, so the window re-skins when the theme dictionary is
    // swapped: the key must exist in both dictionaries.
    private static void AssertWindowUsesSharedTokens(
        string windowName, string xaml)
    {
        var referenced = new HashSet<string>(
            Regex.Matches(xaml, @"DynamicResource\s+([A-Za-z0-9_]+)")
                .Cast<Match>()
                .Select(match => match.Groups[1].Value));

        Assert.NotEmpty(referenced);

        var light = ThemeService.CreateThemeDictionary(ResolvedTheme.Light);
        var dark = ThemeService.CreateThemeDictionary(ResolvedTheme.Dark);

        foreach (var key in referenced)
        {
            Assert.True(light.Contains(key),
                $"{windowName} uses {key}, which the light theme does not define");
            Assert.True(dark.Contains(key),
                $"{windowName} uses {key}, which the dark theme does not define");
        }
    }

    /// <summary>
    /// The XAML source is compiled to BAML and not copied next to the test
    /// binary: resolve it from the repository layout, walking up from the
    /// test output directory until the project tree is found.
    /// </summary>
    private static string ReadWindowXaml(string windowFile)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var depth = 0; directory is not null && depth < 8; depth++)
        {
            var candidate = Path.Combine(
                directory.FullName, "src", "CapParse", windowFile);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"{windowFile} not found above the test output directory.");
    }
}
