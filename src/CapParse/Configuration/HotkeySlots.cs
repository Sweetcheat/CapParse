namespace CapParse.Configuration;

/// <summary>
/// The Win32 hotkey ids the application owns, one per hotkey slot. The ids
/// are stable within the process's lifetime and must never be reused for a
/// different slot: a slot's registration is keyed by its id, so sharing an
/// id would let two slots drive one registration. The capture slot keeps
/// the id the application has always used; the direct slots are the
/// reserved ids of the direct mode's three actions.
/// </summary>
public static class HotkeySlots
{
    /// <summary>The traditional capture hotkey (Ctrl + Shift + X by default).</summary>
    public const int Capture = 1;

    /// <summary>Direct mode: capture a region and copy the image.</summary>
    public const int DirectCopyImage = 2;

    /// <summary>Direct mode: capture a region, run OCR and copy the text.</summary>
    public const int DirectCopyText = 3;

    /// <summary>Direct mode: capture a region and save the image.</summary>
    public const int DirectSaveImage = 4;

    /// <summary>
    /// The direct-mode slot ids, in the order the registrations are
    /// applied and rolled back: a fixed order keeps the transaction
    /// deterministic.
    /// </summary>
    public static readonly int[] Direct =
    {
        DirectCopyImage,
        DirectCopyText,
        DirectSaveImage
    };
}
