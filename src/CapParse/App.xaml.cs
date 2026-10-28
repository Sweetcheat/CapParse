using System.ComponentModel;
using System.IO;
using System.Windows;
using CapParse.Capture;
using CapParse.Configuration;
using CapParse.OCR;
using CapParse.Platform;
using CapParse.Themes;
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
    // The persisted settings, loaded once at construction. A missing or
    // invalid file yields the defaults, so this never throws.
    private readonly AppSettings _settings = AppSettings.Load();
    // Resolves the theme preference (V0.2) and applies the matching
    // token dictionary to the application resources. The resolver
    // consults the Windows "apps" light/dark setting only when the
    // preference is System.
    private readonly ThemeService _themeService =
        new(new ThemeResolver(WindowsSystemTheme.CurrentAppTheme));
    // The OCR engine is created lazily on the first recognition, so
    // starting the app costs nothing; the coordinator releases it on exit.
    private readonly OcrCoordinator _ocrCoordinator = new();
    // Explicit state of the OCR runs; the main window is a view of it.
    private readonly OcrRunState _ocrState = new();
    private WinForms.NotifyIcon? _trayIcon;
    // TEMPORARY diagnostics (opt-in CAPPARSE_TRACE): hidden message window
    // that logs the shell broadcasts which make WinForms' NotifyIcon
    // remove and re-add the tray icon. Created in OnStartup only when the
    // trace channel is active; it never affects behavior (see
    // TrayBroadcastTraceWindow).
    private WinForms.NativeWindow? _traceBroadcastWindow;
    // The tray icon's context menu (its items and their callbacks). The
    // "Capturar" item's enabled state is kept in sync with the mode that
    // is effectively in force (see UpdateTrayCaptureItem), so the direct
    // mode disables it visually and its guarded callback can never start
    // a traditional capture.
    private TrayMenu? _trayMenu;
    private MainWindow? _mainWindow;
    private SettingsWindow? _settingsWindow;
    private GlobalHotkey? _hotkey;
    // The operational owner of the traditional capture hotkey's
    // registration (the combination it is registered with, or null): it
    // starts at startup according to the mode, follows the settings
    // window's pending mode while the window is open (a pending direct
    // mode suspends it for the duration of the shortcut mapping), and is
    // the single source the views read through ActiveHotkey. Created in
    // OnStartup, like the other services that need the UI thread.
    private CaptureHotkeyCoordinator? _captureHotkey;
    // The open settings window's pending direct mode (its toggle), or
    // false when no settings window is open: the traditional capture's
    // callback guard reads it, so a press of the traditional combination
    // while the window holds a pending direct mode can never start a
    // capture. Kept in the application — not the window — because the
    // guard runs in the hotkey callback, outside the window.
    private bool _settingsPendingDirectMode;
    private FrozenOverlaySession? _overlaySession;
    // The application's own transient confirmation (the toast): one
    // instance, created lazily on the first confirmed result and reused
    // for every later one (a new confirmation replaces the current one in
    // place). It is the single presentation of a capture operation's
    // result: the legacy tray balloon no longer mirrors a capture result
    // (presenting the same operation twice on screen was the duplication
    // fixed in §73) — it is kept only for the first-run mode notice.
    // Closed in ExitApplication so it can never outlive the process.
    private CaptureToastWindow? _toastWindow;
    // The last result that was confirmed (shown in the toast and
    // recorded for the tray tooltip's later query), or null while no
    // operation has produced a result. Only real results are recorded
    // here: a cancelled operation records nothing, and a later result
    // always replaces the earlier line, so the tooltip can never keep a
    // stale success standing after a failure.
    private CaptureConfirmation? _lastConfirmation;
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
    // The services the direct capture actions use (direct mode). The
    // runner owns its own stateless clipboard and save-dialog services
    // (the main window keeps its copies); its recognition delegate is the
    // same core the traditional flow uses (see RunRecognitionAsync), so a
    // direct OCR run updates the shared run state and history exactly like
    // any other run — there is no second OCR pipeline. Its save delegate
    // composes the configured save policy at the moment of the action
    // (dialog, or the configured folder), so a settings change applies to
    // the next action without a restart. Created in OnStartup because a
    // field initializer cannot bind its delegates to this instance's
    // methods.
    private DirectActionRunner? _directRunner;
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
    /// The traditional capture hotkey that is currently registered
    /// globally, or null when no combination could be registered (or the
    /// direct mode is in force, which keeps it unregistered for the whole
    /// session). Views read this to display the active shortcut.
    /// </summary>
    public HotkeySetting? ActiveHotkey => _captureHotkey?.Active;

    /// <summary>
    /// A full copy of the persisted settings: the settings window starts
    /// from this snapshot (its pending change) and commits it back through
    /// <see cref="ApplySettings"/> only when the user saves.
    /// </summary>
    public AppSettings SavedSettingsSnapshot() => _settings.Clone();

    /// <summary>
    /// The operational state of a direct-mode slot (see
    /// <see cref="DirectSlotState"/>): registered with the OS, and with
    /// which combination, right now. Views read this to note a configured
    /// slot whose combination could not be registered.
    /// </summary>
    public DirectSlotState DirectSlotOperationalState(int slot)
    {
        var registered = _hotkey is not null && _hotkey.IsRegistered(slot);
        return registered
            ? new DirectSlotState(true, _settings.DirectSlot(slot))
            : DirectSlotState.Off;
    }

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

        // Diagnostics (opt-in CAPPARSE_TRACE): record the channel's state
        // at the earliest point. The log file's very existence at startup
        // confirms the process inherited the variable (a missing file after
        // launch means it did not reach the process — variable set in
        // another shell, GUI launch, user/machine scope), and the echoed
        // path exposes typos such as a literal %TEMP%.
        var tracePath = Environment.GetEnvironmentVariable("CAPPARSE_TRACE");
        if (tracePath is not null)
        {
            FrozenOverlaySession.Trace($"App: CAPPARSE_TRACE active, path '{tracePath}'");
        }

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

        // The theme is resolved and applied before any window is created,
        // so no window is ever shown with the default (unresolved) colors.
        _themeService.Apply(_settings.ThemePreference);

        var imageSaver = new CropImageSaver(new WpfCropImageSaveDialog());

        _directRunner = new DirectActionRunner(
            new CropImageCopier(new WpfImageClipboard()),
            // The raw Win32 writer is the application's single
            // text-clipboard channel (shared with the traditional mode,
            // through MainWindow's own OcrTextCopier): it puts the same
            // CF_UNICODETEXT data the WPF (OLE) path would, without that
            // path's internal 10 × 100 ms retry loop on the UI thread —
            // the loop that, stacked with the outer policy, turned an
            // unopenable clipboard into the multi-second stall of the
            // direct-mode "copy text" defect (see docs/development.md
            // §76).
            new OcrTextCopier(new Win32TextClipboard()),
            // The save policy is consulted when the action runs, not when
            // the runner is created: a settings change (manual/automatic,
            // the folder) applies to the next action without a restart.
            (crop, dpi) => _settings.DirectSavePolicy == ImageSavePolicy.Automatic
                ? imageSaver.TrySaveToFolder(crop, dpi, _settings.DirectSaveFolder)
                : imageSaver.TrySave(crop, dpi),
            crop => RunRecognitionAsync(crop),
            OnDirectFeedback);

        _mainWindow = new MainWindow();
        _mainWindow.Closing += OnMainWindowClosing;
        _mainWindow.Show();

        CreateTrayIcon();

        // TEMPORARY diagnostics (opt-in CAPPARSE_TRACE): watch the shell
        // broadcasts that make WinForms re-add the tray icon (a re-add
        // closes a displayed balloon), so a trace can attribute an early
        // close to that path instead of to the shell closing the balloon
        // on its own. Created only when the trace channel is active.
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CAPPARSE_TRACE")))
        {
            _traceBroadcastWindow = new TrayBroadcastTraceWindow();
            // No parent, no WS_VISIBLE: a hidden top-level window, enough
            // to receive the shell's session-wide broadcasts.
            _traceBroadcastWindow.CreateHandle(new WinForms.CreateParams());
        }

        // The mode decides which hotkey family the startup registers: in
        // direct mode the traditional capture hotkey stays unregistered —
        // its saved combination is kept in the settings (free to be
        // assigned to a direct slot) and only the direct slots are
        // registered, so a launch that is already configured for direct
        // mode never starts a traditional capture by itself. One hidden
        // window serves every slot in both modes.
        _hotkey = new GlobalHotkey();
        _captureHotkey = new CaptureHotkeyCoordinator(
            TryRegisterHotkey,
            () => _hotkey?.Unregister(HotkeySlots.Capture));

        if (HotkeyTransaction.RegisterCaptureAtStartup(_settings))
        {
            RegisterCaptureHotkey();
        }

        RegisterDirectHotkeys();
        ShowFirstRunPresentationIfDue();
    }

    /// <summary>
    /// Registers the direct-mode hotkeys at startup: only when the direct
    /// mode is enabled and only the configured combinations — a legacy
    /// installation (mode disabled, or no combination configured) registers
    /// nothing, so an update never activates the direct mode on its own.
    /// A combination that is taken by another application is left
    /// unregistered (the configured value is preserved in the settings);
    /// a single notice lists every combination that could not be
    /// registered, and the remaining hotkeys keep working.
    /// </summary>
    private void RegisterDirectHotkeys()
    {
        if (!_settings.DirectModeEnabled)
        {
            return;
        }

        var failures = new List<HotkeySetting>();
        foreach (var slot in HotkeySlots.Direct)
        {
            var configured = _settings.DirectSlot(slot);
            if (configured is null)
            {
                continue;
            }

            if (!TryRegisterDirectHotkey(slot, configured))
            {
                failures.Add(configured);
            }
        }

        if (failures.Count > 0)
        {
            MessageBox.Show(
                $"O(s) atalho(s) {string.Join(", ", failures.Select(f => f.DisplayString))} não puderam ser registrados agora.\n" +
                "Outro aplicativo pode já estar usando essa(s) combinação(ões).\n\n" +
                "Os demais atalhos continuam ativos.",
                "CapParse",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// Shows the short, non-mandatory first-run presentation (ui.md §23)
    /// exactly once: the flag is marked as soon as the presentation is
    /// shown, so a later launch - even if the window was never dismissed -
    /// never shows it again. A settings write failure is ignored: the
    /// presentation simply appears one more time on the next launch, which
    /// is a harmless degradation, never a startup failure. The mode the
    /// user chooses in the presentation is applied when the window closes
    /// (see <see cref="OnFirstRunModeChosen"/>); closing it without choosing
    /// changes nothing.
    /// </summary>
    private void ShowFirstRunPresentationIfDue()
    {
        if (_settings.FirstRunPresented)
        {
            return;
        }

        _settings.FirstRunPresented = true;
        _settings.TrySave();

        var howTo = ActiveHotkey is { } active
            ? $"Inicie uma captura com {active.DisplayString}, ou pelo menu do ícone na bandeja (Capturar)."
            : "Inicie uma captura pelo menu do ícone na bandeja (Capturar).";

        var window = new FirstRunWindow(howTo);
        window.Closed += (_, _) => OnFirstRunModeChosen(window);
        window.Show();
    }

    /// <summary>
    /// Applies the mode chosen in the first-run presentation through the
    /// existing settings transaction (the same one the settings window
    /// uses): the traditional mode and a deferred decision keep
    /// <c>DirectModeEnabled</c> at the value of a new installation
    /// (disabled), so they change nothing and register nothing; the direct
    /// mode flips the flag through <see cref="ApplySettings"/> — the
    /// presentation never creates a hotkey on its own, so the transaction
    /// registers nothing and only persists the flag. The user is then
    /// pointed at the settings to configure the direct shortcuts.
    /// </summary>
    private void OnFirstRunModeChosen(FirstRunWindow window)
    {
        // A choice made while the application is shutting down is dropped:
        // the process is terminating, and the mode of a dead session is
        // meaningless.
        if (Dispatcher.HasShutdownStarted
            || window.Choice != FirstRunModeChoice.Direct)
        {
            return;
        }

        var pending = _settings.Clone();
        pending.DirectModeEnabled = true;
        ApplySettings(pending);

        // A rejection cannot happen here: no direct hotkey is configured at
        // first run (no conflict) and the save policy is the dialog one
        // (no folder to validate). When the file could not be written the
        // mode stays active for this session — the same degradation the
        // settings window reports — and the next launch re-shows the
        // presentation because the flag was not persisted.
        ShowTrayNotification(
            "CapParse",
            "Modo Direto ativado. Configure os atalhos em Configurações para usá-lo.",
            WinForms.ToolTipIcon.Info);
    }

    private async void StartCapture(DirectAction action = DirectAction.None)
    {
        // While a capture attempt is in flight (GDI capture, session
        // construction, active overlays) further triggers are ignored
        // until it ends (Idle -> Capturing). The async session construction
        // yields the UI thread between its steps, so a second trigger can
        // arrive in the middle of it: this guard, set before the attempt
        // starts, is what keeps it from reentering. The guard is shared by
        // the traditional flow and the direct actions: no second capture
        // of any kind can start while one is in flight.
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

            var session = await FrozenOverlaySession.CreateAsync(result, (selection, crop) =>
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
                    // The action this capture was started with rides in
                    // this closure — explicitly, so the capture can never
                    // execute the action of another trigger.
                    if (action == DirectAction.None)
                    {
                        RecognizeCropAsync(crop);

                        // The traditional flow confirms the capture itself
                        // (see CaptureConfirmationPolicy): the crop exists
                        // and the result window owns the image from this
                        // moment. The message claims exactly that — not
                        // the OCR, which is still running, and not a
                        // file, which this mode never writes.
                        ShowConfirmation(CaptureConfirmationPolicy.ForTraditionalCapture(true));
                    }
                    else
                    {
                        // The direct flow confirms through its own
                        // feedback (OnDirectFeedback), which reports only
                        // what the action really produced.
                        HandleDirectActionAsync(action, selection, crop);
                    }
                }
                else
                {
                    // No crop (cancelled or empty selection): nothing was
                    // produced, so nothing is confirmed — a cancelled
                    // operation never shows a success message. The OCR
                    // state is untouched, so publish it explicitly to let
                    // the views drop the now-invalidated image.
                    PublishOcrState();
                }
            });

            // A deferred close (Esc during the async construction) may have
            // already run the callback above: in that case no live session
            // remains, so do not keep a stale reference.
            _overlaySession = session.Closed ? null : session;
            FrozenOverlaySession.Trace("StartCapture: session constructed");
        }
        catch
        {
            // The session cleaned up after itself (overlays closed, captured
            // bitmap disposed, closed callback run). Reset the guard here
            // too, so a failed attempt can never leave the app permanently
            // blocked. Show a user-friendly message with a retry; technical
            // details are intentionally not shown.
            _overlaySession = null;
            _captureInFlight = false;

            // The attempt failed: record it as the last result (the tray
            // tooltip is the later query) without a second visual — the
            // dialog below is the user-facing feedback for this failure,
            // and a toast under a modal dialog would only be noise.
            NoteConfirmation(CaptureConfirmationPolicy.ForFailedCapture());

            // A failure that raced the app's own shutdown (the tray "Sair"
            // can be processed while a session step is in flight) must not
            // pop an error dialog over an app that is quitting: the user
            // asked to exit, not to capture.
            if (!Dispatcher.HasShutdownStarted)
            {
                // WPF's MessageBox has no Retry button; OK is the retry
                // (Cancel dismisses the message and keeps the app idle).
                var response = MessageBox.Show(
                    "Não foi possível capturar a tela.\n\nPressione OK para tentar novamente.",
                    "CapParse",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Warning);

                if (response == MessageBoxResult.OK)
                {
                    StartCapture();
                }
            }
        }
    }

    /// <summary>
    /// Runs the OCR engine over the crop of a finished capture without
    /// blocking the UI thread: the engine is created lazily (first use
    /// only) and the recognition itself is asynchronous.
    ///
    /// The run goes through the shared core <see cref="RunRecognitionAsync"/>,
    /// which tracks it in <see cref="OcrState"/> by the id
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
        // The final session cleanup of an exiting application can run the
        // closed callback on the UI thread during shutdown: do not start a
        // new recognition (and a possible error dialog) in an app that is
        // quitting.
        if (Dispatcher.HasShutdownStarted)
        {
            return;
        }

        var outcome = await RunRecognitionAsync(crop);

        if (outcome.Failed && !Dispatcher.HasShutdownStarted)
        {
            MessageBox.Show(
                "Não foi possível reconhecer o texto na região capturada.\n\nTente novamente.",
                "CapParse",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// The recognition core shared by the traditional flow and the direct
    /// actions: it starts the run in <see cref="OcrState"/>, cancels any
    /// recognition already in flight, recognizes the crop and records the
    /// result in <see cref="LastOcrResult"/> and <see cref="History"/> when
    /// the run validly completes. Returns what happened to this run
    /// specifically: its result, a failure (only when this run is the one
    /// that recorded it), or neither when it was superseded or cancelled.
    /// The direct actions consume this outcome to decide their feedback; a
    /// superseded run must stay silent.
    /// </summary>
    private async Task<OcrRunOutcome> RunRecognitionAsync(System.Drawing.Bitmap crop)
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
            // Diagnostics (opt-in CAPPARSE_TRACE): the recognition duration
            // of the shared core, so a trace can compare the OCR stage of
            // the direct and the traditional flows. No image data is
            // recorded, only the size of the crop.
            var ocrStartedAt = Environment.TickCount64;
            FrozenOverlaySession.Trace(
                $"RunRecognitionAsync: started (crop {crop.Width}x{crop.Height}, run {runId})");

            var result = await _ocrCoordinator.RecognizeAsync(
                new ImageData(crop), cancellation.Token);
            FrozenOverlaySession.Trace(
                $"RunRecognitionAsync: engine finished after " +
                $"{Environment.TickCount64 - ocrStartedAt} ms " +
                $"(text chars: {result.Text.Length}, cancelled: {cancellation.IsCancellationRequested})");

            if (cancellation.IsCancellationRequested)
            {
                return new OcrRunOutcome(null, false);
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
                return new OcrRunOutcome(result, false);
            }

            // Superseded by a newer run: it owns the state now.
            return new OcrRunOutcome(null, false);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer capture: nothing to report.
            return new OcrRunOutcome(null, false);
        }
        catch (Exception) when (!cancellation.IsCancellationRequested)
        {
            // The token check keeps a failure that raced the cancellation
            // (for example the previous crop being disposed while the
            // engine was still reading it) from surfacing as an error.
            // Only this run failing it — being the most recent — makes the
            // outcome a failure; a run that was superseded in the meantime
            // must not be reported as one.
            if (_ocrState.FailRun(runId))
            {
                if (!Dispatcher.HasShutdownStarted)
                {
                    PublishOcrState();
                }

                return new OcrRunOutcome(null, true);
            }

            return new OcrRunOutcome(null, false);
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
    /// Runs a direct capture action (direct mode) on the finished
    /// selection: the action happens right away, with tray feedback
    /// instead of the result window. The CopyText action goes through the
    /// same recognition core as the traditional flow, so the shared run
    /// state and history update exactly as they do for any capture. A
    /// cancelled selection (no crop) never reaches here: the session
    /// callback starts the action only when a crop exists.
    /// </summary>
    private async void HandleDirectActionAsync(
        DirectAction action,
        System.Drawing.Rectangle? selection,
        System.Drawing.Bitmap crop)
    {
        // The runner is created in OnStartup; the direct actions can only
        // be triggered through the hotkeys and tray created there, so this
        // is a defensive check, not an expected path.
        var runner = _directRunner;
        if (runner is null || Dispatcher.HasShutdownStarted)
        {
            return;
        }

        // The same DPI convention the result window's buttons use: the
        // effective DPI of the monitor the selection came from, 96 when the
        // selection is unknown.
        var dpi = selection is { } s
            ? MonitorDiscovery.GetEffectiveDpiForSelection(s)
            : 96;

        // Diagnostics (opt-in CAPPARSE_TRACE): the whole direct-action
        // span (action start to the last stage) — the clipboard stages
        // inside it are timed by the copier and the runner themselves.
        var startedAt = Environment.TickCount64;
        FrozenOverlaySession.Trace(
            $"HandleDirectActionAsync: action {action} started " +
            $"(selection {selection?.ToString() ?? "none"}, dpi {dpi})");

        try
        {
            await runner.RunAsync(action, crop, dpi);
            FrozenOverlaySession.Trace(
                $"HandleDirectActionAsync: action {action} finished after " +
                $"{Environment.TickCount64 - startedAt} ms");
        }
        catch
        {
            // The runner reports the failures of the services it wraps as
            // feedback; anything escaping it is unexpected (for example a
            // crop disposed by a newer capture mid-action) and must not
            // take the application down.
            OnDirectFeedback(new DirectFeedback(
                DirectFeedbackKind.Error,
                "Não foi possível concluir a ação."));
        }
    }

    /// <summary>
    /// Presents a direct action's feedback through the application's own
    /// transient confirmation (the toast) — the single presentation of
    /// the action's result: the legacy tray balloon is fire-and-forget
    /// (observed to be missed and to close early, see
    /// docs/development.md §71.3), and presenting the same operation
    /// through a second channel shows it twice on screen (the duplication
    /// fixed in §73). The balloon is kept for the first-run mode notice,
    /// which is an independent event, not a capture result. A notice that
    /// cannot be shown (for example during shutdown) is dropped silently:
    /// the action itself already happened, and the application must never
    /// fail because of its own feedback.
    /// </summary>
    private void OnDirectFeedback(DirectFeedback feedback)
    {
        if (Dispatcher.HasShutdownStarted)
        {
            return;
        }

        ShowConfirmation(CaptureConfirmation.FromDirectFeedback(feedback));
    }

    /// <summary>
    /// Confirms a real result of a capture operation: it is recorded as
    /// the last result (the tray tooltip is the later query — the toast
    /// disappears, the tooltip stays) and shown as the application's own
    /// transient confirmation. A new confirmation replaces any previous
    /// one in the same window, so an older message can never survive a
    /// newer operation's result. A null confirmation (nothing to
    /// communicate) is dropped, as is a result that cannot be shown (for
    /// example during shutdown): the operation itself already happened,
    /// and the application must never fail because of its own feedback.
    /// Call on the UI thread.
    /// </summary>
    private void ShowConfirmation(CaptureConfirmation? confirmation)
    {
        if (confirmation is null || Dispatcher.HasShutdownStarted)
        {
            return;
        }

        _lastConfirmation = confirmation;
        UpdateTrayTooltip();

        try
        {
            _toastWindow ??= new CaptureToastWindow();
            _toastWindow.Present(confirmation);
            FrozenOverlaySession.Trace(
                $"Confirmation: toast shown (kind {confirmation.Kind}, message '{confirmation.Message}')");
        }
        catch (Exception exception)
        {
            // The confirmation is best-effort feedback: the result is
            // already recorded in the tray tooltip, and the application
            // must not crash because of its own feedback (the same
            // degradation the tray balloon applies). The original
            // exception goes to the opt-in CAPPARSE_TRACE log.
            FrozenOverlaySession.Trace(
                $"Confirmation: toast failed to show (kind {confirmation.Kind}): {exception}");
        }
    }

    /// <summary>
    /// Records a result for the tray tooltip without the transient toast:
    /// for results that are already communicated by a modal message (the
    /// capture failure's dialog), so the later query is truthful without
    /// piling a second, redundant visual on top of it. A null
    /// confirmation records nothing. Call on the UI thread.
    /// </summary>
    private void NoteConfirmation(CaptureConfirmation? confirmation)
    {
        if (confirmation is null || Dispatcher.HasShutdownStarted)
        {
            return;
        }

        _lastConfirmation = confirmation;
        UpdateTrayTooltip();
        FrozenOverlaySession.Trace(
            $"Confirmation: recorded for the tray tooltip (kind {confirmation.Kind}, message '{confirmation.Message}')");
    }

    private void ShowTrayNotification(
        string title,
        string message,
        WinForms.ToolTipIcon icon)
    {
        // Diagnostic guards (opt-in CAPPARSE_TRACE): without these lines a
        // dropped notice leaves no trace, and the failure shapes (absent
        // icon, hidden icon, swallowed exception) are indistinguishable in
        // the log from a balloon the shell suppressed on its own.
        if (_trayIcon is null)
        {
            FrozenOverlaySession.Trace(
                $"ShowTrayNotification: tray icon absent, notice '{title}' dropped");
            return;
        }

        if (!_trayIcon.Visible)
        {
            FrozenOverlaySession.Trace(
                $"ShowTrayNotification: tray icon not visible, notice '{title}' dropped");
            return;
        }

        try
        {
            FrozenOverlaySession.Trace(
                $"ShowTrayNotification: calling ShowBalloonTip (title '{title}', icon {icon}, duration 3000 ms)");
            _trayIcon.ShowBalloonTip(3000, title, message, icon);
            // The call returning is NOT proof the user saw the balloon:
            // ShowBalloonTip is fire-and-forget and the shell may still
            // suppress the display. The BalloonTipShown event (subscribed
            // in CreateTrayIcon) is the only display evidence.
            FrozenOverlaySession.Trace(
                $"ShowTrayNotification: ShowBalloonTip returned (title '{title}')");
        }
        catch (Exception exception)
        {
            // ShowBalloonTip can fail when the tray is unavailable (for
            // example while the application is shutting down): dropping the
            // notice is the right degradation, never a crash. The original
            // exception is recorded in the opt-in CAPPARSE_TRACE log so a
            // failed balloon can be diagnosed without exposing technical
            // details in the interface.
            FrozenOverlaySession.Trace(
                $"ShowTrayNotification: ShowBalloonTip threw (title '{title}'), " +
                $"HRESULT {exception.HResult:X8}: {exception}");
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
        // Called from OnStartup right after the coordinator is created; the
        // guard is defensive, not an expected path.
        var coordinator = _captureHotkey;
        if (coordinator is null)
        {
            return;
        }

        var configured = _settings.CaptureHotkey;
        coordinator.Start(_settings);

        if (coordinator.Active is not null && coordinator.Active.Equals(configured))
        {
            return;
        }

        if (coordinator.Active is not null)
        {
            // The saved combination is unavailable right now (another
            // application owns it); the default is active instead.
            MessageBox.Show(
                $"O atalho salvo ({configured.DisplayString}) não pôde ser registrado agora.\n" +
                $"Outro aplicativo pode já estar usando esse atalho.\n\n" +
                $"O atalho padrão ({HotkeySetting.Default.DisplayString}) está ativo.",
                "CapParse",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Nothing could be registered: the app keeps working through the
        // tray instead of retrying in a loop.
        MessageBox.Show(
            $"O CapParse não conseguiu registrar o atalho global {configured.DisplayString}.\n" +
            "Outro aplicativo pode já estar usando esse atalho.\n\n" +
            "A captura continua disponível pelo ícone da bandeja.",
            "CapParse",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private bool TryRegisterHotkey(HotkeySetting setting)
        => _hotkey is not null
            && _hotkey.Register(
                HotkeySlots.Capture,
                (GlobalHotkey.Modifier)setting.Modifiers,
                setting.Key,
                TryStartTraditionalCapture);

    /// <summary>
    /// The single entry point of a traditional capture: the traditional
    /// mode must be effectively in force — the committed mode is
    /// traditional and the open settings window is not holding a pending
    /// direct mode (which suspends the hotkey registration for the
    /// duration of the shortcut mapping) — or the trigger is dropped. This
    /// is the second line of the isolation: a stale WM_HOTKEY queued
    /// before the suspension, a registration that should not exist, or a
    /// click on the tray menu's "Capturar" item (whose callback is this
    /// method, mirroring the tray item's disabled state) can never start
    /// a traditional capture while the direct mode is in effect. Both the
    /// traditional capture hotkey and the tray menu call it.
    /// </summary>
    private void TryStartTraditionalCapture()
    {
        if (!HotkeyTransaction.TraditionalCaptureAllowed(
            _settings.DirectModeEnabled, _settingsPendingDirectMode))
        {
            return;
        }

        StartCapture();
    }

    /// <summary>
    /// Registers a direct-mode slot with the given combination. The slot's
    /// action (copy image, OCR + copy text, save image) is bound at
    /// registration time, so the hotkey can never run the action of
    /// another slot.
    /// </summary>
    private bool TryRegisterDirectHotkey(int slot, HotkeySetting setting)
        => _hotkey is not null
            && _hotkey.Register(
                slot,
                (GlobalHotkey.Modifier)setting.Modifiers,
                setting.Key,
                () => StartCapture(ActionForSlot(slot)));

    /// <summary>The direct action a hotkey slot triggers.</summary>
    private static DirectAction ActionForSlot(int slot) => slot switch
    {
        HotkeySlots.DirectCopyImage => DirectAction.CopyImage,
        HotkeySlots.DirectCopyText => DirectAction.CopyText,
        HotkeySlots.DirectSaveImage => DirectAction.SaveImage,
        _ => DirectAction.None
    };

    /// <summary>
    /// The operational state of the direct-mode slots right now (what is
    /// registered with the OS): the snapshot a settings transaction takes
    /// before it changes anything, so a failed change can be rolled back
    /// to exactly this state.
    /// </summary>
    private DirectHotkeyState CurrentDirectState()
    {
        var state = new DirectHotkeyState();
        foreach (var slot in HotkeySlots.Direct)
        {
            state[slot] = DirectSlotOperationalState(slot);
        }

        return state;
    }

    /// <summary>The outcome of a settings application (the settings window's "Salvar").</summary>
    public enum SettingsApplyOutcome
    {
        /// <summary>The new settings are active and persisted.</summary>
        Applied,

        /// <summary>
        /// The new settings are active (in memory), but the file could not
        /// be written: they last for this session and revert on the next
        /// launch.
        /// </summary>
        SaveFailed,

        /// <summary>
        /// The change was rejected (validation) or a registration failed:
        /// the previous settings and registrations are kept, and nothing
        /// was persisted. <see cref="SettingsApplyResult.Message"/> carries
        /// the user-facing explanation.
        /// </summary>
        Rejected
    }

    /// <summary>
    /// The result of <see cref="ApplySettings"/>; <see cref="Message"/>
    /// carries the user-facing explanation of a rejection.
    /// </summary>
    public sealed record SettingsApplyResult(SettingsApplyOutcome Outcome, string? Message);

    /// <summary>
    /// Transactionally applies the settings the user chose in the settings
    /// window: the change is validated first (hotkey conflicts between the
    /// capture and the direct slots, the automatic save policy's folder),
    /// then applied (the capture hotkey through the recovery chain, the
    /// direct-mode registrations, the theme), and only then persisted.
    /// Any failure restores the operational state the application had
    /// before the attempt and persists nothing, so a failed change can
    /// never leave a partial configuration or an orphan registration
    /// behind. Call on the UI thread.
    /// </summary>
    public SettingsApplyResult ApplySettings(AppSettings pending)
    {
        ArgumentNullException.ThrowIfNull(pending);

        // 1) Validate before touching anything: a conflicting combination
        // would make one shortcut fire two actions, and the automatic
        // policy without a usable folder would fail on every save. Only
        // the hotkeys that will be active in the destination mode
        // participate in the check: in direct mode the direct slots check
        // against each other (the traditional capture hotkey is not
        // registered, so its saved combination does not block a direct
        // slot); in traditional mode the capture hotkey stands alone (the
        // direct slots are not registered), so a combination saved in
        // both modes is valid in both.
        var conflicts = HotkeyTransaction.FindConflicts(pending);

        if (conflicts.Count > 0)
        {
            return new SettingsApplyResult(
                SettingsApplyOutcome.Rejected,
                $"Dois atalhos usam a mesma combinação: {string.Join(", ", conflicts.Select(c => c.DisplayString))}.\n" +
                "Escolha combinações diferentes para aplicar as alterações.");
        }

        if (pending.DirectSavePolicy == ImageSavePolicy.Automatic
            && (string.IsNullOrWhiteSpace(pending.DirectSaveFolder) || !Directory.Exists(pending.DirectSaveFolder)))
        {
            return new SettingsApplyResult(
                SettingsApplyOutcome.Rejected,
                "A política de salvamento automático exige uma pasta de destino válida.\n" +
                "Escolha uma pasta existente ou use a política manual.");
        }

        // 2) Snapshot of the operational state, for the rollback.
        var beforeDirect = CurrentDirectState();
        var beforeCapture = _captureHotkey?.Active;

        // 3) Release the direct-mode registrations the destination state
        // does not want, before the capture side runs: RegisterHotKey
        // refuses a combination that any live registration of the
        // process already holds, so a direct slot that is about to go
        // off must give up its combination before the traditional capture
        // hotkey — which may carry the very same combination, the modes
        // being mutually exclusive — is registered. Without this release
        // first, the direct-to-traditional switch with a shared
        // combination would fail against the process's own registration
        // and roll back for no external reason. Unregistering cannot
        // fail; the rollback in step 6 restores the released slots.
        DirectHotkeyApplier.Release(
            pending,
            beforeDirect,
            slot => _hotkey?.Unregister(slot));

        // 4) The capture hotkey follows the mode (see
        // HotkeyTransaction.PlanCapture): direct mode unregisters it (the
        // saved combination stays in the settings and becomes free for a
        // direct slot); traditional mode registers the combination when it
        // changed, through the recovery chain. While the settings window
        // held a pending direct mode the registration is already
        // suspended, so the direct-mode plan is a no-op; unregistering
        // cannot fail, and a failed registration is rolled back with the
        // snapshot taken in step 2, and in direct mode that snapshot has
        // no capture registration, so the recovery chain can never bring
        // the traditional hotkey back while the mode is on.
        var captureOk = _captureHotkey is { } captureCoordinator
            ? captureCoordinator.ApplyPlan(_settings, pending)
            : true;
        PublishOcrState();

        // 5) The direct-mode registrations: disabled mode unregisters every
        // slot (the configured combinations are preserved in the settings);
        // enabled mode registers only the configured combinations. The slots
        // step 3 released are already off (re-releasing them is a no-op),
        // and in the other direction the capture hotkey is suspended by
        // step 4 before any direct slot is registered — so a registration
        // here can never collide with the process's own other family.
        var direct = DirectHotkeyApplier.Apply(
            pending,
            beforeDirect,
            (slot, combination) => TryRegisterDirectHotkey(slot, combination),
            slot => _hotkey?.Unregister(slot));

        // 6) Commit, or roll the whole transaction back.
        if (captureOk && direct.Succeeded)
        {
            CommitSettings(pending);

            return _settings.TrySave()
                ? new SettingsApplyResult(SettingsApplyOutcome.Applied, null)
                : new SettingsApplyResult(SettingsApplyOutcome.SaveFailed, null);
        }

        // A failure anywhere in the transaction: the direct slots and the
        // capture hotkey go back to the state they had before the attempt,
        // and the settings file is left untouched.
        DirectHotkeyApplier.Restore(
            beforeDirect,
            (slot, combination) => TryRegisterDirectHotkey(slot, combination),
            slot => _hotkey?.Unregister(slot));
        _captureHotkey?.Restore(beforeCapture);
        PublishOcrState();

        var message = !direct.Succeeded && direct.FailureMessage is { } failed
            ? $"A combinação {failed} não pôde ser registrada. Outro aplicativo pode já estar usando essa combinação.\n\n" +
              "As configurações anteriores continuam ativas."
            : "O novo atalho de captura não pôde ser registrado. Outro aplicativo pode já estar usando essa combinação.\n\n" +
              "As configurações anteriores continuam ativas.";

        return new SettingsApplyResult(SettingsApplyOutcome.Rejected, message);
    }

    /// <summary>
    /// Copies the committed settings into the application's live settings
    /// and applies the theme when it changed. Only called when the whole
    /// transaction succeeded, so a rejected change never re-skins the
    /// application.
    /// </summary>
    private void CommitSettings(AppSettings pending)
    {
        var themeChanged = pending.ThemePreference != _settings.ThemePreference;

        _settings.CaptureHotkey = pending.CaptureHotkey;
        _settings.FirstRunPresented = pending.FirstRunPresented;
        _settings.ThemePreference = pending.ThemePreference;
        _settings.DirectModeEnabled = pending.DirectModeEnabled;
        _settings.DirectCopyImageHotkey = pending.DirectCopyImageHotkey;
        _settings.DirectCopyTextHotkey = pending.DirectCopyTextHotkey;
        _settings.DirectSaveImageHotkey = pending.DirectSaveImageHotkey;
        _settings.DirectSavePolicy = pending.DirectSavePolicy;
        _settings.DirectSaveFolder = pending.DirectSaveFolder;

        // The committed mode just changed: the tray menu's "Capturar" item
        // and the tooltip follow it right away (no restart needed) —
        // saving is the only way the committed mode or the direct slots
        // change at runtime.
        UpdateTrayCaptureItem();
        UpdateTrayTooltip();

        if (themeChanged)
        {
            // The single, centralized way to change the theme: the windows
            // that consume the shared tokens update without a restart.
            _themeService.Apply(pending.ThemePreference);
        }
    }

    /// <summary>
    /// The settings window's pending direct-mode change (its toggle): the
    /// traditional capture hotkey follows the pending mode immediately —
    /// a pending direct mode suspends it for the duration of the shortcut
    /// mapping (so its combination can be recorded into a direct slot
    /// without starting a capture), a pending traditional mode restores it
    /// only when the committed mode is traditional. The saved combination
    /// is never touched. Call on the UI thread.
    /// </summary>
    internal void SettingsWindowPendingDirectModeChanged(bool pendingDirectMode)
    {
        _settingsPendingDirectMode = pendingDirectMode;
        _captureHotkey?.ApplyPendingMode(_settings, pendingDirectMode);
        UpdateTrayCaptureItem();
        PublishOcrState();
    }

    /// <summary>
    /// The settings window closed: the pending mode no longer exists, and
    /// the traditional capture hotkey goes back to the state the committed
    /// mode prescribes (registered in traditional mode, unregistered in
    /// direct mode). A canceled session thus never leaves a suspended
    /// registration behind, and a saved mode needs no re-application. Call
    /// on the UI thread.
    /// </summary>
    internal void SettingsWindowClosed()
    {
        _settingsPendingDirectMode = false;
        _captureHotkey?.SyncWithCommittedMode(_settings);
        UpdateTrayCaptureItem();
        PublishOcrState();
    }

    private void CreateTrayIcon()
    {
        // The "Capturar" item calls the same guarded entry point the
        // traditional capture hotkey uses: while the direct mode is in
        // force the guard refuses the capture, whatever the item's visual
        // state a stale trigger may see.
        _trayMenu = new TrayMenu(
            TryStartTraditionalCapture,
            ShowSettings,
            ExitApplication);

        _trayIcon = new WinForms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Text = "CapParse",
            ContextMenuStrip = _trayMenu.Menu,
            Visible = true
        };

        // Left double-click on the tray icon restores the main window
        // (the capture workflow stays on the global hotkey and the menu).
        _trayIcon.DoubleClick += (_, _) => RestoreMainWindow();

        FrozenOverlaySession.Trace("TrayIcon: created (visible)");

        // Diagnostics (opt-in CAPPARSE_TRACE): ShowBalloonTip is
        // fire-and-forget, so these events are the only evidence of
        // whether the shell actually displayed a balloon (or suppressed
        // it). Subscribed once here, where the icon is created.
        _trayIcon.BalloonTipShown += (_, _) =>
            FrozenOverlaySession.Trace(
                "TrayIcon: BalloonTipShown (shell displayed the balloon)");
        _trayIcon.BalloonTipClosed += (_, _) =>
            FrozenOverlaySession.Trace("TrayIcon: BalloonTipClosed");
        // Without this line a user click on the balloon is invisible in
        // the trace (a click closes the balloon and is the only benign
        // explanation for an early close).
        _trayIcon.BalloonTipClicked += (_, _) =>
            FrozenOverlaySession.Trace("TrayIcon: BalloonTipClicked (user clicked the balloon)");

        // The "Capturar" item starts reflecting the mode that is
        // effectively in force at startup (enabled in the traditional
        // mode, disabled in the direct mode), and the tooltip starts
        // reflecting it too.
        UpdateTrayCaptureItem();
        UpdateTrayTooltip();
    }

    /// <summary>
    /// Keeps the tray menu's "Capturar" item in sync with the mode that is
    /// effectively in force: disabled while the direct mode is in effect
    /// (committed, or pending in the open settings window — the same
    /// guard the traditional capture's entry point applies), enabled in
    /// the traditional mode. The item's callback applies the very same
    /// guard, so the visual state and the action can never disagree. Call
    /// on the UI thread, whenever the committed mode or the pending mode
    /// changes.
    /// </summary>
    private void UpdateTrayCaptureItem()
    {
        if (_trayMenu is { } menu)
        {
            menu.SetCaptureEnabled(HotkeyTransaction.TraditionalCaptureAllowed(
                _settings.DirectModeEnabled, _settingsPendingDirectMode));
        }
    }

    /// <summary>
    /// Keeps the tray icon's tooltip in sync with the committed state and
    /// the last confirmed result: while the direct mode is in force with
    /// no direct shortcut configured, the tooltip points at configuring
    /// the shortcuts (the nudge wins over a stale result line); otherwise
    /// the last confirmed result is named — the passive, always-available
    /// companion of the transient toast, which disappears after a few
    /// seconds; with no result yet, the plain application name is shown.
    /// It reflects the committed settings and the recorded result only: a
    /// pending mode in the open settings window is not in force until
    /// saved, so it never changes the tooltip. Call on the UI thread,
    /// whenever the committed mode, the direct slot configuration or the
    /// last confirmed result changes.
    /// </summary>
    private void UpdateTrayTooltip()
    {
        if (_trayIcon is not { } icon)
        {
            return;
        }

        var anySlotConfigured = HotkeySlots.Direct
            .Any(slot => _settings.DirectSlot(slot) is not null);

        icon.Text = DirectModeStatus.TrayTooltip(
            _settings.DirectModeEnabled,
            anySlotConfigured,
            _lastConfirmation?.Message);
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        // The approved mascot, shared with the main window icon: one WPF
        // Resource, no second copy of the identity. Icon(Stream) copies the
        // data, so the stream can be released right after construction; the
        // Icon itself is disposed in ExitApplication.
        var resource = Application.GetResourceStream(new Uri("Assets/capparse.ico", UriKind.Relative));
        using var stream = resource.Stream;
        return new System.Drawing.Icon(stream);
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

    /// <summary>
    /// Opens the settings window — called by the tray menu and by the
    /// main window's title-row button, so both routes share one entry
    /// point. One window at a time: a request while one is already open
    /// activates that window; it is not recreated and its state is not
    /// re-read (the window shows the state it was created with).
    /// </summary>
    internal void ShowSettings()
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closed += (_, _) =>
            {
                _settingsWindow = null;
                SettingsWindowClosed();
            };
            _settingsWindow.Show();
        }

        _settingsWindow.Activate();
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

            // Diagnostic: the only path that hides/disposes the icon. If a
            // trace shows a BalloonTipClosed right after this line, the
            // close came from the exit, not from the shell.
            FrozenOverlaySession.Trace("TrayIcon: hidden and disposed (application exit)");
        }

        if (_traceBroadcastWindow is not null)
        {
            _traceBroadcastWindow.ReleaseHandle();
            _traceBroadcastWindow = null;
        }

        // The toast window is the application's own: it is closed here so
        // it can never survive the process (a hidden toast left behind
        // would be an orphan window with no owner left to dismiss it).
        if (_toastWindow is not null)
        {
            _toastWindow.Close();
            _toastWindow = null;
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

        // Safety net for a shutdown that did not go through ExitApplication.
        _traceBroadcastWindow?.ReleaseHandle();
        _traceBroadcastWindow = null;

        // The toast window, like the trace window, must not outlive the
        // process: a shutdown path that skipped ExitApplication still
        // leaves no orphan window behind.
        _toastWindow?.Close();
        _toastWindow = null;

        base.OnExit(e);
    }

    /// <summary>
    /// TEMPORARY diagnostics (opt-in CAPPARSE_TRACE): a hidden top-level
    /// (message) window that logs the shell broadcasts which make
    /// WinForms' NotifyIcon remove and re-add the tray icon — a setting
    /// change, a display change, or a taskbar restart. A re-add while a
    /// balloon is displayed closes the balloon, so these lines let a
    /// trace attribute an early close to that path instead of to the
    /// shell closing the balloon on its own. Created only when
    /// CAPPARSE_TRACE is set (see OnStartup); it logs and never affects
    /// behavior. The message constants are the Win32 values used by the
    /// shell's broadcast: WM_SETTINGCHANGE, WM_DISPLAYCHANGE and
    /// WM_TASKBARCREATED (WM_USER + 7).
    /// </summary>
    private sealed class TrayBroadcastTraceWindow : WinForms.NativeWindow
    {
        private const int WmSettingChange = 0x001A;
        private const int WmDisplayChange = 0x007E;
        private const int WmTaskbarCreated = 0x0407;

        protected override void WndProc(ref WinForms.Message message)
        {
            switch (message.Msg)
            {
                case WmSettingChange:
                    // LParam names the setting that changed (ANSI string;
                    // null on some broadcasts). The name identifies what
                    // the shell reacted to right before a balloon closed.
                    string? setting = message.LParam is { } param && param != IntPtr.Zero
                        ? System.Runtime.InteropServices.Marshal.PtrToStringAnsi(param)
                        : null;
                    FrozenOverlaySession.Trace(
                        $"TrayIcon: WM_SETTINGCHANGE broadcast (setting '{setting}')");
                    break;

                case WmDisplayChange:
                    FrozenOverlaySession.Trace("TrayIcon: WM_DISPLAYCHANGE broadcast");
                    break;

                case WmTaskbarCreated:
                    FrozenOverlaySession.Trace(
                        "TrayIcon: WM_TASKBARCREATED broadcast (taskbar restarted; the icon is re-added, which closes a displayed balloon)");
                    break;
            }

            base.WndProc(ref message);
        }
    }
}
