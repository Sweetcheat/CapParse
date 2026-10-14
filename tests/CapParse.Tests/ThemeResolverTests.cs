using CapParse.Configuration;
using CapParse.Themes;

namespace CapParse.Tests;

// The resolution logic is pure: the Windows system preference is injected
// as a delegate, so these tests are deterministic and never depend on the
// theme the test machine happens to use.
public class ThemeResolverTests
{
    [Fact]
    public void Resolve_SystemPreference_ResolvesToTheDarkSystemSource()
    {
        var resolver = new ThemeResolver(() => ResolvedTheme.Dark);

        Assert.Equal(ResolvedTheme.Dark, resolver.Resolve(ThemePreference.System));
    }

    [Fact]
    public void Resolve_SystemPreference_ResolvesToTheLightSystemSource()
    {
        var resolver = new ThemeResolver(() => ResolvedTheme.Light);

        Assert.Equal(ResolvedTheme.Light, resolver.Resolve(ThemePreference.System));
    }

    [Fact]
    public void Resolve_ForcedLight_IgnoresTheSystemSource()
    {
        var resolver = new ThemeResolver(() => ResolvedTheme.Dark);

        Assert.Equal(ResolvedTheme.Light, resolver.Resolve(ThemePreference.Light));
    }

    [Fact]
    public void Resolve_ForcedDark_IgnoresTheSystemSource()
    {
        var resolver = new ThemeResolver(() => ResolvedTheme.Light);

        Assert.Equal(ResolvedTheme.Dark, resolver.Resolve(ThemePreference.Dark));
    }

    [Fact]
    public void Resolve_ForcedPreference_NeverConsultsTheSystemSource()
    {
        // A forced preference must not read the system setting at all:
        // the source only matters for a System preference.
        var consulted = false;
        var resolver = new ThemeResolver(() =>
        {
            consulted = true;
            return ResolvedTheme.Dark;
        });

        resolver.Resolve(ThemePreference.Light);

        Assert.False(consulted);
    }
}
