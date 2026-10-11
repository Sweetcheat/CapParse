namespace CapParse.OCR;

/// <summary>
/// One entry in the session's OCR history: a recognized result from a
/// previous capture. The values are fixed when the entry is created, so an
/// entry is immutable and can never be modified by a later capture.
/// </summary>
public sealed class OcrHistoryEntry
{
    private const int PreviewMaxLength = 80;

    public OcrHistoryEntry(int id, int runId, DateTime capturedAt, string text)
    {
        Id = id;
        RunId = runId;
        CapturedAt = capturedAt;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Preview = MakePreview(text);
        DisplayTime = capturedAt.ToString("dd/MM/yyyy HH:mm");
    }

    /// <summary>
    /// Stable identity of the entry, unique within the session.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// The id of the OCR run this entry came from. A run can produce at
    /// most one entry, so this is also the deduplication key.
    /// </summary>
    public int RunId { get; }

    /// <summary>
    /// The local date and time the capture was recorded.
    /// </summary>
    public DateTime CapturedAt { get; }

    /// <summary>
    /// The full recognized text of the capture.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// A single-line, truncated preview of <see cref="Text"/> for the list.
    /// </summary>
    public string Preview { get; }

    /// <summary>
    /// A human-friendly local timestamp for the list.
    /// </summary>
    public string DisplayTime { get; }

    private static string MakePreview(string text)
    {
        var singleLine = text
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Trim();

        return singleLine.Length <= PreviewMaxLength
            ? singleLine
            : string.Concat(singleLine.AsSpan(0, PreviewMaxLength), "…");
    }
}

/// <summary>
/// The in-memory OCR history of the current session. Entries are kept in
/// memory only (nothing is written to disk) and the list keeps at most
/// <see cref="MaxEntries"/> entries, most recent first.
///
/// An entry is added only for a run that recognized non-empty text, and a
/// run can add at most one entry (keyed by its run id), so a single run can
/// never appear twice no matter how many times it is reported. This type is
/// pure state: no threads, no timers, no UI dependencies. It is only read
/// and written on the UI thread.
/// </summary>
public sealed class OcrHistory
{
    /// <summary>
    /// The maximum number of entries kept in the history.
    /// </summary>
    public const int MaxEntries = 20;

    // Index 0 is always the most recent entry.
    private readonly List<OcrHistoryEntry> _entries = new();
    private readonly HashSet<int> _addedRunIds = new();
    private int _nextId = 1;

    /// <summary>
    /// True when the session has recorded no entry yet.
    /// </summary>
    public bool IsEmpty => _entries.Count == 0;

    /// <summary>
    /// The recorded entries, most recent first.
    /// </summary>
    public IReadOnlyList<OcrHistoryEntry> Entries => _entries;

    /// <summary>
    /// Records <paramref name="text"/> as a new history entry captured at
    /// <paramref name="capturedAt"/>.
    /// </summary>
    /// <returns>
    /// False (and nothing is recorded) when the text is empty or whitespace,
    /// or when the run <paramref name="runId"/> was already recorded.
    /// </returns>
    public bool Add(int runId, string text, DateTime capturedAt)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!_addedRunIds.Add(runId))
        {
            return false;
        }

        _entries.Insert(0, new OcrHistoryEntry(_nextId++, runId, capturedAt, text));

        // Drop the oldest entries beyond the limit, and free their run ids so
        // the bookkeeping stays bounded.
        while (_entries.Count > MaxEntries)
        {
            var dropped = _entries[^1];
            _entries.RemoveAt(_entries.Count - 1);
            _addedRunIds.Remove(dropped.RunId);
        }

        return true;
    }
}
