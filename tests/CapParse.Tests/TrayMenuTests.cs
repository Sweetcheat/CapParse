using System.Windows.Forms;
using CapParse.UI;

namespace CapParse.Tests;

// The tray menu's behavior, verified on the real WinForms items (constructed
// on an STA thread, like the other window tests): the "Capturar" item
// starts the traditional capture, the application disables it while the
// direct mode is in force (SetCaptureEnabled is what the application calls,
// passing the same guard the capture entry point applies), and the other
// items (settings, exit) are mode-independent. The mode decision itself
// (which of the committed/pending states disables the item) is the pure
// guard already covered by HotkeyTransactionTests; here the control's real
// behavior is exercised: the enabled state and the click dispatch.
public class TrayMenuTests
{
    [Fact]
    public void CaptureItem_IsEnabledByDefault()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            var menu = new TrayMenu(() => { }, () => { }, () => { });

            Assert.True(menu.CaptureItem.Enabled);
        });
    }

    [Fact]
    public void SetCaptureEnabled_False_DisablesTheCaptureItem_AndTrueReenablesIt()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            var menu = new TrayMenu(() => { }, () => { }, () => { });

            menu.SetCaptureEnabled(false);
            Assert.False(menu.CaptureItem.Enabled);

            menu.SetCaptureEnabled(true);
            Assert.True(menu.CaptureItem.Enabled);
        });
    }

    [Fact]
    public void CaptureItem_Click_InvokesTheCaptureCallback()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            var captures = 0;
            var menu = new TrayMenu(() => captures++, () => { }, () => { });

            menu.CaptureItem.PerformClick();

            Assert.Equal(1, captures);
        });
    }

    [Fact]
    public void OtherItems_KeepWorking_WhileTheCaptureItemIsDisabled()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            var captures = 0;
            var settings = 0;
            var exits = 0;
            var menu = new TrayMenu(() => captures++, () => settings++, () => exits++);

            // The direct mode: the "Capturar" item is disabled, but the
            // remaining items are mode-independent and always available.
            menu.SetCaptureEnabled(false);
            menu.SettingsItem.PerformClick();
            menu.ExitItem.PerformClick();

            Assert.Equal(0, captures);
            Assert.Equal(1, settings);
            Assert.Equal(1, exits);
            Assert.False(menu.CaptureItem.Enabled);
        });
    }

    [Fact]
    public void Menu_HasTheThreeItemsInTheDesignedOrder()
    {
        FirstRunWindowTests.RunOnStaThread(() =>
        {
            var menu = new TrayMenu(() => { }, () => { }, () => { });

            Assert.Equal("Capturar", menu.Menu.Items[0].Text);
            Assert.Equal("Configurações", menu.Menu.Items[1].Text);
            Assert.Equal("Sair", menu.Menu.Items[2].Text);
        });
    }
}
