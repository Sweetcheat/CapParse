using System.Windows;
using Application = System.Windows.Application;

namespace CapParse.Tests;

// The test process's single WPF Application, shared by every test class
// that needs one. WPF allows exactly one Application instance per
// process, and its constructor's check-then-create is not atomic: two
// test classes racing to create one would make the loser throw. The lock
// serializes the adoption/creation, and the Current check makes every
// later consumer adopt the instance the first consumer created, no
// matter which class got there first.
//
// The adopted instance is a bare Application (no window, no message
// loop): enough to host the resource dictionaries the tests merge into
// it. It is never Run and never shut down; the process ends with it.
internal static class TestApplication
{
    private static readonly object CreationLock = new();

    public static Application Ensure()
    {
        lock (CreationLock)
        {
            return Application.Current ?? new Application();
        }
    }

    /// <summary>
    /// Removes the theme token dictionaries a test (or a test class that
    /// ran before it) merged into the shared Application's resources, so
    /// every test starts and ends with the same resource state: a class
    /// that asserts on the merged dictionaries (ThemeServiceTests) can
    /// never see a dictionary a parallel or previous class left behind.
    /// </summary>
    public static void RemoveThemeDictionaries()
    {
        var merged = Ensure().Resources.MergedDictionaries;
        for (var i = merged.Count - 1; i >= 0; i--)
        {
            var source = merged[i].Source?.OriginalString;
            if (source?.EndsWith("Theme.Light.xaml") == true
                || source?.EndsWith("Theme.Dark.xaml") == true)
            {
                merged.RemoveAt(i);
            }
        }
    }
}
