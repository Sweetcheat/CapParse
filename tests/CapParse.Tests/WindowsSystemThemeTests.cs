using CapParse.Themes;

namespace CapParse.Tests;

// Only the pure interpretation of the AppsUseLightTheme value is tested
// here: reading the registry depends on the test machine, so the live
// read is covered by the safe-fallback design (documented in
// WindowsSystemTheme) and by manual validation, not by automation.
public class WindowsSystemThemeTests
{
    [Theory]
    [InlineData(1, ResolvedTheme.Light)]
    [InlineData(0, ResolvedTheme.Dark)]
    [InlineData(null, ResolvedTheme.Light)]  // key absent: safe fallback
    [InlineData(2, ResolvedTheme.Light)]     // invalid value: safe fallback
    [InlineData(-1, ResolvedTheme.Light)]    // invalid value: safe fallback
    public void Interpret_AppsUseLightTheme_MapsTheKnownValues(
        int? appsUseLightTheme, ResolvedTheme expected)
    {
        Assert.Equal(expected, WindowsSystemTheme.Interpret(appsUseLightTheme));
    }
}
