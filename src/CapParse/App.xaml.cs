using System.ComponentModel;
using System.Windows;
using CapParse.Capture;
using CapParse.OCR;
using CapParse.Platform;
using CapParse.UI;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using WinForms = System.Windows.Forms;

namespace CapParse;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly CaptureCoordinator _captureCoordinator = new();
    // The OCR engine is created lazily on the first recognition, so
    // starting the app costs nothing; the coordinator releases it on exit.
    private readonly OcrCoordinator _ocrCoordinator = new();
    private WinForms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private GlobalHotkey? _hotkey;
    private FrozenOverlaySession? _overlaySession;
    // True from the first trigger of a capture until the attempt ends
    // (session closed, or the attempt failed). Unlike _overlaySession,
    // which is only assigned after the constructor returns, this guard
    // also covers the construction: the constructor pumps the UI thread,
    // so a second hotkey/tray trigger can reenter StartCapture while one
    // is in flight.
    private bool _captureInFlight;
    private System.Drawing.Rectangle? _lastSelection;
    private System.Drawing.Bitmap? _lastCrop;
    // Cancellation token of the in-flight recognition, if any. A new
    // capture cancels it so a stale recognition can never publish its
    // result over a newer one.
    private CancellationTokenSource? _ocrCancellation;
    private OcrResult? _lastOcrResult;
    private bool _isShuttingDown;

    public CaptureCoordinator CaptureCoordinator => _captureCoordinator;

    /// <summary>
    /// The selection of the last finished capture, in virtual-desktop
    /// physical coordinates, or null if it was cancelled or had no area.
    /// The M6 crop pipeline consumes this.
    /// </summary>
    public System.Drawing.Rectangle? LastSelection => _lastSelection;

    /// <summary>
    /// The cropped bitmap of the last finished capture (32bpp ARGB, exactly
    /// the selection's size), or null if it was cancelled, had no area, or
    /// the crop failed. The application owns the bitmap: a new capture
    /// result (including a cancel) disposes the previous crop, and the
    /// application disposes it on exit. The next milestone consumes this.
    /// </summary>
    public System.Drawing.Bitmap? LastCrop => _lastCrop;

    /// <summary>
    /// The OCR result of the last capture whose crop was recognized,
    /// or null if none has completed yet, the last capture had no crop,
    /// its recognition was cancelled, or it failed. An image without
    /// text is a valid, empty result and is exposed like any other.
    /// M8-B presents this result to the user.
    /// </summary>
    public OcrResult? LastOcrResult => _lastOcrResult;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Tray utility: the app keeps running while the main window is closed.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _mainWindow = new MainWindow();
        _mainWindow.Closing += OnMainWindowClosing;
        _mainWindow.Show();

        CreateTrayIcon();
        RegisterCaptureHotkey();
    }

    private void StartCapture()
    {
        // While a capture attempt is in flight (GDI capture, session
        // construction, active overlays) further triggers are ignored
        // until it ends (Idle -> Capturing).
        if (_captureInFlight)
        {
            return;
        }

        _captureInFlight = true;

        try
        {
            var result = _captureCoordinator.StartCapture();
            var session = new FrozenOverlaySession(result, (selection, crop) =>
            {
                _overlaySession = null;
                _captureInFlight = false;
                _lastSelection = selection;
                _lastOcrResult = null;

                // A finished attempt (including a cancel) replaces the
                // previous crop: dispose it so no bitmap is left orphaned.
                _lastCrop?.Dispose();
                _lastCrop = crop;

                if (crop is not null)
                {
                    RecognizeCropAsync(crop);
                }
            });

            // The constructor pumps the UI thread, so the session may have
            // already closed itself (a deferred close) before returning:
            // in that case the callback has already reset the guard and no
            // live session remains, so do not keep a stale reference.
            _overlaySession = session.Closed ? null : session;
        }
        catch
        {
            // The session cleaned up after itself (overlays closed, captured
            // bitmap disposed). Reset the guard here too, so a failed
            // attempt can never leave the app permanently blocked. Show a
            // user-friendly message; technical details are intentionally
            // not shown.
            _overlaySession = null;
            _captureInFlight = false;

            MessageBox.Show(
                "Could not capture the screen.\n\nTry again.",
                "CapParse",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// Runs the OCR engine over the crop of a finished capture without
    /// blocking the UI thread: the engine is created lazily (first use
    /// only) and the recognition itself is asynchronous. A recognition
    /// already in flight is cancelled first, and only a non-cancelled
    /// result is published, so a stale recognition can never overwrite
    /// the result of a newer capture.
    ///
    /// Cancellation (a new capture superseded this one) is not an error
    /// and is reported silently. A real failure surfaces with the same
    /// user-friendly style the capture flow uses; the captured text and
    /// image are never logged.
    /// </summary>
    private async void RecognizeCropAsync(System.Drawing.Bitmap crop)
    {
        _ocrCancellation?.Cancel();
        _ocrCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _ocrCancellation = cancellation;

        try
        {
            var result = await _ocrCoordinator.RecognizeAsync(
                new ImageData(crop), cancellation.Token);

            if (!cancellation.IsCancellationRequested)
            {
                _lastOcrResult = result;
            }
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer capture: nothing to report.
        }
        catch (Exception) when (!cancellation.IsCancellationRequested)
        {
            // The token check keeps a failure that raced the cancellation
            // (for example the previous crop being disposed while the
            // engine was still reading it) from surfacing as an error.
            MessageBox.Show(
                "Could not read the text in the captured region.\n\nTry again.",
                "CapParse",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        finally
        {
            if (ReferenceEquals(_ocrCancellation, cancellation))
            {
                _ocrCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private void RegisterCaptureHotkey()
    {
        _hotkey = new GlobalHotkey(StartCapture);

        // Single attempt: if another application owns this shortcut, CapParse
        // keeps working through the tray instead of retrying in a loop.
        if (!_hotkey.Register(GlobalHotkey.Modifier.Control | GlobalHotkey.Modifier.Shift, GlobalHotkey.KeyX))
        {
            MessageBox.Show(
                "CapParse could not register the global hotkey Ctrl + Shift + X.\n" +
                "Another application may already be using this shortcut.\n\n" +
                "Capture is still available from the tray icon.",
                "CapParse",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "CapParse",
            Visible = true
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Capture", null, (_, _) => StartCapture());
        menu.Items.Add("Settings", null, (_, _) => ShowSettings());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());
        _trayIcon.ContextMenuStrip = menu;

        // Left double-click on the tray icon starts the capture workflow.
        _trayIcon.DoubleClick += (_, _) => StartCapture();
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        var resource = Application.GetResourceStream(new Uri("Assets/tray.ico", UriKind.Relative));
        return new System.Drawing.Icon(resource.Stream);
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_isShuttingDown)
        {
            return;
        }

        // Closing the window only hides it; the app stays available from the tray.
        e.Cancel = true;
        _mainWindow?.Hide();
    }

    private void ShowSettings()
    {
        // Placeholder: the Settings window arrives in M13.
        MessageBox.Show(_mainWindow, "Settings are not implemented yet.", "CapParse",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExitApplication()
    {
        _isShuttingDown = true;

        if (_trayIcon is not null)
        {
            var icon = _trayIcon.Icon;
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
            icon?.Dispose();
        }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Stop any in-flight recognition before its engine is released
        // below. The library cancels between its pipeline stages, so the
        // native inference is normally interrupted promptly; if it is
        // still running when the process terminates, the process exit is
        // the final cleanup.
        _ocrCancellation?.Cancel();
        _ocrCancellation?.Dispose();
        _ocrCancellation = null;

        // End any active capture session before the process terminates.
        _overlaySession?.Dispose();
        _overlaySession = null;

        // Release the OCR engine (and the models it loaded) if it was
        // ever created.
        _ocrCoordinator.Dispose();

        // Release the last crop; the session dispose above may have just
        // stored a new one through the callback.
        _lastCrop?.Dispose();
        _lastCrop = null;

        // Release the global hotkey before the process terminates.
        _hotkey?.Dispose();
        _hotkey = null;

        base.OnExit(e);
    }
}
