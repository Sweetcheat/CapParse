namespace CapParse.OCR;

/// <summary>
/// The outcome of one recognition run, as seen by the flow that started
/// it — distinct from <see cref="OcrRunState"/>, which is what the views
/// present:
/// <list type="bullet">
/// <item><see cref="Result"/> is set when the run validly completed the
/// shared state (a valid, possibly empty result).</item>
/// <item><see cref="Failed"/> is set only when the run failed and recorded
/// the failure in the state.</item>
/// <item>Neither set: the run was superseded or cancelled and a newer run
/// owns the state — not an error.</item>
/// </list>
/// The direct actions use this to decide their feedback: a superseded run
/// must stay silent, a failed one must be reported.
/// </summary>
public sealed record OcrRunOutcome(OcrResult? Result, bool Failed);
