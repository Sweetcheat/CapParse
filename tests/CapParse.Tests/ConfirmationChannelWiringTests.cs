using System.IO;

namespace CapParse.Tests;

// Wiring guard for the presentation channels of a capture operation's
// result. The decisions themselves are covered by the pure tests
// (CaptureConfirmationTests, CaptureToastPresenterTests,
// DirectModeStatusTests); what those cannot reach is the application's
// wiring — which channel each result is presented through — because the
// App (a WPF Application) cannot be instantiated in the test process:
// WPF allows one Application per process and OnStartup has system
// effects (see docs/development.md §72.5).
//
// The regression this guard pins (§73): a direct action's result was
// once presented twice on screen — through the application's own toast
// AND the legacy tray balloon, with the same message — while the
// traditional flow presented its toast alone. The assertions read the
// source of the wiring (deterministic; no desktop, no timers, no window
// positions) and check the channel of each path:
//   - a direct action's result: the own toast only (no balloon);
//   - a finished traditional capture: the own toast ("Captura pronta");
//   - the legacy balloon: kept only for the first-run mode notice,
//     which is an independent event, not a capture result.
public class ConfirmationChannelWiringTests
{
    /// <summary>
    /// Locates the application's source file from the test output
    /// directory: walking up to the repository root works in Debug and
    /// Release alike (the bin/&lt;config&gt;/&lt;tfm&gt; depth varies).
    /// </summary>
    private static string AppSourcePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "CapParse", "App.xaml.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate src/CapParse/App.xaml.cs from the test output directory.");
    }

    /// <summary>
    /// The body of the <c>OnDirectFeedback</c> method: from its signature
    /// to the next method declaration (the private
    /// <c>ShowConfirmation</c> that follows it in the file).
    /// </summary>
    private static string OnDirectFeedbackBody()
    {
        var source = File.ReadAllText(AppSourcePath());

        var start = source.IndexOf("private void OnDirectFeedback(", StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException(
                "The OnDirectFeedback method is missing from App.xaml.cs.");
        }

        var end = source.IndexOf("\n    private ", start + 1, StringComparison.Ordinal);
        if (end <= start)
        {
            throw new InvalidOperationException(
                "Could not delimit the OnDirectFeedback method body in App.xaml.cs.");
        }

        return source[start..end];
    }

    [Fact]
    public void DirectActionResult_IsPresentedThroughTheOwnToast_AndNoSecondChannel()
    {
        // The single-presentation rule: the direct action's result goes
        // through the application's own confirmation and through no
        // second visual channel — the legacy balloon used to show the
        // same message a second time on screen (the duplication fixed
        // in §73), so the same operation can never be presented twice.
        var body = OnDirectFeedbackBody();

        Assert.Contains(
            "ShowConfirmation(CaptureConfirmation.FromDirectFeedback(feedback))", body);
        Assert.DoesNotContain("ShowTrayNotification", body);
        Assert.DoesNotContain("ShowBalloonTip", body);
    }

    [Fact]
    public void FinishedTraditionalCapture_IsConfirmedThroughTheOwnToast()
    {
        // The traditional flow's success confirmation is the
        // application's own toast with the ready message: the crop
        // exists and the result window owns the image — nothing is
        // claimed about the OCR (still running) or a file (this mode
        // never writes one). A finished capture produces exactly this
        // one own confirmation.
        var source = File.ReadAllText(AppSourcePath());

        Assert.Contains(
            "ShowConfirmation(CaptureConfirmationPolicy.ForTraditionalCapture(true))", source);
    }

    [Fact]
    public void TheTrayBalloon_IsKeptForTheFirstRunNotice_AndNotForCaptureResults()
    {
        // The legacy balloon is not removed outright: the first-run mode
        // notice is an independent event (not a capture result) and
        // keeps using it. The guard pins that the legitimate use
        // survives (the call is still present in the application) while
        // the per-operation use is gone (asserted in the first test).
        var source = File.ReadAllText(AppSourcePath());

        Assert.Contains("ShowTrayNotification", source);
        Assert.DoesNotContain("ShowTrayNotification", OnDirectFeedbackBody());
    }
}
