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
    // Explicit state of the OCR runs; the main window is a view of it.
    private readonly OcrRunState _ocrState = new();
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
    // The in-memory OCR history of the session. A run adds an entry only
    // when it completes validly (see RecognizeCropAsync), so a superseded or
    // cancelled run never records a stale result here.
    private readonly OcrHistory _history = new();
    // Single-instance guard: acquired on the UI thread in OnStartup and
    // released on the same thread in OnExit; the OS also reclaims the mutex
    // if the process terminates unexpectedly.
    private SingleInstanceGuard? _singleInstance;
    private bool _isShuttingDown;

    // Stable, session-scoped (no "Global\" prefix) name for the single-
    // instance mutex. The suffix lets the name evolve if the semantics ever
    // change, without colliding with an older build.
    private const string SingleInstanceName = "CapParse.SingleInstance.v1";

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

    /// <summary>
    /// The explicit state of the OCR runs (initial, processing, text,
    /// no text, failed). Views read it; only <see cref="RecognizeCropAsync"/>
    /// changes it.
    /// </summary>
    public OcrRunState OcrState => _ocrState;

    /// <summary>
    /// The in-memory OCR history of the session: the recognized results of
    /// previous captures, most recent first, at most
    /// <see cref="OcrHistory.MaxEntries"/> entries. Independent of
    /// <see cref="LastOcrResult"/> and <see cref="OcrState"/>: consulting a
    /// history entry never changes the current result or the run state. The
    /// main window is a view of this.
    /// </summary>
    public OcrHistory History => _history;

    /// <summary>
    /// Raised on the UI thread whenever <see cref="OcrState"/> changes, so
    /// views can refresh. Recognition completes on a thread-pool thread,
    /// so the publication is marshaled to the dispatcher.
    /// </summary>
    public event EventHandler? OcrStateChanged;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Single instance per user/session: acquire the guard on this (UI)
        // thread before any exclusive resource (window, tray, hotkey) is
        // created. A second instance shows a message and shuts down without
        // starting a functional instance.
        _singleInstance = new SingleInstanceGuard(SingleInstanceName);
        if (!_singleInstance.TryAcquire())
        {
            MessageBox.Show(
                "Já existe uma instância do CapParse em execução.",
                "CapParse",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

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
            FrozenOverlaySession.Trace("StartCapture: capturing desktop");
            var result = _captureCoordinator.StartCapture();
            FrozenOverlaySession.Trace("StartCapture: desktop captured");

            // TEMPORARY diagnostic (black-flash investigation): with
            // CAPPARSE_NO_OVERLAY=1 the desktop is captured and logged but
            // no overlay window is created at all. If the black frame still
            // appears in this mode it cannot come from the overlay windows.
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CAPPARSE_NO_OVERLAY")))
            {
                FrozenOverlaySession.Trace("StartCapture: NO_OVERLAY diagnostic mode, no session created");
                result.Dispose();
                _captureInFlight = false;
                return;
            }

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
            FrozenOverlaySession.Trace("StartCapture: session constructed");
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

            // A failure that raced the app's own shutdown (the tray "Sair"
            // can be processed while the session constructor pumps the UI
            // thread) must not pop an error dialog over an app that is
            // quitting: the user asked to exit, not to capture.
            if (!Dispatcher.HasShutdownStarted)
            {
                MessageBox.Show(
                    "Não foi possível capturar a tela.\n\nTente novamente.",
                    "CapParse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    /// <summary>
    /// Runs the OCR engine over the crop of a finished capture without
    /// blocking the UI thread: the engine is created lazily (first use
    /// only) and the recognition itself is asynchronous.
    ///
    /// The run is tracked in <see cref="OcrState"/> by the id
    /// <see cref="OcrRunState.BeginRun"/> returns: a recognition already in
    /// flight is cancelled first, and only the most recent run may complete
    /// or fail the state, so a stale (superseded or cancelled) recognition
    /// can never overwrite a newer one, and a cancellation never leaves the
    /// interface stuck in the processing state.
    ///
    /// Cancellation (a new capture superseded this one) is not an error and
    /// is reported silently. A real failure is shown in the window's state
    /// and with the same user-friendly message the capture flow uses; the
    /// captured text and image are never logged.
    /// </summary>
    private async void RecognizeCropAsync(System.Drawing.Bitmap crop)
    {
        // Called on the UI thread (the session's closed callback), so the
        // state move to Processing is observed by the window immediately.
        var runId = _ocrState.BeginRun();
        PublishOcrState();

        _ocrCancellation?.Cancel();
        _ocrCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _ocrCancellation = cancellation;

        try
        {
            var result = await _ocrCoordinator.RecognizeAsync(
                new ImageData(crop), cancellation.Token);

            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            if (_ocrState.CompleteRun(runId, result))
            {
                _lastOcrResult = result;

                // Only a run that validly completes the state records an
                // entry: a superseded or cancelled run returns false above
                // and is never added, and a run adds at most one entry, so
                // the history can never grow from stale or repeated results.
                _history.Add(runId, result.Text, DateTime.Now);

                PublishOcrState();
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
            if (_ocrState.FailRun(runId))
            {
                PublishOcrState();
                MessageBox.Show(
                    "Não foi possível reconhecer o texto na região capturada.\n\nTente novamente.",
                    "CapParse",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
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

    /// <summary>
    /// Notifies the views that <see cref="OcrState"/> changed. Recognition
    /// completes on a thread-pool thread, so the notification is marshaled
    /// to the WPF dispatcher: subscribers always run on the UI thread.
    /// </summary>
    private void PublishOcrState()
    {
        void Raise() => OcrStateChanged?.Invoke(this, EventArgs.Empty);

        if (Dispatcher.CheckAccess())
        {
            Raise();
        }
        else
        {
            Dispatcher.BeginInvoke(Raise);
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
                "O CapParse não conseguiu registrar o atalho global Ctrl + Shift + X.\n" +
                "Outro aplicativo pode já estar usando esse atalho.\n\n" +
                "A captura continua disponível pelo ícone da bandeja.",
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
        menu.Items.Add("Capturar", null, (_, _) => StartCapture());
        menu.Items.Add("Configurações", null, (_, _) => ShowSettings());
        menu.Items.Add("Sair", null, (_, _) => ExitApplication());
        _trayIcon.ContextMenuStrip = menu;

        // Left double-click on the tray icon restores the main window
        // (the capture workflow stays on the global hotkey and the menu).
        _trayIcon.DoubleClick += (_, _) => RestoreMainWindow();
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

    /// <summary>
    /// Restores the main window from the tray: a hidden window is shown again,
    /// a minimized one returns to the normal state, and the window is
    /// activated so the user can immediately return to the app. Double-clicking
    /// the tray icon calls this instead of starting a capture. It touches only
    /// the window's visibility/state and never the OCR result, history, or run
    /// state. Safe when no window exists: nothing is created and no capture is
    /// started.
    /// </summary>
    private void RestoreMainWindow()
    {
        // The NotifyIcon events fire on the UI thread, so the window can be
        // manipulated directly without a dispatcher hop.
        if (_mainWindow is null)
        {
            return;
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        _mainWindow.Activate();
    }

    private void ShowSettings()
    {
        // Placeholder: the Settings window arrives in M13.
        MessageBox.Show(_mainWindow, "As configurações ainda não foram implementadas.", "CapParse",
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

        // Release the single-instance mutex on the owning (UI) thread. The OS
        // would also reclaim it on process exit, so this additionally covers
        // the crash path.
        _singleInstance?.Dispose();
        _singleInstance = null;

        base.OnExit(e);
    }
}
