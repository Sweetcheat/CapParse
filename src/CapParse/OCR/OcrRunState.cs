namespace CapParse.OCR;

/// <summary>
/// The presentation state of the application's OCR runs: what the
/// interface should show right now.
/// </summary>
public enum OcrPhase
{
    /// <summary>No capture has produced a result yet.</summary>
    Idle,

    /// <summary>A recognition is in progress.</summary>
    Processing,

    /// <summary>The last run produced text.</summary>
    Succeeded,

    /// <summary>The last run completed and found no text: a valid, empty
    /// result, not an error.</summary>
    NoText,

    /// <summary>The last run failed.</summary>
    Failed
}

/// <summary>
/// Explicit state of the OCR runs the application has performed. Each run
/// is identified by the id <see cref="BeginRun"/> returns; completion,
/// failure and cancellation only take effect for the most recent run, so
/// a stale (superseded or cancelled) run can never overwrite the state of
/// a newer one. Pure state: no threads, no timers, no UI dependencies.
/// </summary>
public sealed class OcrRunState
{
    private int _runId;

    public OcrRunState()
    {
        Phase = OcrPhase.Idle;
        Text = string.Empty;
    }

    /// <summary>
    /// The current phase.
    /// </summary>
    public OcrPhase Phase { get; private set; }

    /// <summary>
    /// The recognized text of the most recent successful run with text.
    /// Empty in every other phase.
    /// </summary>
    public string Text { get; private set; }

    /// <summary>
    /// True only when there is text to copy.
    /// </summary>
    public bool CanCopy => Phase == OcrPhase.Succeeded;

    /// <summary>
    /// Starts a new run and returns its id. The state moves to
    /// <see cref="OcrPhase.Processing"/> and any previous text is dropped,
    /// so an old result is never shown while a new run is in flight.
    /// </summary>
    public int BeginRun()
    {
        _runId++;
        Phase = OcrPhase.Processing;
        Text = string.Empty;
        return _runId;
    }

    /// <summary>
    /// Marks this run as finished with <paramref name="result"/>. An image
    /// without text is the valid <see cref="OcrPhase.NoText"/> outcome.
    /// </summary>
    /// <returns>False when <paramref name="runId"/> is not the most recent
    /// run (it was superseded or cancelled); the state is left untouched.</returns>
    public bool CompleteRun(int runId, OcrResult result)
    {
        // Only the most recent run may complete the state: any other id
        // is a stale (superseded or cancelled) run.
        if (runId != _runId || result is null)
        {
            return false;
        }

        Text = result.Text;
        Phase = result.Text.Length > 0 ? OcrPhase.Succeeded : OcrPhase.NoText;
        return true;
    }

    /// <summary>
    /// Marks this run as failed.
    /// </summary>
    /// <returns>False when <paramref name="runId"/> is not the most recent
    /// run; the state is left untouched.</returns>
    public bool FailRun(int runId)
    {
        // Only the most recent run may fail the state (see above).
        if (runId != _runId)
        {
            return false;
        }

        Phase = OcrPhase.Failed;
        Text = string.Empty;
        return true;
    }
}
