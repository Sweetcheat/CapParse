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

            return new AppSettings { CaptureHotkey = hotkey };
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
                CaptureHotkey = CaptureHotkey
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

    /// <summary>The on-disk representation of the settings file.</summary>
    private sealed class SettingsFile
    {
        public int Version { get; set; }

        public HotkeySetting? CaptureHotkey { get; set; }
    }
}
