using System.IO;
using CapParse.Configuration;

namespace CapParse.Tests;

// The settings file lives in a temporary directory per test class run, so
// the tests never touch the user's real settings file.
public class AppSettingsTests : IDisposable
{
    private readonly string _directory =
        Directory.CreateTempSubdirectory("capparse-settings-").FullName;

    private string SettingsPath => System.IO.Path.Combine(_directory, "settings.json");

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // Best effort: a leftover temp directory is harmless.
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var settings = AppSettings.Load(SettingsPath);

        Assert.Equal(HotkeySetting.Default, settings.CaptureHotkey);
        Assert.False(settings.FirstRunPresented);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var settings = new AppSettings
        {
            CaptureHotkey = new HotkeySetting(
                (uint)CapParse.Platform.GlobalHotkey.Modifier.Control, 0x41) // Ctrl + A
        };

        Assert.True(settings.TrySave(SettingsPath));

        var loaded = AppSettings.Load(SettingsPath);
        Assert.Equal(settings.CaptureHotkey, loaded.CaptureHotkey);
        Assert.Equal("Ctrl + A", loaded.CaptureHotkey.DisplayString);
    }

    [Fact]
    public void Load_InvalidJson_ReturnsDefaults()
    {
        File.WriteAllText(SettingsPath, "{ this is not json ");

        var settings = AppSettings.Load(SettingsPath);

        Assert.Equal(HotkeySetting.Default, settings.CaptureHotkey);
    }

    [Theory]
    [InlineData(@"{ ""Version"": 1, ""CaptureHotkey"": { ""Modifiers"": 0, ""Key"": 88 } }")]     // no required modifier
    [InlineData(@"{ ""Version"": 1, ""CaptureHotkey"": { ""Modifiers"": 4, ""Key"": 88 } }")]    // Shift only
    [InlineData(@"{ ""Version"": 1, ""CaptureHotkey"": { ""Modifiers"": 2, ""Key"": 18 } }")]    // unsupported key (Esc)
    [InlineData(@"{ ""Version"": 1, ""CaptureHotkey"": { ""Modifiers"": 2, ""Key"": 0 } }")]     // no key
    [InlineData(@"{ ""Version"": 1, ""CaptureHotkey"": { ""Modifiers"": 16, ""Key"": 88 } }")]   // unknown modifier
    [InlineData(@"{ ""Version"": 1 }")]                                                         // hotkey missing
    [InlineData(@"{ ""Version"": 2, ""CaptureHotkey"": { ""Modifiers"": 6, ""Key"": 88 } }")]    // unsupported version
    public void Load_InvalidFile_ReturnsDefaults(string content)
    {
        File.WriteAllText(SettingsPath, content);

        var settings = AppSettings.Load(SettingsPath);

        Assert.Equal(HotkeySetting.Default, settings.CaptureHotkey);
    }

    [Fact]
    public void Save_ThenLoad_FirstRunPresented_RoundTrips()
    {
        var settings = new AppSettings
        {
            CaptureHotkey = HotkeySetting.Default,
            FirstRunPresented = true
        };

        Assert.True(settings.TrySave(SettingsPath));

        var loaded = AppSettings.Load(SettingsPath);
        Assert.True(loaded.FirstRunPresented);
    }

    [Fact]
    public void Load_LegacyFileWithoutTheFlag_FirstRunNotPresented()
    {
        // A file written before the flag existed must read as "first run
        // not presented yet", so the presentation shows for upgrading
        // users exactly once.
        File.WriteAllText(SettingsPath,
            "{ \"Version\": 1, \"CaptureHotkey\": { \"Modifiers\": 6, \"Key\": 88 } }");

        var settings = AppSettings.Load(SettingsPath);

        Assert.False(settings.FirstRunPresented);
        Assert.Equal("Ctrl + Shift + X", settings.CaptureHotkey.DisplayString);
    }

    [Fact]
    public void Load_InvalidFile_FirstRunNotPresented()
    {
        File.WriteAllText(SettingsPath, "{ this is not json ");

        Assert.False(AppSettings.Load(SettingsPath).FirstRunPresented);
    }

    [Fact]
    public void Load_DoesNotModifyTheFile()
    {
        const string content = "{ corrupted ";
        File.WriteAllText(SettingsPath, content);

        AppSettings.Load(SettingsPath);

        Assert.Equal(content, File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void Load_LegacyFileWithoutTheThemePreference_FallsBackToSystem()
    {
        // A file written before the preference existed must read as
        // "follow Windows": the only reading that does not change the
        // appearance of an upgrading user.
        File.WriteAllText(SettingsPath,
            "{ \"Version\": 1, \"CaptureHotkey\": { \"Modifiers\": 6, \"Key\": 88 } }");

        var settings = AppSettings.Load(SettingsPath);

        Assert.Equal(ThemePreference.System, settings.ThemePreference);
        Assert.Equal("Ctrl + Shift + X", settings.CaptureHotkey.DisplayString);
    }

    [Theory]
    [InlineData(ThemePreference.System, "system")]
    [InlineData(ThemePreference.Light, "light")]
    [InlineData(ThemePreference.Dark, "dark")]
    public void Save_ThenLoad_ThemePreferenceRoundTrips(
        ThemePreference preference, string expectedJsonValue)
    {
        var settings = new AppSettings { ThemePreference = preference };

        Assert.True(settings.TrySave(SettingsPath));

        var content = File.ReadAllText(SettingsPath);
        Assert.Contains($"\"ThemePreference\": \"{expectedJsonValue}\"", content);

        Assert.Equal(preference, AppSettings.Load(SettingsPath).ThemePreference);
    }

    [Theory]
    [InlineData("blue")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("2")]
    public void Load_InvalidThemePreference_FallsBackToSystemAndKeepsTheOtherValues(
        string themeValue)
    {
        // A corrupted theme value must degrade only the theme: the hotkey
        // and the first-run flag survive it.
        File.WriteAllText(SettingsPath,
            "{ \"Version\": 1, \"CaptureHotkey\": { \"Modifiers\": 6, \"Key\": 88 }, " +
            $"\"FirstRunPresented\": true, \"ThemePreference\": \"{themeValue}\" }}");

        var settings = AppSettings.Load(SettingsPath);

        Assert.Equal(ThemePreference.System, settings.ThemePreference);
        Assert.Equal("Ctrl + Shift + X", settings.CaptureHotkey.DisplayString);
        Assert.True(settings.FirstRunPresented);
    }

    [Fact]
    public void Load_ThemePreference_IsCaseInsensitiveAndToleratesWhitespace()
    {
        File.WriteAllText(SettingsPath,
            "{ \"Version\": 1, \"CaptureHotkey\": { \"Modifiers\": 6, \"Key\": 88 }, " +
            "\"ThemePreference\": \"  DaRk  \" }");

        Assert.Equal(ThemePreference.Dark, AppSettings.Load(SettingsPath).ThemePreference);
    }

    [Fact]
    public void Save_OverwritesThePreviousFile()
    {
        var first = new AppSettings
        {
            CaptureHotkey = new HotkeySetting(
                (uint)CapParse.Platform.GlobalHotkey.Modifier.Control, 0x41)
        };
        Assert.True(first.TrySave(SettingsPath));

        var second = new AppSettings
        {
            CaptureHotkey = HotkeySetting.Default
        };
        Assert.True(second.TrySave(SettingsPath));

        var loaded = AppSettings.Load(SettingsPath);
        Assert.Equal(HotkeySetting.Default, loaded.CaptureHotkey);
    }

    [Fact]
    public void Save_CreatesTheDirectoryWhenMissing()
    {
        var nested = System.IO.Path.Combine(_directory, "new", "sub", "settings.json");
        var settings = new AppSettings { CaptureHotkey = HotkeySetting.Default };

        Assert.True(settings.TrySave(nested));
        Assert.Equal(HotkeySetting.Default, AppSettings.Load(nested).CaptureHotkey);
    }

    [Fact]
    public void TrySave_UnwritablePath_ReturnsFalse()
    {
        // A directory component that is a regular file makes every
        // create/write attempt fail, without needing special permissions.
        var blocker = System.IO.Path.Combine(_directory, "blocker");
        File.WriteAllText(blocker, "blocker");
        var unwritable = System.IO.Path.Combine(blocker, "settings.json");

        var settings = new AppSettings { CaptureHotkey = HotkeySetting.Default };

        Assert.False(settings.TrySave(unwritable));
    }

    [Fact]
    public void DefaultSettingsPath_IsUnderTheUserLocalAppData()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(localAppData, AppSettings.DefaultSettingsPath);
        Assert.EndsWith(System.IO.Path.Combine("CapParse", "settings.json"), AppSettings.DefaultSettingsPath);
    }

    [Fact]
    public void ThePersistedFile_ContainsNoPersonalData()
    {
        // The settings file must never grow personal data: it holds the
        // version, the hotkey combination and the first-run flag, and
        // nothing else.
        var settings = new AppSettings
        {
            CaptureHotkey = HotkeySetting.Default,
            FirstRunPresented = true
        };
        Assert.True(settings.TrySave(SettingsPath));

        var content = File.ReadAllText(SettingsPath);
        Assert.Contains("\"Version\"", content);
        Assert.Contains("\"CaptureHotkey\"", content);
        Assert.Contains("\"Modifiers\"", content);
        Assert.Contains("\"Key\"", content);
        Assert.Contains("\"FirstRunPresented\"", content);
        Assert.Contains("\"ThemePreference\"", content);
    }

    // --------------------------------------------------------------------
    // Direct mode: defaults, legacy migration and per-field fallback
    // --------------------------------------------------------------------

    [Fact]
    public void Load_LegacyFile_DirectModeOff_SlotsUnset_ManualPolicy_EmptyFolder()
    {
        // A file written before the direct mode existed must read with the
        // mode disabled and every slot unset: an existing installation
        // never enters the direct mode after an update.
        File.WriteAllText(SettingsPath,
            "{ \"Version\": 1, \"CaptureHotkey\": { \"Modifiers\": 6, \"Key\": 88 } }");

        var settings = AppSettings.Load(SettingsPath);

        Assert.False(settings.DirectModeEnabled);
        Assert.Null(settings.DirectCopyImageHotkey);
        Assert.Null(settings.DirectCopyTextHotkey);
        Assert.Null(settings.DirectSaveImageHotkey);
        Assert.Equal(ImageSavePolicy.Manual, settings.DirectSavePolicy);
        Assert.Equal(string.Empty, settings.DirectSaveFolder);
        Assert.Equal("Ctrl + Shift + X", settings.CaptureHotkey.DisplayString);
    }

    [Fact]
    public void Save_ThenLoad_DirectModeRoundTrips()
    {
        var settings = new AppSettings
        {
            DirectModeEnabled = true,
            DirectCopyImageHotkey = new HotkeySetting(
                (uint)CapParse.Platform.GlobalHotkey.Modifier.Control, 0x43),
            DirectCopyTextHotkey = new HotkeySetting(
                (uint)CapParse.Platform.GlobalHotkey.Modifier.Control
                | (uint)CapParse.Platform.GlobalHotkey.Modifier.Shift, 0x44),
            // The third slot stays unset: an unset slot round-trips as
            // absent, and an absent slot reads back as unset.
            DirectSavePolicy = ImageSavePolicy.Automatic,
            DirectSaveFolder = "C:\\Capturas"
        };

        Assert.True(settings.TrySave(SettingsPath));

        var loaded = AppSettings.Load(SettingsPath);
        Assert.True(loaded.DirectModeEnabled);
        Assert.Equal(settings.DirectCopyImageHotkey, loaded.DirectCopyImageHotkey);
        Assert.Equal(settings.DirectCopyTextHotkey, loaded.DirectCopyTextHotkey);
        Assert.Null(loaded.DirectSaveImageHotkey);
        Assert.Equal(ImageSavePolicy.Automatic, loaded.DirectSavePolicy);
        Assert.Equal("C:\\Capturas", loaded.DirectSaveFolder);
    }

    [Theory]
    [InlineData("automatic")]
    [InlineData("AUTOMATIC")]
    [InlineData("  automatic  ")]
    public void Load_SavePolicy_IsCaseInsensitiveAndToleratesWhitespace(string policyValue)
    {
        File.WriteAllText(SettingsPath,
            "{ \"Version\": 1, \"CaptureHotkey\": { \"Modifiers\": 6, \"Key\": 88 }, " +
            $"\"DirectSavePolicy\": \"{policyValue}\" }}");

        Assert.Equal(ImageSavePolicy.Automatic, AppSettings.Load(SettingsPath).DirectSavePolicy);
    }

    [Theory]
    [InlineData("blue")]
    [InlineData("")]
    public void Load_InvalidSavePolicy_FallsBackToManualAndKeepsTheOtherValues(string policyValue)
    {
        // A corrupted policy value must degrade only the policy: the mode,
        // the slots and the rest of the file survive it.
        File.WriteAllText(SettingsPath,
            "{ \"Version\": 1, \"CaptureHotkey\": { \"Modifiers\": 6, \"Key\": 88 }, " +
            "\"DirectModeEnabled\": true, " +
            "\"DirectCopyImageHotkey\": { \"Modifiers\": 2, \"Key\": 67 }, " +
            $"\"DirectSavePolicy\": \"{policyValue}\" }}");

        var settings = AppSettings.Load(SettingsPath);

        Assert.Equal(ImageSavePolicy.Manual, settings.DirectSavePolicy);
        Assert.True(settings.DirectModeEnabled);
        Assert.NotNull(settings.DirectCopyImageHotkey);
        Assert.Equal("Ctrl + Shift + X", settings.CaptureHotkey.DisplayString);
    }

    [Fact]
    public void Load_InvalidDirectHotkey_SlotReadsAsUnset_OthersSurvive()
    {
        // A corrupted slot (Shift only: not a valid hotkey) reads as unset
        // on its own: the other slots and the mode flag survive it, and a
        // corrupted file can never register a hotkey the user did not set.
        File.WriteAllText(SettingsPath,
            "{ \"Version\": 1, \"CaptureHotkey\": { \"Modifiers\": 6, \"Key\": 88 }, " +
            "\"DirectModeEnabled\": true, " +
            "\"DirectCopyImageHotkey\": { \"Modifiers\": 4, \"Key\": 67 }, " +
            "\"DirectCopyTextHotkey\": { \"Modifiers\": 2, \"Key\": 68 } }");

        var settings = AppSettings.Load(SettingsPath);

        Assert.Null(settings.DirectCopyImageHotkey);
        Assert.NotNull(settings.DirectCopyTextHotkey);
        Assert.True(settings.DirectModeEnabled);
    }

    [Fact]
    public void Clone_CopiesEveryField_Independently()
    {
        var original = new AppSettings
        {
            CaptureHotkey = new HotkeySetting(2, 0x41),
            FirstRunPresented = true,
            ThemePreference = ThemePreference.Dark,
            DirectModeEnabled = true,
            DirectCopyImageHotkey = new HotkeySetting(2, 0x43),
            DirectSavePolicy = ImageSavePolicy.Automatic,
            DirectSaveFolder = "C:\\x"
        };

        var clone = original.Clone();

        // Editing the clone must not touch the original: the settings
        // window works on a clone precisely for this reason.
        clone.DirectModeEnabled = false;
        clone.DirectCopyImageHotkey = null;
        clone.DirectSaveFolder = "changed";
        clone.ThemePreference = ThemePreference.Light;
        clone.CaptureHotkey = HotkeySetting.Default;

        Assert.True(original.DirectModeEnabled);
        Assert.NotNull(original.DirectCopyImageHotkey);
        Assert.Equal("C:\\x", original.DirectSaveFolder);
        Assert.Equal(ThemePreference.Dark, original.ThemePreference);
        Assert.Equal(new HotkeySetting(2, 0x41), original.CaptureHotkey);
    }

    [Fact]
    public void SetDirectSlot_MapsTheRightFields()
    {
        var settings = new AppSettings();
        var combo = new HotkeySetting(2, 0x41);

        settings.SetDirectSlot(HotkeySlots.DirectCopyImage, combo);
        Assert.Equal(combo, settings.DirectCopyImageHotkey);
        Assert.Null(settings.DirectCopyTextHotkey);
        Assert.Null(settings.DirectSaveImageHotkey);

        settings.SetDirectSlot(HotkeySlots.DirectCopyText, combo);
        settings.SetDirectSlot(HotkeySlots.DirectCopyText, null);
        Assert.Null(settings.DirectCopyTextHotkey);

        Assert.Throws<ArgumentOutOfRangeException>(() => settings.SetDirectSlot(99, combo));
    }

    // --------------------------------------------------------------------
    // First-run mode choice (stage 5): persistence through the existing
    // settings, without the presentation creating any hotkey
    // --------------------------------------------------------------------

    [Fact]
    public void FirstRun_DirectChoice_PersistsTheFlag_WithoutCreatingAnyHotkey()
    {
        // The "direct mode" choice of the first-run presentation flips the
        // existing flag through the settings' own persistence: the flag
        // round-trips and the presentation never invents a hotkey — every
        // slot stays unset, so an installation that chose the direct mode
        // at first run registers nothing until the user configures
        // shortcuts in the settings.
        var settings = new AppSettings { DirectModeEnabled = true };

        Assert.True(settings.TrySave(SettingsPath));

        var loaded = AppSettings.Load(SettingsPath);

        Assert.True(loaded.DirectModeEnabled);
        Assert.Null(loaded.DirectCopyImageHotkey);
        Assert.Null(loaded.DirectCopyTextHotkey);
        Assert.Null(loaded.DirectSaveImageHotkey);
        Assert.Equal(ImageSavePolicy.Manual, loaded.DirectSavePolicy);
    }

    [Fact]
    public void FirstRun_TraditionalOrDeferredChoice_KeepsTheModeDisabled_AcrossRelaunches()
    {
        // Choosing (or deferring) the traditional mode leaves the flag at
        // the default of a new installation: the choice itself writes
        // nothing, and the file written when the presentation is shown
        // reads the mode back as disabled on the next launch, with the
        // first-run flag persisted so the presentation never appears
        // again.
        var settings = new AppSettings { FirstRunPresented = true };

        Assert.True(settings.TrySave(SettingsPath));

        var loaded = AppSettings.Load(SettingsPath);

        Assert.True(loaded.FirstRunPresented);
        Assert.False(loaded.DirectModeEnabled);
    }
}
