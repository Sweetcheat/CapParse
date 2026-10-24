using System.IO;
using System.Text.Json;

namespace CapParse.Configuration;

/// <summary>
/// The application's persisted settings. V0.1 persists a single value —
/// the capture hotkey — in a small JSON file under the user's local
/// application data folder (<c>%LOCALAPPDATA%\CapParse\settings.json</c>).
/// The file contains no personal data and no captured content.
///
/// Loading never throws: a missing file, an unreadable file, a file that
/// does not parse, or a file with an invalid value all yield the default
/// settings, and the file itself is never modified by loading. Saving is
/// atomic (a temporary file is written first and then moved over the
/// target), so a failure can never leave a half-written file behind.
/// </summary>
public sealed class AppSettings
{
    private const int SupportedVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public AppSettings()
    {
    }

    /// <summary>The capture hotkey (the default when nothing is persisted).</summary>
    public HotkeySetting CaptureHotkey { get; set; } = HotkeySetting.Default;

    /// <summary>
    /// True once the first-run presentation (<c>ui.md</c> §23) has been
    /// shown: it must never appear a second time. False in a missing or
    /// legacy file that predates the flag, which is exactly the first-run
    /// condition. Not personal data.
    /// </summary>
    public bool FirstRunPresented { get; set; }

    /// <summary>
    /// The theme preference (V0.2), as chosen in the settings. A missing
    /// or legacy file — one written before the preference existed — reads
    /// as <see cref="ThemePreference.System"/>, which is the only reading
    /// that does not change the appearance of an upgrading user. An
    /// invalid persisted value falls back to the same default (see
    /// <see cref="ParseThemePreference">); it never invalidates the rest
    /// of the file. Not personal data.
    /// </summary>
    public ThemePreference ThemePreference { get; set; } = ThemePreference.System;

    /// <summary>
    /// Whether the direct mode (direct capture actions) is enabled, as
    /// chosen in the settings. A missing or legacy file — one written
    /// before the setting existed — reads as <c>false</c>, so an existing
    /// installation never enters the direct mode after an update. The
    /// flag only controls the registrations: the configured combinations
    /// below are preserved while the mode is disabled.
    /// </summary>
    public bool DirectModeEnabled { get; set; }

    /// <summary>
    /// The direct-mode hotkey that copies the captured region as an image,
    /// or null when the slot is unset (the default: nothing is registered
    /// for it). An invalid persisted value reads as null (see
    /// <see cref="ValidHotkey">); the slot is never left on a combination
    /// the user did not configure.
    /// </summary>
    public HotkeySetting? DirectCopyImageHotkey { get; set; }

    /// <summary>The direct-mode hotkey that runs OCR and copies the text, or null when unset.</summary>
    public HotkeySetting? DirectCopyTextHotkey { get; set; }

    /// <summary>The direct-mode hotkey that saves the captured image, or null when unset.</summary>
    public HotkeySetting? DirectSaveImageHotkey { get; set; }

    /// <summary>
    /// How the direct "save image" action stores the image: ask for a
    /// location on every action (<see cref="ImageSavePolicy.Manual"/>, the
    /// default for missing and legacy files) or write to
    /// <see cref="DirectSaveFolder"/> (<see cref="ImageSavePolicy.Automatic").
    /// An invalid persisted value falls back to
    /// <see cref="ImageSavePolicy.Manual"/> (see
    /// <see cref="ParseSavePolicy">); it never invalidates the rest of the
    /// file.
    /// </summary>
    public ImageSavePolicy DirectSavePolicy { get; set; } = ImageSavePolicy.Manual;

    /// <summary>
    /// The folder the automatic save policy writes to (empty when not
    /// chosen). A user-chosen location, like the hotkey combinations: no
    /// personal data and no captured content. The application validates
    /// the folder before accepting the automatic policy (see the settings
    /// application in the application layer).
    /// </summary>
    public string DirectSaveFolder { get; set; } = string.Empty;

    /// <summary>The settings file location: <c>%LOCALAPPDATA%\CapParse\settings.json</c>.</summary>
    public static string DefaultSettingsPath
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CapParse", "settings.json");

    /// <summary>Loads the settings from the default location (defaults when the file is missing or invalid).</summary>
    public static AppSettings Load() => Load(DefaultSettingsPath);

