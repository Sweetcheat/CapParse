using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CapParse.Configuration;
using CapParse.Themes;
// The test project also sees the Windows Forms reference (transitively,
// through the application project), which defines its own Button: the
// alias keeps the WPF type, the same way SettingsWindow.xaml.cs does.
using Button = System.Windows.Controls.Button;

namespace CapParse.Tests;

// Regression test for the crash reported when the settings window was
// opened: the direct-mode toggle's style pointed the toggle at the
// button's control template (TargetType="Button"), and WPF validates the
// template's TargetType against the control's type only when the template
// is instantiated — on the first layout pass, i.e. when the window is
// shown. XAML compilation and the tests that never show the window
// therefore could not catch it: the process crashed on the desktop the
// first time the user opened the settings.
//
// This test builds the real window on an STA thread (with a bare
// Application and the theme resources the window's constructor needs)
// and forces the template instantiation on the toggle and on the regular
// buttons — exactly the step that used to throw. A template whose
// TargetType does not match the control's type throws from the layout
// pass, so a green test proves the toggle and the buttons each carry a
// template compatible with their own type.
//
// Shares the WpfApp collection with ThemeServiceTests: both mutate the
// process's single Application resource state (theme dictionaries), and
// WPF collections are not thread-safe, so the classes must not run in
// parallel.
[Collection("WpfApp")]
public class SettingsWindowTemplateTests
{
    [Fact]
    public void SettingsWindow_TemplatesAreCompatibleWithTheirControlTypes()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            // The shared process-wide Application (adopted, never Run).
            // The theme dictionaries the window's tokens resolve from
            // must be the only ones merged: other test classes may have
            // left theirs behind, and this test must not leave its own,
            // so the shared resource state is the same on the way out.
            TestApplication.Ensure();
            TestApplication.RemoveThemeDictionaries();
            try
            {
                // The same centralized theme application the startup
                // performs, with a fixed source so no OS theme lookup is
                // needed: the window's state refresh resolves the shared
                // tokens through FindResource, which requires the
                // dictionary to be merged.
                new ThemeService(new ThemeResolver(() => ResolvedTheme.Light))
                    .Apply(ThemePreference.Light);

                // Constructing the window parses its XAML (the styles and
                // templates are resolved here); the crash used to happen
                // one step later, when the templates were instantiated.
                var window = new SettingsWindow();

                // The window is never shown in this test, so force the
                // step the crash used to happen in: the first layout pass
                // (run when the window is shown) is when control
                // templates are instantiated, and a template whose
                // TargetType does not match the control's type throws
                // from there — synchronously, taking the process down.
                // Measuring and arranging each control directly
                // reproduces that instantiation (a detached window's own
                // Measure pass does not propagate through the scroll
                // viewer, so the controls are measured on their own):
                // in the buggy state this raised, on the toggle, the
                // exact exception the user saw when opening the window.
                var toggle = (ToggleButton)window.FindName("DirectModeToggle");
                Assert.IsType<ControlTemplate>(toggle.Template);
                toggle.Measure(new Size(800, 800));
                toggle.Arrange(new Rect(toggle.DesiredSize));

                // The regular buttons keep their own (Button-typed)
                // template: the fix must not break the other direction of
                // the mismatch.
                foreach (var name in new[] { "ChangeButton", "SaveButton", "CancelButton" })
                {
                    var button = (Button)window.FindName(name);
                    Assert.IsType<ControlTemplate>(button.Template);
                    button.Measure(new Size(800, 800));
                    button.Arrange(new Rect(button.DesiredSize));
                }
            }
            finally
            {
                TestApplication.RemoveThemeDictionaries();
            }
        });
    }
}
