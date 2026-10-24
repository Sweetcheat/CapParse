using System.Windows;
using System.Windows.Controls.Primitives;
using CapParse.Configuration;
using CapParse.Themes;

namespace CapParse.Tests;

// Cancelling the settings window (closing it without saving) must
// discard every pending change: the settings file is untouched and the
// running application keeps the state it had. The window is constructed
// on an STA thread with the shared test Application (the same harness
// the template regression test uses); the edit is made through the real
// toggle control, exactly as a user's click would do it, and the window
// is closed through its public Close, exactly as the cancel button's
// handler does.
//
// Shares the WpfApp collection with the other classes that touch the
// shared Application's resource state.
[Collection("WpfApp")]
public class SettingsWindowCancelTests
{
    [Fact]
    public void Cancel_DiscardsThePendingChanges_LeavesTheSettingsFileUntouched()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            TestApplication.Ensure();
            TestApplication.RemoveThemeDictionaries();
            try
            {
                new ThemeService(new ThemeResolver(() => ResolvedTheme.Light))
                    .Apply(ThemePreference.Light);

                var before = AppSettings.Load();

                var window = new SettingsWindow();

                // The pending edit a user would make: enable the direct
                // mode through the real toggle (its Checked handler is
                // the same one a click would raise).
                var toggle = (ToggleButton)window.FindName("DirectModeToggle");
                toggle.IsChecked = true;
                Assert.True(toggle.IsChecked == true);

                // Cancel: close the window without saving.
                window.Close();

                // Nothing was committed: the settings on disk are the
                // same settings they had before the window opened.
                var after = AppSettings.Load();
                Assert.Equal(before.DirectModeEnabled, after.DirectModeEnabled);
                Assert.Equal(before.CaptureHotkey, after.CaptureHotkey);
                Assert.Equal(before.DirectSlot(HotkeySlots.DirectCopyImage), after.DirectSlot(HotkeySlots.DirectCopyImage));
                Assert.Equal(before.DirectSlot(HotkeySlots.DirectCopyText), after.DirectSlot(HotkeySlots.DirectCopyText));
                Assert.Equal(before.DirectSlot(HotkeySlots.DirectSaveImage), after.DirectSlot(HotkeySlots.DirectSaveImage));
                Assert.Equal(before.DirectSavePolicy, after.DirectSavePolicy);
                Assert.Equal(before.DirectSaveFolder, after.DirectSaveFolder);
                Assert.Equal(before.ThemePreference, after.ThemePreference);
                Assert.Equal(before.FirstRunPresented, after.FirstRunPresented);
            }
            finally
            {
                TestApplication.RemoveThemeDictionaries();
            }
        });
    }
}