    /// <summary>
    /// Loads the settings from the given file. A missing, unreadable,
    /// unparsable or invalid file yields the default settings and never
    /// throws; the file is never modified by loading.
    /// </summary>
    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            var file = JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(path), JsonOptions);
            if (file is null
                || file.Version != SupportedVersion
                || file.CaptureHotkey is not { } hotkey
                || !hotkey.IsValid)
            {
                return new AppSettings();
            }

            return new AppSettings
            {
                CaptureHotkey = hotkey,
                // Absent in a legacy file: the JSON default (false) is the
                // correct first-run reading.
                FirstRunPresented = file.FirstRunPresented,
                ThemePreference = ParseThemePreference(file.ThemePreference),
                // Each direct-mode field is parsed leniently on its own, like the
                // theme preference: a corrupted field degrades to its safe
                // default (mode off, slot unset, manual policy) without
                // invalidating the rest of the file.
                DirectModeEnabled = file.DirectModeEnabled,
                DirectCopyImageHotkey = ValidHotkey(file.DirectCopyImageHotkey),
                DirectCopyTextHotkey = ValidHotkey(file.DirectCopyTextHotkey),
                DirectSaveImageHotkey = ValidHotkey(file.DirectSaveImageHotkey),
                DirectSavePolicy = ParseSavePolicy(file.DirectSavePolicy),
                DirectSaveFolder = file.DirectSaveFolder ?? string.Empty
            };
        }
        catch
        {
            // A corrupted or unreadable file must never prevent the
            // application from starting: fall back to the defaults and
            // leave the file untouched (a later valid save rewrites it).
            return new AppSettings();
        }
    }

    /// <summary>Saves the settings to the default location. Returns false, without throwing, when the file cannot be written.</summary>
    public bool TrySave() => TrySave(DefaultSettingsPath);

    /// <summary>
    /// Saves the settings to the given file, atomically. Returns false,
    /// without throwing, when the file cannot be written (permissions,
    /// disk): the settings simply stay at their current value.
    /// </summary>
    public bool TrySave(string path)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var file = new SettingsFile
            {
                Version = SupportedVersion,
                CaptureHotkey = CaptureHotkey,
                FirstRunPresented = FirstRunPresented,
                ThemePreference = ThemePreference.ToString().ToLowerInvariant(),
                DirectModeEnabled = DirectModeEnabled,
                DirectCopyImageHotkey = DirectCopyImageHotkey,
                DirectCopyTextHotkey = DirectCopyTextHotkey,
                DirectSaveImageHotkey = DirectSaveImageHotkey,
                DirectSavePolicy = DirectSavePolicy.ToString().ToLowerInvariant(),
                DirectSaveFolder = DirectSaveFolder
            };

            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(file, JsonOptions));
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch
        {
            // A failure to write must not crash the application.
            return false;
        }
    }

    /// <summary>
    /// Parses the persisted theme preference leniently: absent (a legacy
    /// file) means <see cref="ThemePreference.System"/>; "system",
    /// "light" and "dark" (case-insensitive, surrounding whitespace
    /// ignored) map to the matching preference; any other value is
    /// corrupted and falls back to <see cref="ThemePreference.System"/>
    /// on its own, so the hotkey and the first-run flag survive a bad
    /// theme value.
    /// </summary>
    internal static ThemePreference ParseThemePreference(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ThemePreference.System;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "system" => ThemePreference.System,
            "light" => ThemePreference.Light,
            "dark" => ThemePreference.Dark,
            _ => ThemePreference.System
        };
    }

    /// <summary>
    /// Parses the persisted save policy leniently: absent (a legacy file)
    /// means <see cref="ImageSavePolicy.Manual"/>, the only policy that
    /// needs no folder; "manual" and "automatic" (case-insensitive,
    /// surrounding whitespace ignored) map to the matching policy; any
    /// other value is corrupted and falls back to
    /// <see cref="ImageSavePolicy.Manual"/> on its own, so the hotkeys and
    /// the rest of the file survive a bad policy value.
    /// </summary>
    internal static ImageSavePolicy ParseSavePolicy(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ImageSavePolicy.Manual;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "automatic" => ImageSavePolicy.Automatic,
            _ => ImageSavePolicy.Manual
        };
    }

    /// <summary>
    /// A persisted direct-mode hotkey is kept only when it is valid: a
    /// missing (unset slot) or corrupted value reads as "not configured",
    /// so a legacy or corrupted file can never register a hotkey the user
    /// did not set.
    /// </summary>
    private static HotkeySetting? ValidHotkey(HotkeySetting? setting)
        => setting is { } hotkey && hotkey.IsValid ? hotkey : null;

    /// <summary>
    /// A full copy of the current values: the settings window edits a
    /// clone (the pending change) and commits it to the application only
    /// when the user saves, so canceling can never modify the active
    /// configuration.
    /// </summary>
    public AppSettings Clone() => new()
    {
        CaptureHotkey = CaptureHotkey,
        FirstRunPresented = FirstRunPresented,
        ThemePreference = ThemePreference,
        DirectModeEnabled = DirectModeEnabled,
        DirectCopyImageHotkey = DirectCopyImageHotkey,
        DirectCopyTextHotkey = DirectCopyTextHotkey,
        DirectSaveImageHotkey = DirectSaveImageHotkey,
        DirectSavePolicy = DirectSavePolicy,
        DirectSaveFolder = DirectSaveFolder
    };

    /// <summary>
    /// The configured direct-mode hotkey of <paramref name="slot"/> (null
    /// when the slot is unset).
    /// </summary>
    public HotkeySetting? DirectSlot(int slot) => slot switch
    {
        HotkeySlots.DirectCopyImage => DirectCopyImageHotkey,
        HotkeySlots.DirectCopyText => DirectCopyTextHotkey,
        HotkeySlots.DirectSaveImage => DirectSaveImageHotkey,
        _ => null
    };

    /// <summary>
    /// Sets the configured direct-mode hotkey of <paramref name="slot"/>
    /// (null clears the slot). Clearing a slot never touches the other
    /// settings: disabling the direct mode keeps every configured
    /// combination.
    /// </summary>
    public void SetDirectSlot(int slot, HotkeySetting? value)
    {
        switch (slot)
        {
            case HotkeySlots.DirectCopyImage:
                DirectCopyImageHotkey = value;
                break;

            case HotkeySlots.DirectCopyText:
                DirectCopyTextHotkey = value;
                break;

            case HotkeySlots.DirectSaveImage:
                DirectSaveImageHotkey = value;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }

    /// <summary>The on-disk representation of the settings file.</summary>
    private sealed class SettingsFile
    {
        public int Version { get; set; }

        public HotkeySetting? CaptureHotkey { get; set; }

        public bool FirstRunPresented { get; set; }

        public string? ThemePreference { get; set; }

        public bool DirectModeEnabled { get; set; }

        public HotkeySetting? DirectCopyImageHotkey { get; set; }

        public HotkeySetting? DirectCopyTextHotkey { get; set; }

        public HotkeySetting? DirectSaveImageHotkey { get; set; }

        public string? DirectSavePolicy { get; set; }

        public string? DirectSaveFolder { get; set; }
    }
}
