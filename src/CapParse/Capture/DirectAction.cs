namespace CapParse.Capture;

/// <summary>
/// What the application does once a capture selection is finished.
/// <see cref="None"/> is the traditional flow: the crop is presented in
/// the main window and the user acts on its buttons. The direct actions
/// perform their single action right after the selection, with no result
/// window interaction. The action is passed explicitly to the capture
/// flow (the closure of the one in-flight capture carries it), so a
/// capture can never execute the wrong action.
/// </summary>
public enum DirectAction
{
    /// <summary>The traditional flow: the result window and its buttons.</summary>
    None,

    /// <summary>Copy the selected region to the clipboard as an image.</summary>
    CopyImage,

    /// <summary>Recognize the region's text locally and copy it.</summary>
    CopyText,

    /// <summary>Save the region to an image file (dialog in V0.2).</summary>
    SaveImage
}
