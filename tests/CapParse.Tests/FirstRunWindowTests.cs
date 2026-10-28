using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using CapParse;

namespace CapParse.Tests;

// Regression tests for the first-run window's sizing (ui.md §23): the
// window must never clip its content. The layout is a SizeToContent=Height
// window whose content is capped at the available desktop area by a
// ScrollViewer, so the tests assert exactly those properties on the real
// window (constructed, never shown) plus the pure height-cap function.
public class FirstRunWindowTests
{
    [Theory]
    [InlineData(768, 708)]   // a typical 1080p work area at 100% scale
    [InlineData(1040, 980)]  // a taller screen
    [InlineData(400, 340)]   // a low screen
    public void ContentMaxHeight_ReservesTheWindowChrome(
        double workAreaHeight,
        double expected)
    {
        Assert.Equal(expected, FirstRunWindow.ContentMaxHeight(workAreaHeight));
    }

    [Fact]
    public void ContentMaxHeight_AbsurdlySmallWorkArea_KeepsAMinimum()
    {
        // A work area smaller than the chrome reserve must not collapse the
        // content to zero: the window demands at least its minimum height.
        Assert.Equal(120, FirstRunWindow.ContentMaxHeight(100));
        Assert.Equal(120, FirstRunWindow.ContentMaxHeight(0));
    }

    [Fact]
    public void Window_SizesToContentHeight_AndCapsAtWorkArea()
    {
        // WPF windows require an STA thread; xUnit's test threads are MTA,
        // so the window is constructed on a dedicated STA thread.
        RunOnStaThread(() =>
        {
            var window = new FirstRunWindow("Inicie uma captura com Ctrl + Shift + X.");

            // The fixed 300 DIP height that clipped the content is gone:
            // the window grows with its content instead.
            Assert.Equal(SizeToContent.Height, window.SizeToContent);
            Assert.True(double.IsNaN(window.Height), "the window must not carry a fixed height");

            // The width is the one established by the design.
            Assert.Equal(520, window.Width);
            Assert.Equal(ResizeMode.NoResize, window.ResizeMode);

            // The content sits in a scroll viewer that caps its growth at
            // the available desktop area: extreme font/DPI combinations
            // scroll instead of extending the window past the screen.
            var scroller = Assert.IsType<ScrollViewer>(window.Content);
            Assert.Equal(ScrollBarVisibility.Auto, scroller.VerticalScrollBarVisibility);
            Assert.Equal(ScrollBarVisibility.Disabled, scroller.HorizontalScrollBarVisibility);

            if (SystemParameters.WorkArea.Height > 0)
            {
                var max = FirstRunWindow.ContentMaxHeight(SystemParameters.WorkArea.Height);
                Assert.Equal(max, scroller.MaxHeight);
                Assert.InRange(scroller.MaxHeight, 120, SystemParameters.WorkArea.Height);
            }
        });
    }

    [Fact]
    public void Choice_DefaultsToDeferred_WhenTheWindowIsClosedWithoutChoosing()
    {
        // Closing the presentation without choosing (the X button, Esc)
        // keeps the traditional mode: the choice stays Deferred, so a
        // dismissed presentation never changes a setting.
        RunOnStaThread(() =>
        {
            var window = new FirstRunWindow("Inicie uma captura com Ctrl + Shift + X.");

            Assert.Equal(FirstRunModeChoice.Deferred, window.Choice);

            window.Close();

            Assert.Equal(FirstRunModeChoice.Deferred, window.Choice);
        });
    }

