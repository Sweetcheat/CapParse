namespace CapParse.Configuration;

/// <summary>
/// How the direct "save image" action stores the captured region (direct
/// mode). <see cref="Manual"/> asks for a location on every action through
/// the standard save dialog (canceling it is a normal, silent outcome);
/// <see cref="Automatic"/> writes the image to the configured folder with a
/// generated, unique file name, without any dialog. The policy is persisted
/// in the settings file (see <see cref="AppSettings"/>).
/// </summary>
public enum ImageSavePolicy
{
    /// <summary>Ask for a location on every action (save dialog).</summary>
    Manual = 0,

    /// <summary>Write to the configured folder (unique name, never overwrites).</summary>
    Automatic = 1
}