    [Theory]
    [InlineData("TraditionalButton", FirstRunModeChoice.Traditional)]
    [InlineData("DeferButton", FirstRunModeChoice.Deferred)]
    public void ChoiceButton_SetsTheChoice_AndClosesTheWindow(
        string buttonName,
        FirstRunModeChoice expected)
    {
        // The traditional mode and the deferred decision close the
        // presentation on the spot and carry no activation confirmation.
        RunOnStaThread(() =>
        {
            var window = new FirstRunWindow("Inicie uma captura com Ctrl + Shift + X.");
            var closed = false;
            window.Closed += (_, _) => closed = true;

            var button = Assert.IsType<Button>(window.FindName(buttonName));
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));

            Assert.True(closed, "the choice button must close the presentation");
            Assert.Equal(expected, window.Choice);
        });
    }

    [Fact]
    public void DirectButton_SetsTheChoice_ShowsTheConfirmation_WithoutClosing()
    {
        // Choosing the direct mode acknowledges the activation in the
        // window itself before it closes: the choice card is replaced by
        // the confirmation card, and the window stays open until the user
        // dismisses it — no arbitrary delay, no new dialog. The tray
        // balloon that reports the same fact is a redundant, best-effort
        // signal and can never be the only one.
        RunOnStaThread(() =>
        {
            var window = new FirstRunWindow("Inicie uma captura com Ctrl + Shift + X.");
            var closed = false;
            window.Closed += (_, _) => closed = true;

            var direct = Assert.IsType<Button>(window.FindName("DirectButton"));
            direct.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, direct));

            Assert.False(closed, "the confirmation must be seen before the window closes");
            Assert.Equal(FirstRunModeChoice.Direct, window.Choice);

            var choiceCard = Assert.IsType<Border>(window.FindName("ChoiceCard"));
            var confirmation = Assert.IsType<Border>(window.FindName("DirectConfirmationCard"));
            Assert.Equal(Visibility.Collapsed, choiceCard.Visibility);
            Assert.Equal(Visibility.Visible, confirmation.Visibility);
        });
    }

    [Fact]
    public void DirectConfirmation_PointsAtConfiguringAShortcut_AndClosesTheWindow()
    {
        // The confirmation names the next step (the settings) and the
        // close button ends the presentation; the choice made before it
        // is what gets applied when the window closes.
        RunOnStaThread(() =>
        {
            var window = new FirstRunWindow("Inicie uma captura com Ctrl + Shift + X.");
            var closed = false;
            window.Closed += (_, _) => closed = true;

            var direct = Assert.IsType<Button>(window.FindName("DirectButton"));
            direct.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, direct));

            var message = Assert.IsType<TextBlock>(window.FindName("DirectConfirmationMessage"));
            Assert.Contains("Modo Direto ativado", message.Text);
            Assert.Contains("Configurações", message.Text);

            var close = Assert.IsType<Button>(window.FindName("CloseButton"));
            close.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, close));

            Assert.True(closed, "the close button must close the presentation");
            Assert.Equal(FirstRunModeChoice.Direct, window.Choice);
        });
    }

    [Fact]
    public void Window_ExplainsBothModes_WithTheShortcutNote()
    {
        // The presentation explains the two modes and that the direct
        // mode's shortcuts are configured in the settings: it never
        // invents a combination on the user's behalf.
        RunOnStaThread(() =>
        {
            var window = new FirstRunWindow("Inicie uma captura com Ctrl + Shift + X.");

            Assert.Equal(
                "Inicie uma captura com Ctrl + Shift + X.",
                window.HowToCaptureText.Text);

            var traditional = Assert.IsType<TextBlock>(
                window.FindName("TraditionalModeDescriptionText"));
            Assert.Contains("Recomendado", traditional.Text);

            var direct = Assert.IsType<TextBlock>(
                window.FindName("DirectModeDescriptionText"));
            Assert.Contains("Configurações", direct.Text);

            // The traditional mode is the default choice: Enter confirms it.
            Assert.True(Assert.IsType<Button>(window.FindName("TraditionalButton")).IsDefault);
        });
    }

    /// <summary>
    /// Runs <paramref name="action"/> on a dedicated STA thread with a WPF
    /// message pump (WPF UI components require STA). The pump lets the
    /// window process the messages its construction needs; any exception
    /// (including assertion failures) is propagated back to the test thread.
    /// </summary>
    internal static void RunOnStaThread(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                var frame = new DispatcherFrame();
                dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        // A dispatcher operation swallows callback
                        // exceptions (they only surface through
                        // Dispatcher.UnhandledException, which nothing
                        // subscribes to here): capture the error so the
                        // test thread can rethrow it and the test can fail.
                        error = ex;
                    }
                    finally
                    {
                        frame.Continue = false;
                    }
                });
                Dispatcher.PushFrame(frame);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
